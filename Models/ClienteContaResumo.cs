namespace GVC.Mobile.Models;

public sealed class ClienteContaResumo
{
    public int ClienteID { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public int QuantidadeParcelas { get; set; }

    public int QuantidadeAtrasadas { get; set; }

    public decimal TotalParcelas { get; set; }

    public decimal TotalRecebido { get; set; }

    public decimal SaldoEmAberto { get; set; }

    public decimal SaldoVencido { get; set; }

    public string Documento
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Cnpj))
                return FormatarCnpj(Cnpj);

            if (!string.IsNullOrWhiteSpace(Cpf))
                return FormatarCpf(Cpf);

            return "Documento não informado";
        }
    }

    public bool PossuiSaldo =>
        SaldoEmAberto > 0;

    public bool PossuiAtraso =>
        SaldoVencido > 0;

    private static string FormatarCpf(string valor)
    {
        var numeros = SomenteNumeros(valor);

        if (numeros.Length != 11)
            return valor;

        return Convert.ToUInt64(numeros)
            .ToString(@"000\.000\.000\-00");
    }

    private static string FormatarCnpj(string valor)
    {
        var numeros = SomenteNumeros(valor);

        if (numeros.Length != 14)
            return valor;

        return Convert.ToUInt64(numeros)
            .ToString(@"00\.000\.000\/0000\-00");
    }

    private static string SomenteNumeros(string valor)
    {
        return new string(
            valor.Where(char.IsDigit).ToArray());
    }
}