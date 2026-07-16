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
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<SincronizacaoService> _logger;
    private readonly SemaphoreSlim _sincronizacaoLock = new(1, 1);
    private readonly IAppSettingsService  _settingsService;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public SincronizacaoService(
       IApiService apiService,
       IAppSettingsService settingsService,
       IEmpresaRepository empresaRepository,
       IProdutoRepository produtoRepository,
       IClienteRepository clienteRepository,
       IContaReceberRepository contaReceberRepository,
       DatabaseService databaseService,
       ILogger<SincronizacaoService> logger)
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _empresaRepository = empresaRepository;
        _produtoRepository = produtoRepository;
        _clienteRepository = clienteRepository;
        _contaReceberRepository = contaReceberRepository;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<SincronizacaoResult> SincronizarAsync(IProgress<double>? progresso = null, CancellationToken cancellationToken = default)
    {
        if (!await _sincronizacaoLock.WaitAsync(0, cancellationToken))
        {
            return new SincronizacaoResult
            {
                Sucesso = false, Mensagem = "Já existe uma sincronização em andamento."
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

            var arquivosExtraidos = Directory
                .EnumerateFiles(
                    pastaExtracao,
                    "*",
                    SearchOption.AllDirectories)
                .Select(arquivo =>
                    Path.GetRelativePath(
                        pastaExtracao,
                        arquivo))
                .ToList();

            _logger.LogInformation(
                "Arquivos extraídos do pacote: {Arquivos}",
                string.Join(
                    " | ",
                    arquivosExtraidos));

            progresso?.Report(0.62);

            var caminhoManifest =
                LocalizarArquivoObrigatorio(
                    pastaExtracao,
                    "manifest.json");

            var caminhoEmpresas =
                LocalizarArquivoObrigatorio(
                    pastaExtracao,
                    "empresas.json");

            var caminhoProdutos =
                LocalizarArquivoObrigatorio(
                    pastaExtracao,
                    "produtos.json");

            var caminhoClientes =
                LocalizarArquivoObrigatorio(
                    pastaExtracao,
                    "clientes.json");

            var caminhoContasReceber =
                LocalizarArquivoObrigatorio(
                    pastaExtracao,
                    "contas-receber.json");

            var manifest =
                await LerJsonAsync<SyncManifestDto>(
                    caminhoManifest,
                    cancellationToken);

            ValidarManifest(manifest);

            var empresasDto =
                await LerJsonAsync<List<EmpresaSyncDto>>(
                    caminhoEmpresas,
                    cancellationToken);

            var produtosDto =
                await LerJsonAsync<List<ProdutoSyncDto>>(
                    caminhoProdutos,
                    cancellationToken);

            var clientesDto =
                await LerJsonAsync<List<ClienteSyncDto>>(
                    caminhoClientes,
                    cancellationToken);

            var contasReceberDto =
                await LerJsonAsync<List<ContaReceberSyncDto>>(
                    caminhoContasReceber,
                    cancellationToken);


            if (empresasDto.Count != manifest.QuantidadeEmpresas)
            {
                throw new InvalidDataException( "A quantidade de empresas do arquivo não corresponde ao manifesto.");
            }

            if (produtosDto.Count !=  manifest.QuantidadeProdutos)
            {
                throw new InvalidDataException( "A quantidade de produtos do arquivo não corresponde ao manifesto.");
            }
            if (clientesDto.Count != manifest.QuantidadeClientes)
            {
                throw new InvalidDataException( "A quantidade de clientes do arquivo não corresponde ao manifesto.");
            }

            if (contasReceberDto.Count != manifest.QuantidadeContasReceber)
            {
                throw new InvalidDataException( "A quantidade de contas a receber do arquivo não corresponde ao manifesto.");
            }

            progresso?.Report(0.70);

            pastaImagensNova =
                await PrepararImagensAsync(
                    pastaExtracao,
                    cancellationToken);

            var empresas =
                ConverterEmpresas(
                    empresasDto);



            var produtos =
                 ConverterProdutos(
                     produtosDto,
                pastaImagensNova);

            var clientes =
                ConverterClientes(
                    clientesDto);

            var contasReceber =
                ConverterContasReceber(
                    contasReceberDto);

            progresso?.Report(0.78);


            progresso?.Report(0.76);

            // Empresas
            await _empresaRepository.SubstituirTodasAsync(
                empresas);

            GarantirEmpresaSelecionada(
                empresas);

            progresso?.Report(0.80);

            // Produtos
            await _produtoRepository.SubstituirTodosAsync(
                produtos);

            progresso?.Report(0.85);

            // Clientes
            await _clienteRepository.LimparAsync();

            await _clienteRepository.InserirOuAtualizarAsync(
                clientes);

            progresso?.Report(0.90);

            // Contas a receber
            await _contaReceberRepository.LimparAsync();

            await _contaReceberRepository.InserirOuAtualizarAsync(
                contasReceber);

            progresso?.Report(0.94);

            return new SincronizacaoResult
            {
                Sucesso = true,
                Mensagem = "Sincronização concluída com sucesso.",
                Versao = manifest.Versao,

                        QuantidadeEmpresas =   manifest.QuantidadeEmpresas,
                        QuantidadeProdutos = manifest.QuantidadeProdutos,
                        QuantidadeClientes = manifest.QuantidadeClientes,
                        QuantidadeContasReceber = manifest.QuantidadeContasReceber,
                        QuantidadeImagens = manifest.QuantidadeImagens,
                        QuantidadeImagensAusentes = manifest.QuantidadeImagensAusentes,
                        DataGeracaoUtc =  manifest.DataGeracaoUtc
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
    private void GarantirEmpresaSelecionada(
    IReadOnlyCollection<Empresa> empresas)
    {
        if (empresas.Count == 0)
            return;

        var settings =
            _settingsService.Obter();

        var empresaAtualExiste =
            empresas.Any(empresa =>
                empresa.EmpresaID ==
                settings.EmpresaID);

        if (empresaAtualExiste)
            return;

        var primeiraEmpresa =
            empresas
                .OrderBy(empresa =>
                    empresa.NomeExibicao)
                .First();

        settings.EmpresaID =
            primeiraEmpresa.EmpresaID;

        _settingsService.Salvar(
            settings);
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

    private static void ExtrairPacoteComSeguranca( string caminhoZip, string pastaDestino)
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

    private static string LocalizarArquivoObrigatorio(
    string pastaExtracao,
    string nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(pastaExtracao))
        {
            throw new ArgumentException(
                "A pasta de extração não foi informada.",
                nameof(pastaExtracao));
        }

        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new ArgumentException(
                "O nome do arquivo não foi informado.",
                nameof(nomeArquivo));
        }

        if (!Directory.Exists(pastaExtracao))
        {
            throw new DirectoryNotFoundException(
                $"A pasta de extração não foi encontrada: {pastaExtracao}");
        }

        var arquivos = Directory.EnumerateFiles(
            pastaExtracao,
            "*",
            SearchOption.AllDirectories);

        var arquivoEncontrado = arquivos.FirstOrDefault(
            arquivo =>
                string.Equals(
                    Path.GetFileName(arquivo),
                    nomeArquivo,
                    StringComparison.OrdinalIgnoreCase));

        if (arquivoEncontrado is null)
        {
            var arquivosEncontrados = Directory
                .EnumerateFiles(
                    pastaExtracao,
                    "*",
                    SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .OrderBy(nome => nome)
                .ToList();

            var lista = arquivosEncontrados.Count == 0
                ? "Nenhum arquivo foi extraído."
                : string.Join(", ", arquivosEncontrados);

            throw new FileNotFoundException(
                $"O arquivo obrigatório '{nomeArquivo}' não foi encontrado. " +
                $"Arquivos extraídos: {lista}");
        }

        return arquivoEncontrado;
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

    private static async Task<string> PrepararImagensAsync(string pastaExtracao,  CancellationToken cancellationToken)
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
    private static List<Empresa> ConverterEmpresas( IReadOnlyCollection<EmpresaSyncDto> empresasDto)
    {
        var empresas =
            new List<Empresa>(empresasDto.Count);

        foreach (var dto in empresasDto)
        {
            empresas.Add(new Empresa
            {
                EmpresaID =
                    dto.EmpresaID,

                RazaoSocial =
                    dto.RazaoSocial?.Trim()
                    ?? string.Empty,

                NomeFantasia =
                    dto.NomeFantasia?.Trim(),

                CNPJ =
                    dto.CNPJ?.Trim()
                    ?? string.Empty,

                InscricaoEstadual =
                    dto.InscricaoEstadual?.Trim(),

                Telefone =
                    dto.Telefone?.Trim(),

                Email =
                    dto.Email?.Trim(),

                Site =
                    dto.Site?.Trim(),

                Logo =
                    dto.Logo
            });
        }

        return empresas;
    }

    private static List<Produto> ConverterProdutos( IReadOnlyCollection<ProdutoSyncDto> produtosDto, string pastaImagensNova)
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

    private async Task SalvarConfiguracoesAsync( SyncManifestDto manifest)
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
                },
                new ConfiguracaoLocal
                {
                    Chave = "QuantidadeClientes",
                    Valor = manifest.QuantidadeClientes.ToString()
                },
                new ConfiguracaoLocal
                {
                    Chave = "QuantidadeContasReceber",
                    Valor = manifest.QuantidadeContasReceber.ToString()
                },
                new ConfiguracaoLocal
                {
                    Chave = "QuantidadeEmpresas",
                    Valor =
                        manifest.QuantidadeEmpresas.ToString()
                },


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
    private static List<Cliente> ConverterClientes(
    IReadOnlyCollection<ClienteSyncDto> clientesDto)
    {
        var clientes =
            new List<Cliente>(clientesDto.Count);

        foreach (var dto in clientesDto)
        {
            clientes.Add(new Cliente
            {
                ClienteID = dto.ClienteID,
                Nome = dto.Nome?.Trim() ?? string.Empty,
                Cpf = dto.Cpf?.Trim(),
                Cnpj = dto.Cnpj?.Trim(),
                Telefone = dto.Telefone?.Trim(),
                Email = dto.Email?.Trim(),
                TipoCliente = dto.TipoCliente?.Trim(),
                Status = dto.Status,
                LimiteCredito = dto.LimiteCredito,
                DataUltimaCompra = dto.DataUltimaCompra,
                EmpresaID = dto.EmpresaID
            });
        }

        return clientes;
    }


    private static List<ContaReceber> ConverterContasReceber(
    IReadOnlyCollection<ContaReceberSyncDto> contasDto)
    {
        var contas =
            new List<ContaReceber>(contasDto.Count);

        foreach (var dto in contasDto)
        {
            contas.Add(new ContaReceber
            {
                ParcelaID = dto.ParcelaID,
                VendaID = dto.VendaID,
                ClienteID = dto.ClienteID,
                NomeCliente = dto.NomeCliente?.Trim() ?? string.Empty,
                NumeroParcela = dto.NumeroParcela,
                DataVenda = dto.DataVenda,
                DataVencimento = dto.DataVencimento,
                ValorParcela = dto.ValorParcela,
                ValorRecebido = dto.ValorRecebido,
                Juros = dto.Juros,
                Multa = dto.Multa,
                Saldo = dto.Saldo,
                StatusParcela = dto.StatusParcela?.Trim() ?? string.Empty,
                FormaPgtoID = dto.FormaPgtoID,
                NomeFormaPagamento =
                    dto.NomeFormaPagamento?.Trim(),
                DataPagamento = dto.DataPagamento,
                ObservacaoParcela =
                    dto.ObservacaoParcela?.Trim(),
                ObservacaoVenda =
                    dto.ObservacaoVenda?.Trim(),
                TotalBrutoVenda = dto.TotalBrutoVenda,
                TotalDescontoVenda = dto.TotalDescontoVenda,
                TotalLiquidoVenda = dto.TotalLiquidoVenda,
                StatusVenda = dto.StatusVenda?.Trim() ?? string.Empty,
                EmpresaID = dto.EmpresaID
            });
        }

        return contas;
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