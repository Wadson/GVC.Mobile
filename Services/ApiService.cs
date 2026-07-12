using GVC.Mobile.Configuration;
using GVC.Mobile.Models;
using GVC.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GVC.Mobile.Services;

public sealed class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _settings;
    private readonly ILogger<ApiService> _logger;

    public ApiService(
        HttpClient httpClient,
        ApiSettings settings,
        ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> VerificarConectividadeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "health",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Não foi possível acessar o Health Check da API.");

            return false;
        }
    }

    public async Task<DownloadPacoteResult> BaixarPacoteCompletoAsync(
        IProgress<double>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        var nomeArquivoLocal =
            $"GVC_SYNC_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

        var caminhoArquivoLocal = Path.Combine(
            FileSystem.CacheDirectory,
            nomeArquivoLocal);

        try
        {
            var endpoint =
                $"api/sincronizacao/completa?empresaId={_settings.EmpresaID}";

            _logger.LogInformation(
                "Iniciando download do pacote de sincronização. Endpoint: {Endpoint}",
                endpoint);

            using var response = await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var conteudoErro =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogWarning(
                    "Falha ao baixar o pacote. Status: {StatusCode}. Resposta: {Resposta}",
                    response.StatusCode,
                    conteudoErro);

                return new DownloadPacoteResult
                {
                    Sucesso = false,
                    MensagemErro = CriarMensagemErro(
                        response.StatusCode,
                        conteudoErro)
                };
            }

            var tamanhoTotal =
                response.Content.Headers.ContentLength;

            await using var streamOrigem =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using var streamDestino = new FileStream(
                caminhoArquivoLocal,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 128 * 1024,
                useAsync: true);

            var buffer = new byte[128 * 1024];
            long totalBaixado = 0;

            while (true)
            {
                var quantidadeLida =
                    await streamOrigem.ReadAsync(
                        buffer.AsMemory(0, buffer.Length),
                        cancellationToken);

                if (quantidadeLida == 0)
                    break;

                await streamDestino.WriteAsync(
                    buffer.AsMemory(0, quantidadeLida),
                    cancellationToken);

                totalBaixado += quantidadeLida;

                if (tamanhoTotal.HasValue &&
                    tamanhoTotal.Value > 0)
                {
                    var percentual =
                        (double)totalBaixado /
                        tamanhoTotal.Value;

                    progresso?.Report(percentual);
                }
            }

            await streamDestino.FlushAsync(
                cancellationToken);

            progresso?.Report(1);

            var arquivoInfo = new FileInfo(
                caminhoArquivoLocal);

            if (!arquivoInfo.Exists ||
                arquivoInfo.Length == 0)
            {
                throw new InvalidOperationException(
                    "O pacote foi baixado, mas o arquivo está vazio.");
            }

            _logger.LogInformation(
                "Pacote baixado com sucesso. Caminho: {Caminho}. Tamanho: {TamanhoBytes} bytes.",
                caminhoArquivoLocal,
                arquivoInfo.Length);

            return new DownloadPacoteResult
            {
                Sucesso = true,
                CaminhoArquivo = caminhoArquivoLocal,
                NomeArquivo = nomeArquivoLocal,
                TamanhoBytes = arquivoInfo.Length
            };
        }
        catch (OperationCanceledException)
        {
            ExcluirArquivoParcial(caminhoArquivoLocal);

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    "O download da sincronização foi cancelado."
            };
        }
        catch (HttpRequestException ex)
        {
            ExcluirArquivoParcial(caminhoArquivoLocal);

            _logger.LogError(
                ex,
                "Erro de comunicação com a API.");

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    "Não foi possível conectar à API. Verifique a rede, o endereço do servidor e se a API está em execução."
            };
        }
        catch (Exception ex)
        {
            ExcluirArquivoParcial(caminhoArquivoLocal);

            _logger.LogError(
                ex,
                "Erro ao baixar o pacote de sincronização.");

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    $"Não foi possível baixar o pacote: {ex.Message}"
            };
        }
    }

    private static string CriarMensagemErro(
        HttpStatusCode statusCode,
        string conteudoResposta)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "A API recusou a chave de acesso.",

            HttpStatusCode.Forbidden =>
                "O acesso ao recurso foi negado.",

            HttpStatusCode.NotFound =>
                "O endpoint de sincronização não foi encontrado.",

            HttpStatusCode.TooManyRequests =>
                "O limite de sincronizações foi atingido. Aguarde antes de tentar novamente.",

            HttpStatusCode.ServiceUnavailable =>
                "A API ou o banco de dados está temporariamente indisponível.",

            _ =>
                string.IsNullOrWhiteSpace(conteudoResposta)
                    ? $"A API retornou o código {(int)statusCode}."
                    : conteudoResposta
        };
    }

    private static void ExcluirArquivoParcial(
        string caminhoArquivo)
    {
        try
        {
            if (File.Exists(caminhoArquivo))
                File.Delete(caminhoArquivo);
        }
        catch
        {
            // Não substitui o erro original.
        }
    }
}