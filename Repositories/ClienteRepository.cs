using GVC.Mobile.Data;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;

namespace GVC.Mobile.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly DatabaseService _databaseService;
    private readonly SemaphoreSlim _operacaoLock = new(1, 1);

    public ClienteRepository(
        DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task InserirOuAtualizarAsync(
        IEnumerable<Cliente> clientes)
    {
        ArgumentNullException.ThrowIfNull(clientes);

        var lista = clientes.ToList();

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

    public async Task<List<Cliente>> ObterTodosAsync(
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
            .Table<Cliente>()
            .Where(cliente =>
                cliente.EmpresaID == empresaId)
            .OrderBy(cliente =>
                cliente.Nome)
            .ToListAsync();
    }

    public async Task<List<Cliente>> PesquisarAsync(
        string termo,
        int empresaId,
        int limite = 50)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentException(
                "O código da empresa deve ser maior que zero.",
                nameof(empresaId));
        }

        var database =
            await _databaseService.ObterConexaoAsync();

        limite = Math.Clamp(
            limite,
            1,
            500);

        var termoNormalizado =
            termo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                termoNormalizado))
        {
            return await database
                .Table<Cliente>()
                .Where(cliente =>
                    cliente.EmpresaID == empresaId)
                .OrderBy(cliente =>
                    cliente.Nome)
                .Take(limite)
                .ToListAsync();
        }

        var padrao =
            $"%{termoNormalizado}%";

        if (int.TryParse(
                termoNormalizado,
                out var clienteId))
        {
            return await database.QueryAsync<Cliente>(
                """
                SELECT *
                FROM Clientes
                WHERE EmpresaID = ?
                  AND (
                       ClienteID = ?
                    OR Nome LIKE ?
                    OR Cpf LIKE ?
                    OR Cnpj LIKE ?
                    OR Telefone LIKE ?
                  )
                ORDER BY
                    CASE
                        WHEN ClienteID = ? THEN 0
                        ELSE 1
                    END,
                    Nome
                LIMIT ?;
                """,
                empresaId,
                clienteId,
                padrao,
                padrao,
                padrao,
                padrao,
                clienteId,
                limite);
        }

        return await database.QueryAsync<Cliente>(
            """
            SELECT *
            FROM Clientes
            WHERE EmpresaID = ?
              AND (
                   Nome LIKE ?
                OR Cpf LIKE ?
                OR Cnpj LIKE ?
                OR Telefone LIKE ?
              )
            ORDER BY Nome
            LIMIT ?;
            """,
            empresaId,
            padrao,
            padrao,
            padrao,
            padrao,
            limite);
    }

    public async Task<Cliente?> ObterAsync(
        int clienteId)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<Cliente>()
            .Where(cliente =>
                cliente.ClienteID == clienteId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> ContarAsync(
        int? empresaId = null)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        if (empresaId.HasValue)
        {
            return await database
                .Table<Cliente>()
                .Where(cliente =>
                    cliente.EmpresaID ==
                    empresaId.Value)
                .CountAsync();
        }

        return await database
            .Table<Cliente>()
            .CountAsync();
    }

    public async Task LimparAsync()
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        await database.DeleteAllAsync<Cliente>();
    }
}