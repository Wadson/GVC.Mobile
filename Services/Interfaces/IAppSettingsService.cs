using GVC.Mobile.Configuration;

namespace GVC.Mobile.Services.Interfaces;

public interface IAppSettingsService
{
    ApiSettings Obter();

    void Salvar(ApiSettings settings);

    void RestaurarPadroes();
}