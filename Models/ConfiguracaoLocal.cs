using SQLite;

namespace GVC.Mobile.Models;

[Table("Configuracoes")]
public sealed class ConfiguracaoLocal
{
    [PrimaryKey]
    [MaxLength(100)]
    public string Chave { get; set; } = string.Empty;

    public string? Valor { get; set; }
}