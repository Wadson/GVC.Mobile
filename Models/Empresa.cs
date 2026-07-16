using SQLite;

namespace GVC.Mobile.Models;

[Table("Empresas")]
public sealed class Empresa
{
    [PrimaryKey]
    public int EmpresaID { get; set; }

    [MaxLength(150)]
    public string RazaoSocial { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? NomeFantasia { get; set; }

    [MaxLength(14)]
    public string CNPJ { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? InscricaoEstadual { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Site { get; set; }

    public byte[]? Logo { get; set; }

    [Ignore]
    public string NomeExibicao =>
        !string.IsNullOrWhiteSpace(NomeFantasia)
            ? NomeFantasia.Trim()
            : RazaoSocial.Trim();

    [Ignore]
    public string CnpjFormatado =>
        FormatarCnpj(CNPJ);

    public override string ToString()
    {
        return NomeExibicao;
    }

    private static string FormatarCnpj(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "CNPJ não informado";

        var numeros = new string(
            valor.Where(char.IsDigit).ToArray());

        if (numeros.Length != 14)
            return valor;

        return Convert.ToUInt64(numeros)
            .ToString(@"00\.000\.000\/0000\-00");
    }
}