namespace GVC.Mobile.DTOs;

public sealed class ClienteSyncDto
{
    public int ClienteID { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? TipoCliente { get; set; }

    public int Status { get; set; }

    public decimal? LimiteCredito { get; set; }

    public DateTime? DataUltimaCompra { get; set; }

    public int EmpresaID { get; set; }
}