using GVC.Mobile.Models;
using GVC.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GVC.Mobile.Views;

public partial class ProdutosPage : ContentPage
{
    private readonly ProdutosViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _carregada;

    public ProdutosPage(
        ProdutosViewModel viewModel,
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

    private async void cvProdutos_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        var produto =
            e.CurrentSelection
                .FirstOrDefault() as Produto;

        if (produto is null)
            return;

        cvProdutos.SelectedItem = null;
        _viewModel.ProdutoSelecionado = null;

        var paginaDetalhe = _serviceProvider.GetRequiredService< ProdutoDetalhePage>();

        paginaDetalhe.CarregarProduto(produto);

        await Navigation.PushAsync(
            paginaDetalhe);
    }
}