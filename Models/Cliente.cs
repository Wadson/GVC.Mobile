using SQLite;

namespace GVC.Mobile.Models;

[Table("Clientes")]
public sealed class Cliente
{
    [PrimaryKey]
    public int ClienteID { get; set; }

    [Indexed]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(11)]
    public string? Cpf { get; set; }

    [Indexed]
    [MaxLength(14)]
    public string? Cnpj { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? TipoCliente { get; set; }

    public int Status { get; set; }

    public decimal? LimiteCredito { get; set; }

    public DateTime? DataUltimaCompra { get; set; }

    [Indexed]
    public int EmpresaID { get; set; }
}