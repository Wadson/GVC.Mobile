using CommunityToolkit.Maui;
using GVC.Mobile.Configuration;
using GVC.Mobile.Data;
using Microsoft.Extensions.Logging;
using GVC.Mobile.Services;
using GVC.Mobile.Services.Interfaces;
using GVC.Mobile.Views;
using GVC.Mobile.Repositories;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.ViewModels;

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

        builder.Services.AddSingleton(new ApiSettings
        {
            BaseUrl = "http://10.0.0.130:5080",
            ApiKey = "123456",
            ApiKeyHeaderName = "X-GVC-API-Key",
            EmpresaID = 3,
            Timeout = TimeSpan.FromMinutes(10)
        });

        builder.Services.AddTransient<ProdutosViewModel>();
        builder.Services.AddTransient<ProdutosPage>();
        builder.Services.AddTransient<ProdutoDetalheViewModel>();
        builder.Services.AddTransient<ProdutoDetalhePage>();


        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<IProdutoRepository, ProdutoRepository>();
        builder.Services.AddSingleton< ISincronizacaoService, SincronizacaoService>();
        builder.Services.AddHttpClient< IApiService, ApiService>( (serviceProvider, httpClient) =>
            {
                var settings = serviceProvider.GetRequiredService<ApiSettings>();

                var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";

                httpClient.BaseAddress =new Uri(baseUrl);

                httpClient.Timeout = settings.Timeout;

                httpClient.DefaultRequestHeaders.Add( settings.ApiKeyHeaderName, settings.ApiKey);

                httpClient.DefaultRequestHeaders.Add( "Accept", "application/zip, application/json");
            });        

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}