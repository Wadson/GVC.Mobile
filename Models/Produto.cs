using SQLite;

namespace GVC.Mobile.Models;

[Table("Produtos")]
public sealed class Produto
{
    [PrimaryKey]
    public int ProdutoID { get; set; }

    [Indexed]
    [MaxLength(100)]
    public string NomeProduto { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(15)]
    public string? Referencia { get; set; }

    public decimal? PrecoCompra { get; set; }

    public decimal PrecoCusto { get; set; }

    public decimal PrecoDeVenda { get; set; }

    public int Estoque { get; set; }

    [Indexed]
    [MaxLength(20)]
    public string? GtinEan { get; set; }

    public int? MarcaID { get; set; }

    [Indexed]
    [MaxLength(100)]
    public string? Marca { get; set; }

    [Indexed]
    public int EmpresaID { get; set; }

    public string? ImagemLocal { get; set; }
}