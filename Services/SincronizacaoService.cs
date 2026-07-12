using GVC.Mobile.Data;
using GVC.Mobile.DTOs;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;

namespace GVC.Mobile.Services;

public sealed class SincronizacaoService : ISincronizacaoService
{
    private const string FormatoPacoteEsperado =
        "GVC-SYNC-1.0";

    private readonly IApiService _apiService;
    private readonly IProdutoRepository _produtoRepository;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<SincronizacaoService> _logger;

    private readonly SemaphoreSlim _sincronizacaoLock =
        new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public SincronizacaoService(
        IApiService apiService,
        IProdutoRepository produtoRepository,
        DatabaseService databaseService,
        ILogger<SincronizacaoService> logger)
    {
        _apiService = apiService;
        _produtoRepository = produtoRepository;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<SincronizacaoResult> SincronizarAsync(
        IProgress<double>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _sincronizacaoLock.WaitAsync(
                0,
                cancellationToken))
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem =
                    "Já existe uma sincronização em andamento."
            };
        }

        string? caminhoZip = null;
        string? pastaExtracao = null;
        string? pastaImagensNova = null;

        try
        {
            progresso?.Report(0.02);

            var conectado =
                await _apiService.VerificarConectividadeAsync(
                    cancellationToken);

            if (!conectado)
            {
                return new SincronizacaoResult
                {
                    Sucesso = false,
                    Mensagem =
                        "Não foi possível acessar a API. Verifique a rede e o servidor."
                };
            }

            progresso?.Report(0.05);

            var progressoDownload = new Progress<double>(
                valor =>
                {
                    var progressoTotal =
                        0.05 + valor * 0.45;

                    progresso?.Report(progressoTotal);
                });

            var download =
                await _apiService.BaixarPacoteCompletoAsync(
                    progressoDownload,
                    cancellationToken);

            if (!download.Sucesso ||
                string.IsNullOrWhiteSpace(
                    download.CaminhoArquivo))
            {
                return new SincronizacaoResult
                {
                    Sucesso = false,
                    Mensagem =
                        download.MensagemErro ??
                        "Não foi possível baixar o pacote."
                };
            }

            caminhoZip = download.CaminhoArquivo;

            progresso?.Report(0.52);

            pastaExtracao = CriarPastaTemporaria();

            ExtrairPacoteComSeguranca(
                caminhoZip,
                pastaExtracao);

            progresso?.Report(0.62);

            var manifest =
                await LerJsonAsync<SyncManifestDto>(
                    Path.Combine(
                        pastaExtracao,
                        "manifest.json"),
                    cancellationToken);

            ValidarManifest(manifest);

            var produtosDto =
                await LerJsonAsync<List<ProdutoSyncDto>>(
                    Path.Combine(
                        pastaExtracao,
                        "produtos.json"),
                    cancellationToken);

            if (produtosDto.Count !=
                manifest.QuantidadeProdutos)
            {
                throw new InvalidDataException(
                    "A quantidade de produtos do arquivo não corresponde ao manifesto.");
            }

            progresso?.Report(0.70);

            pastaImagensNova =
                await PrepararImagensAsync(
                    pastaExtracao,
                    cancellationToken);

            var produtos =
                ConverterProdutos(
                    produtosDto,
                    pastaImagensNova);

            progresso?.Report(0.80);

            await _produtoRepository.SubstituirTodosAsync(
                produtos);

            progresso?.Report(0.92);

            await SalvarConfiguracoesAsync(
                manifest);

            AtivarNovaPastaDeImagens(
                pastaImagensNova);

            pastaImagensNova = null;

            progresso?.Report(1);

            return new SincronizacaoResult
            {
                Sucesso = true,
                Mensagem =
                    "Sincronização concluída com sucesso.",
                Versao = manifest.Versao,
                QuantidadeProdutos =
                    manifest.QuantidadeProdutos,
                QuantidadeImagens =
                    manifest.QuantidadeImagens,
                QuantidadeImagensAusentes =
                    manifest.QuantidadeImagensAusentes,
                DataGeracaoUtc =
                    manifest.DataGeracaoUtc
            };
        }
        catch (OperationCanceledException)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem =
                    "A sincronização foi cancelada."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao sincronizar os dados.");

            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem =
                    $"Não foi possível concluir a sincronização: {ex.Message}"
            };
        }
        finally
        {
            ExcluirArquivoSilenciosamente(
                caminhoZip);

            ExcluirDiretorioSilenciosamente(
                pastaExtracao);

            ExcluirDiretorioSilenciosamente(
                pastaImagensNova);

            _sincronizacaoLock.Release();
        }
    }

    private static string CriarPastaTemporaria()
    {
        var pasta = Path.Combine(
            FileSystem.CacheDirectory,
            "sync",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(pasta);

        return pasta;
    }

    private static void ExtrairPacoteComSeguranca(
        string caminhoZip,
        string pastaDestino)
    {
        using var archive =
            ZipFile.OpenRead(caminhoZip);

        var raizDestino =
            Path.GetFullPath(pastaDestino)
            + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(
                    entry.FullName))
            {
                continue;
            }

            var caminhoDestino =
                Path.GetFullPath(
                    Path.Combine(
                        pastaDestino,
                        entry.FullName));

            if (!caminhoDestino.StartsWith(
                    raizDestino,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "O pacote contém um caminho de arquivo inválido.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(
                    caminhoDestino);

                continue;
            }

            var diretorio =
                Path.GetDirectoryName(
                    caminhoDestino);

            if (!string.IsNullOrWhiteSpace(
                    diretorio))
            {
                Directory.CreateDirectory(
                    diretorio);
            }

            entry.ExtractToFile(
                caminhoDestino,
                overwrite: true);
        }
    }

    private static async Task<T> LerJsonAsync<T>(
        string caminhoArquivo,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(caminhoArquivo))
        {
            throw new FileNotFoundException(
                $"O arquivo obrigatório não foi encontrado: {Path.GetFileName(caminhoArquivo)}");
        }

        await using var stream =
            File.OpenRead(caminhoArquivo);

        var resultado =
            await JsonSerializer.DeserializeAsync<T>(
                stream,
                JsonOptions,
                cancellationToken);

        return resultado
            ?? throw new InvalidDataException(
                $"O arquivo {Path.GetFileName(caminhoArquivo)} está vazio ou inválido.");
    }

    private static void ValidarManifest(
        SyncManifestDto manifest)
    {
        if (!string.Equals(
                manifest.FormatoPacote,
                FormatoPacoteEsperado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Formato de pacote incompatível: {manifest.FormatoPacote}.");
        }

        if (manifest.QuantidadeProdutos < 0)
        {
            throw new InvalidDataException(
                "O manifesto contém uma quantidade inválida de produtos.");
        }

        if (string.IsNullOrWhiteSpace(
                manifest.Versao))
        {
            throw new InvalidDataException(
                "O manifesto não possui uma versão válida.");
        }
    }

    private static async Task<string>
        PrepararImagensAsync(
            string pastaExtracao,
            CancellationToken cancellationToken)
    {
        var pastaOrigem = Path.Combine(
            pastaExtracao,
            "imagens");

        var pastaNova = Path.Combine(
            FileSystem.AppDataDirectory,
            $"imagens_novas_{Guid.NewGuid():N}");

        Directory.CreateDirectory(pastaNova);

        if (!Directory.Exists(pastaOrigem))
            return pastaNova;

        foreach (var arquivo in Directory.EnumerateFiles(
                     pastaOrigem,
                     "*",
                     SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var caminhoRelativo =
                Path.GetRelativePath(
                    pastaOrigem,
                    arquivo);

            var destino = Path.Combine(
                pastaNova,
                caminhoRelativo);

            var diretorioDestino =
                Path.GetDirectoryName(destino);

            if (!string.IsNullOrWhiteSpace(
                    diretorioDestino))
            {
                Directory.CreateDirectory(
                    diretorioDestino);
            }

            await using var origem =
                File.OpenRead(arquivo);

            await using var destinoStream =
                new FileStream(
                    destino,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true);

            await origem.CopyToAsync(
                destinoStream,
                cancellationToken);
        }

        return pastaNova;
    }

    private static List<Produto> ConverterProdutos(
        IReadOnlyCollection<ProdutoSyncDto> produtosDto,
        string pastaImagensNova)
    {
        var produtos =
            new List<Produto>(
                produtosDto.Count);

        foreach (var dto in produtosDto)
        {
            string? caminhoImagemLocal = null;

            if (!string.IsNullOrWhiteSpace(
                    dto.ImagemLocal))
            {
                var nomeArquivo =
                    Path.GetFileName(
                        dto.ImagemLocal);

                var caminhoTemporario =
                    Path.Combine(
                        pastaImagensNova,
                        nomeArquivo);

                if (File.Exists(caminhoTemporario))
                {
                    caminhoImagemLocal =
                        Path.Combine(
                            FileSystem.AppDataDirectory,
                            "imagens",
                            nomeArquivo);
                }
            }

            produtos.Add(new Produto
            {
                ProdutoID = dto.ProdutoID,
                NomeProduto =
                    dto.NomeProduto.Trim(),
                Referencia =
                    dto.Referencia?.Trim(),
                PrecoCompra =
                    dto.PrecoCompra,
                PrecoCusto =
                    dto.PrecoCusto,
                PrecoDeVenda =
                    dto.PrecoDeVenda,
                Estoque =
                    dto.Estoque,
                GtinEan =
                    dto.GtinEan?.Trim(),
                MarcaID =
                    dto.MarcaID,
                Marca =
                    dto.Marca?.Trim(),
                EmpresaID =
                    dto.EmpresaID,
                ImagemLocal =
                    caminhoImagemLocal
            });
        }

        return produtos;
    }

    private async Task SalvarConfiguracoesAsync(
        SyncManifestDto manifest)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        var configuracoes =
            new[]
            {
                new ConfiguracaoLocal
                {
                    Chave =
                        "UltimaVersaoSincronizada",
                    Valor =
                        manifest.Versao
                },
                new ConfiguracaoLocal
                {
                    Chave =
                        "UltimaSincronizacaoUtc",
                    Valor =
                        DateTime.UtcNow.ToString("O")
                },
                new ConfiguracaoLocal
                {
                    Chave =
                        "QuantidadeProdutos",
                    Valor =
                        manifest.QuantidadeProdutos.ToString()
                },
                new ConfiguracaoLocal
                {
                    Chave =
                        "QuantidadeImagens",
                    Valor =
                        manifest.QuantidadeImagens.ToString()
                }
            };

        foreach (var configuracao in configuracoes)
        {
            await database.InsertOrReplaceAsync(
                configuracao);
        }
    }

    private static void AtivarNovaPastaDeImagens(
        string pastaNova)
    {
        var pastaAtual = Path.Combine(
            FileSystem.AppDataDirectory,
            "imagens");

        var pastaBackup = Path.Combine(
            FileSystem.AppDataDirectory,
            "imagens_backup");

        ExcluirDiretorioSilenciosamente(
            pastaBackup);

        if (Directory.Exists(pastaAtual))
        {
            Directory.Move(
                pastaAtual,
                pastaBackup);
        }

        Directory.Move(
            pastaNova,
            pastaAtual);

        ExcluirDiretorioSilenciosamente(
            pastaBackup);
    }

    private static void ExcluirArquivoSilenciosamente(
        string? caminho)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(caminho) &&
                File.Exists(caminho))
            {
                File.Delete(caminho);
            }
        }
        catch
        {
            // Limpeza auxiliar.
        }
    }

    private static void ExcluirDiretorioSilenciosamente(
        string? caminho)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(caminho) &&
                Directory.Exists(caminho))
            {
                Directory.Delete(
                    caminho,
                    recursive: true);
            }
        }
        catch
        {
            // Limpeza auxiliar.
        }
    }
}