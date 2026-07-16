using GVC.Mobile.Models;

namespace GVC.Mobile.Services.Interfaces;

public interface IProdutoCardService
{
    Task<string> GerarCardAsync(
        Produto produto,
        CancellationToken cancellationToken = default);
}