namespace GVC.Mobile.Models;

public sealed class DownloadPacoteResult
{
    public bool Sucesso { get; init; }

    public string? CaminhoArquivo { get; init; }

    public string? NomeArquivo { get; init; }

    public long TamanhoBytes { get; init; }

    public string? MensagemErro { get; init; }
}