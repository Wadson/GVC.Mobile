using GVC.Mobile.Models;
using GVC.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GVC.Mobile.Views;

public partial class ClientesContasPage : ContentPage
{
    private readonly ClientesContasViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    private bool _carregada;

    public ClientesContasPage(
        ClientesContasViewModel viewModel,
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

        if (_carregada)
            return;

        _carregada = true;

        await _viewModel.CarregarAsync();
    }

    private async void cvClientes_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        var cliente =
            e.CurrentSelection
                .FirstOrDefault()
            as ClienteContaResumo;

        if (cliente is null)
            return;

        cvClientes.SelectedItem = null;
        _viewModel.ClienteSelecionado = null;

        var pagina =
            _serviceProvider.GetRequiredService<
                ContasClientePage>();

        await pagina.CarregarClienteAsync(cliente);

        await Navigation.PushAsync(pagina);
    }
}