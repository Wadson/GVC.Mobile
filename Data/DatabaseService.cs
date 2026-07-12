using GVC.Mobile.Models;
using SQLite;


namespace GVC.Mobile.Data;

public sealed class DatabaseService
{
    private readonly SemaphoreSlim _inicializacaoLock = new(1, 1);

    private SQLiteAsyncConnection? _database;

    public async Task InicializarAsync()
    {
        if (_database is not null)
            return;

        await _inicializacaoLock.WaitAsync();

        try
        {
            if (_database is not null)
                return;

            var caminhoBanco = Path.Combine(
                FileSystem.AppDataDirectory,
                "gvc_mobile.db3");

            _database = new SQLiteAsyncConnection(
                caminhoBanco,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            await _database.CreateTableAsync<Produto>();

            await _database.CreateTableAsync<ConfiguracaoLocal>();
        }
        finally
        {
            _inicializacaoLock.Release();
        }
    }

    public async Task<SQLiteAsyncConnection> ObterConexaoAsync()
    {
        await InicializarAsync();

        return _database
            ?? throw new InvalidOperationException(
                "Não foi possível inicializar o banco SQLite.");
    }

    public async Task<string> ObterCaminhoBancoAsync()
    {
        await InicializarAsync();

        return Path.Combine(
            FileSystem.AppDataDirectory,
            "gvc_mobile.db3");
    }
}