using CommunityToolkit.Maui;
using GVC.Mobile.Data;
using GVC.Mobile.Repositories;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services;
using GVC.Mobile.Services.Interfaces;
using GVC.Mobile.ViewModels;
using GVC.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace GVC.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

        // -------------------------------------------------
        // SERVIÇOS PRINCIPAIS
        // -------------------------------------------------

        builder.Services.AddSingleton<DatabaseService>();

        builder.Services.AddSingleton<
            IAppSettingsService,
            AppSettingsService>();

        builder.Services.AddSingleton<
            IProdutoRepository,
            ProdutoRepository>();

        builder.Services.AddSingleton<
            ISincronizacaoService,
            SincronizacaoService>();

        // -------------------------------------------------
        // HTTP / API
        // -------------------------------------------------

        builder.Services.AddHttpClient<
            IApiService,
            ApiService>(
            httpClient =>
            {
                httpClient.Timeout =
                    TimeSpan.FromMinutes(10);
            });

        // -------------------------------------------------
        // VIEWMODELS
        // -------------------------------------------------

        builder.Services.AddTransient<HomeViewModel>();

        builder.Services.AddTransient<ProdutosViewModel>();

        builder.Services.AddTransient<
            ProdutoDetalheViewModel>();

        // -------------------------------------------------
        // PÁGINAS
        // -------------------------------------------------

        builder.Services.AddTransient<HomePage>();

        builder.Services.AddTransient<ProdutosPage>();

        builder.Services.AddTransient<
            ProdutoDetalhePage>();

        builder.Services.AddTransient<
            ConfiguracoesPage>();

        builder.Services.AddTransient<SobrePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}