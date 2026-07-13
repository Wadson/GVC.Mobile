using GVC.Mobile.Configuration;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string ChaveBaseUrl =
        "ApiSettings.BaseUrl";

    private const string ChaveApiKey =
        "ApiSettings.ApiKey";

    private const string ChaveHeaderName =
        "ApiSettings.ApiKeyHeaderName";

    private const string ChaveEmpresaId =
        "ApiSettings.EmpresaID";

    private const string BaseUrlPadrao =
        "http://10.0.0.130:5080";

    private const string ApiKeyPadrao =
        "123456";

    private const string HeaderPadrao =
        "X-GVC-API-Key";

    private const int EmpresaIdPadrao = 3;

    public ApiSettings Obter()
    {
        return new ApiSettings
        {
            BaseUrl = Preferences.Default.Get(
                ChaveBaseUrl,
                BaseUrlPadrao),

            ApiKey = Preferences.Default.Get(
                ChaveApiKey,
                ApiKeyPadrao),

            ApiKeyHeaderName = Preferences.Default.Get(
                ChaveHeaderName,
                HeaderPadrao),

            EmpresaID = Preferences.Default.Get(
                ChaveEmpresaId,
                EmpresaIdPadrao),

            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public void Salvar(ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var baseUrl = NormalizarBaseUrl(
            settings.BaseUrl);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException(
                "Informe o endereço da API.");
        }

        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "O endereço da API não é válido.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new ArgumentException(
                "Informe a chave de acesso da API.");
        }

        if (settings.EmpresaID <= 0)
        {
            throw new ArgumentException(
                "O código da empresa deve ser maior que zero.");
        }

        Preferences.Default.Set(
            ChaveBaseUrl,
            baseUrl);

        Preferences.Default.Set(
            ChaveApiKey,
            settings.ApiKey.Trim());

        Preferences.Default.Set(
            ChaveHeaderName,
            string.IsNullOrWhiteSpace(
                settings.ApiKeyHeaderName)
                ? HeaderPadrao
                : settings.ApiKeyHeaderName.Trim());

        Preferences.Default.Set(
            ChaveEmpresaId,
            settings.EmpresaID);
    }

    public void RestaurarPadroes()
    {
        Preferences.Default.Remove(
            ChaveBaseUrl);

        Preferences.Default.Remove(
            ChaveApiKey);

        Preferences.Default.Remove(
            ChaveHeaderName);

        Preferences.Default.Remove(
            ChaveEmpresaId);
    }

    private static string NormalizarBaseUrl(
        string? baseUrl)
    {
        return baseUrl?
            .Trim()
            .TrimEnd('/')
            ?? string.Empty;
    }
}