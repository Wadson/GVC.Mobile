using CommunityToolkit.Mvvm.ComponentModel;
using GVC.Mobile.Models;

namespace GVC.Mobile.ViewModels;

public partial class ProdutoDetalheViewModel : ObservableObject
{
    [ObservableProperty]
    private Produto? _produto;

    public void Carregar(
        Produto produto)
    {
        Produto = produto;
    }
}