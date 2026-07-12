namespace GVC.Mobile.Configuration;

public sealed class ApiSettings
{
    public string BaseUrl { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string ApiKeyHeaderName { get; init; } = "X-GVC-API-Key";

    public int EmpresaID { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}