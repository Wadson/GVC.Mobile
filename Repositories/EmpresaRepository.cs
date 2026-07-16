using GVC.Mobile.Data;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;

namespace GVC.Mobile.Repositories;

public sealed class EmpresaRepository
    : IEmpresaRepository
{
    private readonly DatabaseService
        _databaseService;

    private readonly SemaphoreSlim
        _operacaoLock = new(1, 1);

    public EmpresaRepository(
        DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SubstituirTodasAsync(
        IReadOnlyCollection<Empresa> empresas)
    {
        ArgumentNullException.ThrowIfNull(empresas);

        var database =
            await _databaseService.ObterConexaoAsync();

        await _operacaoLock.WaitAsync();

        try
        {
            await database.RunInTransactionAsync(
                connection =>
                {
                    connection.DeleteAll<Empresa>();

                    if (empresas.Count > 0)
                    {
                        connection.InsertAll(
                            empresas,
                            runInTransaction: false);
                    }
                });
        }
        finally
        {
            _operacaoLock.Release();
        }
    }

    public async Task<List<Empresa>>
        ObterTodasAsync()
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        var empresas =
            await database
                .Table<Empresa>()
                .ToListAsync();

        return empresas
            .OrderBy(empresa =>
                empresa.NomeExibicao)
            .ToList();
    }

    public async Task<Empresa?> ObterPorIdAsync(
        int empresaId)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<Empresa>()
            .Where(empresa =>
                empresa.EmpresaID == empresaId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> ContarAsync()
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<Empresa>()
            .CountAsync();
    }

    public async Task LimparAsync()
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        await database.DeleteAllAsync<Empresa>();
    }
}