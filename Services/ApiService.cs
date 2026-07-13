using GVC.Mobile.Configuration;
using GVC.Mobile.Models;
using GVC.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GVC.Mobile.Services;

public sealed class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<ApiService> _logger;

    public ApiService(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<bool> VerificarConectividadeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settingsService.Obter();

            using var request = CriarRequest(
                HttpMethod.Get,
                settings,
                "health",
                incluirApiKey: false);

            using var response =
                await _httpClient.SendAsync(
                    request,
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

    public async Task<DownloadPacoteResult>
        BaixarPacoteCompletoAsync(
            IProgress<double>? progresso = null,
            CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Obter();

        var nomeArquivoLocal =
            $"GVC_SYNC_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

        var caminhoArquivoLocal = Path.Combine(
            FileSystem.CacheDirectory,
            nomeArquivoLocal);

        try
        {
            var endpoint =
                $"api/sincronizacao/completa" +
                $"?empresaId={settings.EmpresaID}";

            using var request = CriarRequest(
                HttpMethod.Get,
                settings,
                endpoint,
                incluirApiKey: true);

            request.Headers.TryAddWithoutValidation(
                "Accept",
                "application/zip, application/json");

            using var response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var conteudoErro =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

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

            await using var origem =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using var destino = new FileStream(
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
                    await origem.ReadAsync(
                        buffer.AsMemory(
                            0,
                            buffer.Length),
                        cancellationToken);

                if (quantidadeLida == 0)
                    break;

                await destino.WriteAsync(
                    buffer.AsMemory(
                        0,
                        quantidadeLida),
                    cancellationToken);

                totalBaixado += quantidadeLida;

                if (tamanhoTotal is > 0)
                {
                    progresso?.Report(
                        (double)totalBaixado /
                        tamanhoTotal.Value);
                }
            }

            await destino.FlushAsync(
                cancellationToken);

            progresso?.Report(1);

            var arquivo = new FileInfo(
                caminhoArquivoLocal);

            if (!arquivo.Exists ||
                arquivo.Length == 0)
            {
                throw new InvalidOperationException(
                    "O pacote baixado está vazio.");
            }

            return new DownloadPacoteResult
            {
                Sucesso = true,
                CaminhoArquivo = caminhoArquivoLocal,
                NomeArquivo = nomeArquivoLocal,
                TamanhoBytes = arquivo.Length
            };
        }
        catch (OperationCanceledException)
        {
            ExcluirArquivoParcial(
                caminhoArquivoLocal);

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    "A sincronização foi cancelada."
            };
        }
        catch (HttpRequestException ex)
        {
            ExcluirArquivoParcial(
                caminhoArquivoLocal);

            _logger.LogError(
                ex,
                "Falha de comunicação com a API.");

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    "Não foi possível conectar à API. " +
                    "Verifique o endereço, a rede e o servidor."
            };
        }
        catch (Exception ex)
        {
            ExcluirArquivoParcial(
                caminhoArquivoLocal);

            _logger.LogError(
                ex,
                "Erro durante o download do pacote.");

            return new DownloadPacoteResult
            {
                Sucesso = false,
                MensagemErro =
                    $"Não foi possível baixar o pacote: " +
                    $"{ex.Message}"
            };
        }
    }

    private static HttpRequestMessage CriarRequest(
        HttpMethod method,
        ApiSettings settings,
        string endpoint,
        bool incluirApiKey)
    {
        var baseUrl =
            settings.BaseUrl.TrimEnd('/');

        var caminho =
            endpoint.TrimStart('/');

        var request = new HttpRequestMessage(
            method,
            $"{baseUrl}/{caminho}");

        if (incluirApiKey)
        {
            request.Headers.TryAddWithoutValidation(
                settings.ApiKeyHeaderName,
                settings.ApiKey);
        }

        return request;
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
                "O endpoint não foi encontrado.",

            HttpStatusCode.TooManyRequests =>
                "O limite de sincronizações foi atingido. " +
                "Aguarde antes de tentar novamente.",

            HttpStatusCode.ServiceUnavailable =>
                "A API ou o banco está indisponível.",

            _ =>
                string.IsNullOrWhiteSpace(
                    conteudoResposta)
                    ? $"A API retornou o código " +
                      $"{(int)statusCode}."
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