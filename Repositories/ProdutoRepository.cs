using GVC.Mobile.Data;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;

namespace GVC.Mobile.Repositories;

public sealed class ProdutoRepository : IProdutoRepository
{
    private readonly DatabaseService _databaseService;
    private readonly SemaphoreSlim _operacaoLock = new(1, 1);

    public ProdutoRepository(
        DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SubstituirTodosAsync(
        IReadOnlyCollection<Produto> produtos)
    {
        if (produtos is null)
        {
            throw new ArgumentNullException(
                nameof(produtos));
        }

        var database =
            await _databaseService.ObterConexaoAsync();

        await _operacaoLock.WaitAsync();

        try
        {
            await database.RunInTransactionAsync(
                conexao =>
                {
                    conexao.DeleteAll<Produto>();

                    if (produtos.Count > 0)
                    {
                        conexao.InsertAll(
                            produtos,
                            runInTransaction: false);
                    }
                });
        }
        finally
        {
            _operacaoLock.Release();
        }
    }

    public async Task<int> ContarAsync(
       int? empresaId = null)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        if (empresaId.HasValue)
        {
            return await database
                .Table<Produto>()
                .Where(produto =>
                    produto.EmpresaID ==
                    empresaId.Value)
                .CountAsync();
        }

        return await database
            .Table<Produto>()
            .CountAsync();
    }

    public async Task<IReadOnlyList<Produto>> PesquisarAsync(
    string? termo,
    int empresaId,
    int limite = 200)
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
                .Table<Produto>()
                .Where(produto =>
                    produto.EmpresaID == empresaId)
                .OrderBy(produto =>
                    produto.NomeProduto)
                .Take(limite)
                .ToListAsync();
        }

        var padrao =
            $"%{termoNormalizado}%";

        if (int.TryParse(
                termoNormalizado,
                out var produtoId))
        {
            return await database.QueryAsync<Produto>(
                """
            SELECT *
            FROM Produtos
            WHERE EmpresaID = ?
              AND (
                   ProdutoID = ?
                OR NomeProduto LIKE ?
                OR Referencia LIKE ?
                OR GtinEan LIKE ?
                OR Marca LIKE ?
              )
            ORDER BY
                CASE
                    WHEN ProdutoID = ? THEN 0
                    ELSE 1
                END,
                NomeProduto
            LIMIT ?;
            """,
                empresaId,
                produtoId,
                padrao,
                padrao,
                padrao,
                padrao,
                produtoId,
                limite);
        }

        return await database.QueryAsync<Produto>(
            """
        SELECT *
        FROM Produtos
        WHERE EmpresaID = ?
          AND (
               NomeProduto LIKE ?
            OR Referencia LIKE ?
            OR GtinEan LIKE ?
            OR Marca LIKE ?
          )
        ORDER BY NomeProduto
        LIMIT ?;
        """,
            empresaId,
            padrao,
            padrao,
            padrao,
            padrao,
            limite);
    }

    public async Task<Produto?> ObterPorIdAsync(
        int produtoId)
    {
        var database =
            await _databaseService.ObterConexaoAsync();

        return await database
            .Table<Produto>()
            .Where(produto =>
                produto.ProdutoID == produtoId)
            .FirstOrDefaultAsync();
    }
}