using GVC.Mobile.Configuration;
using GVC.Mobile.Services.Interfaces;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;

namespace GVC.Mobile.Views;

public partial class ConfiguracoesPage : ContentPage
{
    private readonly IAppSettingsService _settingsService;
    private readonly IApiService _apiService;
    private readonly IEmpresaRepository _empresaRepository;

    private List<Empresa> _empresas = [];

    public ConfiguracoesPage(
      IAppSettingsService settingsService,
      IApiService apiService,
      IEmpresaRepository empresaRepository)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _apiService = apiService;
        _empresaRepository = empresaRepository;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarConfiguracoesAsync();
    }

    private async Task CarregarConfiguracoesAsync()
    {
        ////Temporariamente, exibe a quantidade de empresas no SQLite para fins de teste
        //var quantidade = await _empresaRepository.ContarAsync();

        //await DisplayAlert(
        //    "Empresas",
        //    $"Qtde de empresas no SQLite: {quantidade}",
        //    "OK");

        ////Carrega as configurações salvas no SQLite



        var settings =
            _settingsService.Obter();

        txtBaseUrl.Text =
            settings.BaseUrl;

        txtApiKey.Text =
            settings.ApiKey;

        _empresas =
            await _empresaRepository.ObterTodasAsync();

        pckEmpresa.ItemsSource =
            _empresas;

        pckEmpresa.SelectedItem =
            _empresas.FirstOrDefault(
                empresa =>
                    empresa.EmpresaID ==
                    settings.EmpresaID);

        if (pckEmpresa.SelectedItem is null &&
            _empresas.Count > 0)
        {
            pckEmpresa.SelectedItem =
                _empresas[0];
        }

        lblResultado.Text =
            _empresas.Count == 0
                ? "Nenhuma empresa sincronizada. Execute a sincronização de dados."
                : $"{_empresas.Count:N0} empresa(s) disponível(is).";
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

    private async void Salvar_Clicked( object sender, EventArgs e)
    {
        try
        {
            SalvarConfiguracoes();

            await DisplayAlertAsync(
                "Configurações",
                "Configurações salvas com sucesso.",
                "OK");

            var empresa = pckEmpresa.SelectedItem as Empresa;

            await DisplayAlertAsync(
                "Configurações",
                empresa is null
                    ? "Configurações salvas com sucesso."
                    : $"Empresa selecionada:\n{empresa.NomeExibicao}\n\n" +
                      "As telas de produtos e contas utilizarão esta empresa.",
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

        await CarregarConfiguracoesAsync();
    }

    private void SalvarConfiguracoes()
    {
        if (pckEmpresa.SelectedItem is not Empresa
            empresaSelecionada)
        {
            throw new ArgumentException(
                "Selecione uma empresa para consulta.");
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
                    empresaSelecionada.EmpresaID,

                Timeout =
                    TimeSpan.FromMinutes(10)
            });
    }
}