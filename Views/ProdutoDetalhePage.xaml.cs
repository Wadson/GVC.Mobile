using GVC.Mobile.Models;
using GVC.Mobile.ViewModels;

namespace GVC.Mobile.Views;

public partial class ProdutoDetalhePage : ContentPage
{
    private readonly ProdutoDetalheViewModel _viewModel;

    public ProdutoDetalhePage(
        ProdutoDetalheViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        BindingContext = _viewModel;
    }

    public void CarregarProduto(
        Produto produto)
    {
        _viewModel.Carregar(produto);
    }
}