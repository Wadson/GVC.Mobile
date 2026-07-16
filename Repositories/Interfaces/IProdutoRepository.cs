using GVC.Mobile.Models;

namespace GVC.Mobile.Repositories.Interfaces;

public interface IProdutoRepository
{
    Task SubstituirTodosAsync(
        IReadOnlyCollection<Produto> produtos);

    Task<int> ContarAsync(
        int? empresaId = null);

    Task<IReadOnlyList<Produto>> PesquisarAsync(
        string? termo,
        int empresaId,
        int limite = 200);

    Task<Produto?> ObterPorIdAsync(
        int produtoId);
}