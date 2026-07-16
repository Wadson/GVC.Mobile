using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVC.Mobile.Models;

namespace GVC.Mobile.ViewModels;

public partial class ProdutoDetalheViewModel : ObservableObject
{
    [ObservableProperty]
    private Produto? _produto;

    [ObservableProperty]
    private bool _mostrarPrecosInternos;

    public string TextoBotaoPrecos =>
        MostrarPrecosInternos
            ? "Ocultar preços internos"
            : "Mostrar preços internos";

    public string IconeBotaoPrecos =>
        MostrarPrecosInternos
            ? "🙈"
            : "👁";

    public void Carregar(Produto produto)
    {
        Produto = produto;

        // Sempre abre os detalhes com compra e custo ocultos.
        MostrarPrecosInternos = false;
    }

    [RelayCommand]
    private void AlternarPrecosInternos()
    {
        MostrarPrecosInternos =
            !MostrarPrecosInternos;
    }

    partial void OnMostrarPrecosInternosChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(TextoBotaoPrecos));

        OnPropertyChanged(
            nameof(IconeBotaoPrecos));
    }
}