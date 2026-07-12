using GVC.Mobile.Models;

namespace GVC.Mobile.Services.Interfaces;

public interface IApiService
{
    Task<bool> VerificarConectividadeAsync(
        CancellationToken cancellationToken = default);

    Task<DownloadPacoteResult> BaixarPacoteCompletoAsync(
        IProgress<double>? progresso = null,
        CancellationToken cancellationToken = default);
}