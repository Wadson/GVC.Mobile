using GVC.Mobile.Data;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;

namespace GVC.Mobile.Repositories;

public sealed class ContaReceberRepository
    : IContaReceberRepository
{
    private readonly DatabaseService _databaseService;

    private readonly SemaphoreSlim _operacaoLock =
        new(1, 1);

    public ContaReceberRepository(
        DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task InserirOuAtualizarAsync(
        IEnumerable<ContaReceber> contas)
    {
        ArgumentNullException.ThrowIfNull(contas);

        var lista = contas.ToList();

        if (lista.Count == 0)
            return;

        var database =
            await _databaseService.ObterConexaoAsync();

        await _operacaoLock.WaitAsync();

        try
        {
            await database.RunInTransactionAsync(
                connection =>
                {
                    connection.InsertAll(
                        lista,
                        runInTransaction: false);
                });
        }
        finally
        {
            _operacaoLock.Release();
        }
    }

    public async Task<List<ContaReceber>>
        ObterTodasAsync(
            int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentException(
                "O código da empresa deve ser maior que zero.",
                nameof(empresaId));
        }

        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<ContaReceber>()
            .Where(conta =>
                conta.EmpresaID == empresaId)
            .OrderBy(conta =>
                conta.NomeCliente)
            .ThenBy(conta =>
                conta.DataVencimento)
            .ToListAsync();
    }

    public async Task<List<ContaReceber>>
        PesquisarPorClienteAsync(
            int clienteId,
            int empresaId)
    {
        if (clienteId <= 0)
        {
            throw new ArgumentException(
                "O código do cliente deve ser maior que zero.",
                nameof(clienteId));
        }

        if (empresaId <= 0)
        {
            throw new ArgumentException(
                "O código da empresa deve ser maior que zero.",
                nameof(empresaId));
        }

        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<ContaReceber>()
            .Where(conta =>
                conta.ClienteID == clienteId &&
                conta.EmpresaID == empresaId)
            .OrderBy(conta =>
                conta.DataVencimento)
            .ThenBy(conta =>
                conta.NumeroParcela)
            .ToListAsync();
    }

    public async Task<int> ContarAsync(
        int? empresaId = null)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        if (empresaId.HasValue)
        {
            return await database
                .Table<ContaReceber>()
                .Where(conta =>
                    conta.EmpresaID ==
                    empresaId.Value)
                .CountAsync();
        }

        return await database
            .Table<ContaReceber>()
            .CountAsync();
    }

    public async Task LimparAsync()
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        await database
            .DeleteAllAsync<ContaReceber>();
    }
}