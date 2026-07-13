using GVC.Mobile.Data;
using GVC.Mobile.Views;

namespace GVC.Mobile;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly HomePage _homePage;

    public App(
        DatabaseService databaseService,
        HomePage homePage)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _homePage = homePage;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var navigationPage =
            new NavigationPage(_homePage)
            {
                BarBackgroundColor =
                    Color.FromArgb("#173B57"),

                BarTextColor =
                    Colors.White
            };

        return new Window(navigationPage);
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            await _databaseService.InicializarAsync();
        }
        catch (Exception ex)
        {
            var paginaAtual =
                Windows.FirstOrDefault()?.Page;

            if (paginaAtual is not null)
            {
                await paginaAtual.DisplayAlertAsync(
                    "Erro",
                    $"Não foi possível inicializar o banco local.\n\n{ex.Message}",
                    "OK");
            }
        }
    }
}