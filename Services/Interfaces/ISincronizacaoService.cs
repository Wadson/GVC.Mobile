using GVC.Mobile.Models;

namespace GVC.Mobile.Services.Interfaces;

public interface ISincronizacaoService
{
    Task<SincronizacaoResult> SincronizarAsync(
        IProgress<double>? progresso = null,
        CancellationToken cancellationToken = default);
}