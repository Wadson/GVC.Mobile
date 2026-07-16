using GVC.Mobile.Models;

namespace GVC.Mobile.Repositories.Interfaces;

public interface IClienteRepository
{
    Task InserirOuAtualizarAsync(
        IEnumerable<Cliente> clientes);

    Task<List<Cliente>> ObterTodosAsync(
        int empresaId);

    Task<List<Cliente>> PesquisarAsync(
        string termo,
        int empresaId,
        int limite = 50);

    Task<Cliente?> ObterAsync(
        int clienteId);

    Task<int> ContarAsync(
        int? empresaId = null);

    Task LimparAsync();
}