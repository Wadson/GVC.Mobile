using GVC.Mobile.Models;

namespace GVC.Mobile.Repositories.Interfaces;

public interface IEmpresaRepository
{
    Task SubstituirTodasAsync(
        IReadOnlyCollection<Empresa> empresas);

    Task<List<Empresa>> ObterTodasAsync();

    Task<Empresa?> ObterPorIdAsync(
        int empresaId);

    Task<int> ContarAsync();

    Task LimparAsync();
}