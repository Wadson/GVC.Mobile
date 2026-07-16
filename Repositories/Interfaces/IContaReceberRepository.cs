using GVC.Mobile.Models;

namespace GVC.Mobile.Repositories.Interfaces;

public interface IContaReceberRepository
{
    Task InserirOuAtualizarAsync(
        IEnumerable<ContaReceber> contas);

    Task<List<ContaReceber>> ObterTodasAsync(
     int empresaId);

    Task<List<ContaReceber>> PesquisarPorClienteAsync(
        int clienteId,
        int empresaId);

    Task<int> ContarAsync(
        int? empresaId = null);

    Task LimparAsync();
}