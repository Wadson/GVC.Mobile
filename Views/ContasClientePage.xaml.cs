using GVC.Mobile.Models;
using GVC.Mobile.ViewModels;

namespace GVC.Mobile.Views;

public partial class ContasClientePage : ContentPage
{
    private readonly ContasClienteViewModel _viewModel;

    public ContasClientePage(
        ContasClienteViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        BindingContext = _viewModel;
    }

    public Task CarregarClienteAsync(
        ClienteContaResumo cliente)
    {
        return _viewModel.CarregarAsync(cliente);
    }
}