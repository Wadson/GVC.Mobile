namespace GVC.Mobile.Configuration;

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyHeaderName { get; set; } =
        "X-GVC-API-Key";

    public int EmpresaID { get; set; }

    public TimeSpan Timeout { get; set; } =
        TimeSpan.FromMinutes(10);
}