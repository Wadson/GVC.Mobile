using SQLite;
using Microsoft.Maui.Graphics;


namespace GVC.Mobile.Models;

[Table("ContasReceber")]
public sealed class ContaReceber
{
    [PrimaryKey]
    public int ParcelaID { get; set; }

    [Indexed]
    public int VendaID { get; set; }

    [Indexed]
    public int ClienteID { get; set; }

    [Indexed]
    [MaxLength(100)]
    public string NomeCliente { get; set; } = string.Empty;

    public int NumeroParcela { get; set; }

    public DateTime DataVenda { get; set; }

    [Indexed]
    public DateTime DataVencimento { get; set; }

    public decimal ValorParcela { get; set; }

    public decimal ValorRecebido { get; set; }

    public decimal Juros { get; set; }

    public decimal Multa { get; set; }

    public decimal Saldo { get; set; }

    [Indexed]
    [MaxLength(20)]
    public string StatusParcela { get; set; } = string.Empty;

    public int? FormaPgtoID { get; set; }

    [MaxLength(50)]
    public string? NomeFormaPagamento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public string? ObservacaoParcela { get; set; }

    public string? ObservacaoVenda { get; set; }

    public decimal TotalBrutoVenda { get; set; }

    public decimal TotalDescontoVenda { get; set; }

    public decimal TotalLiquidoVenda { get; set; }

    [MaxLength(20)]
    public string StatusVenda { get; set; } = string.Empty;

    [Indexed]
    public int EmpresaID { get; set; }

    [Ignore]
    public string StatusExibicao
    {
        get
        {
            if (Saldo <= 0 ||
                string.Equals(
                    StatusParcela,
                    "Pago",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Pago";
            }

            if (string.Equals(
                    StatusParcela,
                    "Cancelada",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Cancelada";
            }

            if (DataVencimento.Date < DateTime.Today)
                return "Atrasada";

            return StatusParcela;
        }
    }

    [Ignore]
    public Color CorStatus
    {
        get
        {
            return StatusExibicao switch
            {
                "Pago" =>
                    Color.FromArgb("#16734A"),

                "Atrasada" =>
                    Color.FromArgb("#A12727"),

                "Cancelada" =>
                    Color.FromArgb("#737B82"),

                "ParcialmentePago" =>
                    Color.FromArgb("#B26B00"),

                _ =>
                    Color.FromArgb("#173B57")
            };
        }
    }

    [Ignore]
    public Color CorFundoStatus
    {
        get
        {
            return StatusExibicao switch
            {
                "Pago" =>
                    Color.FromArgb("#EAF7F0"),

                "Atrasada" =>
                    Color.FromArgb("#FFF0F0"),

                "Cancelada" =>
                    Color.FromArgb("#F0F1F2"),

                "ParcialmentePago" =>
                    Color.FromArgb("#FFF5E5"),

                _ =>
                    Color.FromArgb("#EDF4F8")
            };
        }
    }
}