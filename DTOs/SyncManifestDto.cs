namespace GVC.Mobile.DTOs;

public sealed class SyncManifestDto
{
    public string Versao { get; set; } = string.Empty;

    public DateTime DataGeracaoUtc { get; set; }

    public int QuantidadeProdutos { get; set; }
    public int QuantidadeClientes { get; set; }
    public int QuantidadeContasReceber { get; set; }

    public int QuantidadeImagens { get; set; }

    public int QuantidadeImagensPadrao { get; set; }

    public int QuantidadeImagensAusentes { get; set; }

    public int? EmpresaID { get; set; }
    //public int EmpresaSelecionadaID { get; set; }

    public string FormatoPacote { get; set; } = string.Empty;

    public int QuantidadeEmpresas { get; set; }

    public string Escopo { get; set; } = string.Empty;    
    
}