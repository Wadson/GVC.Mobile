using GVC.Mobile.Configuration;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.Views;

public partial class ConfiguracoesPage : ContentPage
{
    private readonly IAppSettingsService
        _settingsService;

    private readonly IApiService
        _apiService;

    public ConfiguracoesPage(
        IAppSettingsService settingsService,
        IApiService apiService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _apiService = apiService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarConfiguracoes();
    }

    private void CarregarConfiguracoes()
    {
        var settings =
            _settingsService.Obter();

        txtBaseUrl.Text =
            settings.BaseUrl;

        txtEmpresaId.Text =
            settings.EmpresaID.ToString();

        txtApiKey.Text =
            settings.ApiKey;

        lblResultado.Text =
            string.Empty;
    }

    private async void TestarConexao_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            loading.IsVisible = true;
            loading.IsRunning = true;

            SalvarConfiguracoes();

            lblResultado.Text =
                "Testando conexão...";

            var conectado =
                await _apiService
                    .VerificarConectividadeAsync();

            lblResultado.Text = conectado
                ? "Conexão realizada com sucesso."
                : "Não foi possível acessar a API.";
        }
        catch (Exception ex)
        {
            lblResultado.Text =
                ex.Message;
        }
        finally
        {
            loading.IsVisible = false;
            loading.IsRunning = false;
        }
    }

    private async void Salvar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            SalvarConfiguracoes();

            await DisplayAlertAsync(
                "Configurações",
                "Configurações salvas com sucesso.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro",
                ex.Message,
                "OK");
        }
    }

    private async void Restaurar_Clicked(
        object sender,
        EventArgs e)
    {
        var confirmar =
            await DisplayAlertAsync(
                "Restaurar configurações",
                "Deseja restaurar o endereço, empresa e chave padrão?",
                "Restaurar",
                "Cancelar");

        if (!confirmar)
            return;

        _settingsService.RestaurarPadroes();

        CarregarConfiguracoes();
    }

    private void SalvarConfiguracoes()
    {
        if (!int.TryParse(
                txtEmpresaId.Text,
                out var empresaId))
        {
            throw new ArgumentException(
                "Informe um código de empresa válido.");
        }

        _settingsService.Salvar(
            new ApiSettings
            {
                BaseUrl =
                    txtBaseUrl.Text?.Trim()
                    ?? string.Empty,

                ApiKey =
                    txtApiKey.Text?.Trim()
                    ?? string.Empty,

                ApiKeyHeaderName =
                    "X-GVC-API-Key",

                EmpresaID =
                    empresaId,

                Timeout =
                    TimeSpan.FromMinutes(10)
            });
    }
}