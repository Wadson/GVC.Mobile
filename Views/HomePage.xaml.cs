using GVC.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GVC.Mobile.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public HomePage(
        HomeViewModel viewModel,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _serviceProvider = serviceProvider;

        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.CarregarResumoAsync();
    }

    private async void Sincronizar_Clicked(
        object sender,
        EventArgs e)
    {
        var confirmar =
            await DisplayAlertAsync(
                "Sincronizar produtos",
                "Deseja baixar novamente todos os produtos e imagens?",
                "Sincronizar",
                "Cancelar");

        if (!confirmar)
            return;

        var resultado =
            await _viewModel.SincronizarAsync();

        await DisplayAlertAsync(
            resultado.Sucesso
                ? "Sincronização concluída"
                : "Falha na sincronização",
            resultado.Sucesso
                ? $"Produtos: {resultado.QuantidadeProdutos}\n" +
                  $"Imagens: {resultado.QuantidadeImagens}\n" +
                  $"Versão: {resultado.Versao}"
                : resultado.Mensagem,
            "OK");
    }

    private async void ConsultarProdutos_Clicked(
        object sender,
        EventArgs e)
    {
        var pagina =
            _serviceProvider.GetRequiredService<
                ProdutosPage>();

        await Navigation.PushAsync(pagina);
    }

    private async void Configuracoes_Clicked(
        object sender,
        EventArgs e)
    {
        var pagina =
            _serviceProvider.GetRequiredService<
                ConfiguracoesPage>();

        await Navigation.PushAsync(pagina);
    }

    private async void Sobre_Clicked(
        object sender,
        EventArgs e)
    {
        var pagina =
            _serviceProvider.GetRequiredService<
                SobrePage>();

        await Navigation.PushAsync(pagina);
    }
}