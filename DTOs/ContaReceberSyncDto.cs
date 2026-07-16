namespace GVC.Mobile.DTOs;

public sealed class ContaReceberSyncDto
{
    public int ParcelaID { get; set; }

    public int VendaID { get; set; }

    public int ClienteID { get; set; }

    public string NomeCliente { get; set; } = string.Empty;

    public int NumeroParcela { get; set; }

    public DateTime DataVenda { get; set; }

    public DateTime DataVencimento { get; set; }

    public decimal ValorParcela { get; set; }

    public decimal ValorRecebido { get; set; }

    public decimal Juros { get; set; }

    public decimal Multa { get; set; }

    public decimal Saldo { get; set; }

    public string StatusParcela { get; set; } = string.Empty;

    public int? FormaPgtoID { get; set; }

    public string? NomeFormaPagamento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public string? ObservacaoParcela { get; set; }

    public string? ObservacaoVenda { get; set; }

    public decimal TotalBrutoVenda { get; set; }

    public decimal TotalDescontoVenda { get; set; }

    public decimal TotalLiquidoVenda { get; set; }

    public string StatusVenda { get; set; } = string.Empty;

    public int EmpresaID { get; set; }
}