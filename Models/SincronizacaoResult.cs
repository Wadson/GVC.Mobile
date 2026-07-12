namespace GVC.Mobile.Models;

public sealed class SincronizacaoResult
{
    public bool Sucesso { get; init; }

    public string Mensagem { get; init; } = string.Empty;

    public string? Versao { get; init; }

    public int QuantidadeProdutos { get; init; }

    public int QuantidadeImagens { get; init; }

    public int QuantidadeImagensAusentes { get; init; }

    public DateTime? DataGeracaoUtc { get; init; }
}