using GVC.Mobile.Models;
using GVC.Mobile.Services.Interfaces;
using GVC.Mobile.ViewModels;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace GVC.Mobile.Views;

public partial class ProdutoDetalhePage : ContentPage
{
    private readonly ProdutoDetalheViewModel _viewModel;
    private readonly IProdutoCardService _produtoCardService;

    private bool _estaCompartilhando;

    public ProdutoDetalhePage(
        ProdutoDetalheViewModel viewModel,
        IProdutoCardService produtoCardService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _produtoCardService =
            produtoCardService;

        BindingContext = _viewModel;
    }

    public void CarregarProduto(
        Produto produto)
    {
        _viewModel.Carregar(produto);
    }

    private async void CompartilharProduto_Clicked(
        object sender,
        EventArgs e)
    {
        if (_estaCompartilhando)
            return;

        var produto =
            _viewModel.Produto;

        if (produto is null)
        {
            await DisplayAlertAsync(
                "Compartilhar produto",
                "Nenhum produto foi carregado.",
                "OK");

            return;
        }

        try
        {
            _estaCompartilhando = true;

            if (sender is Button botao)
            {
                botao.IsEnabled = false;
                botao.Text =
                    "Gerando card...";
            }

            var caminhoCard =
                await _produtoCardService
                    .GerarCardAsync(produto);

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title =
                        $"Compartilhar {produto.NomeProduto}",

                    File =
                        new ShareFile(caminhoCard)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro ao compartilhar",
                $"Não foi possível gerar ou compartilhar o card.\n\n{ex.Message}",
                "OK");
        }
        finally
        {
            _estaCompartilhando = false;

            if (sender is Button botao)
            {
                botao.IsEnabled = true;
                botao.Text =
                    "Enviar card pelo WhatsApp";
            }
        }
    }
}