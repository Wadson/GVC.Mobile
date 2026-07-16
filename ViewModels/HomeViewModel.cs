using CommunityToolkit.Mvvm.ComponentModel;
using GVC.Mobile.Data;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ISincronizacaoService
        _sincronizacaoService;

    private readonly IProdutoRepository
        _produtoRepository;

    private readonly IEmpresaRepository
        _empresaRepository;

    private readonly IAppSettingsService
        _settingsService;

    private readonly DatabaseService
        _databaseService;

    private CancellationTokenSource?
        _sincronizacaoCancellation;

    [ObservableProperty]
    private bool _estaSincronizando;
    public bool PodeSincronizar => !EstaSincronizando;

    [ObservableProperty]
    private double _progresso;

    [ObservableProperty]
    private string _percentual = string.Empty;

    [ObservableProperty]
    private string _mensagemSincronizacao =
        "Dados disponíveis para consulta offline.";

    [ObservableProperty]
    private string _ultimaSincronizacao =
        "Nunca sincronizado";

    [ObservableProperty]
    private string _versaoSincronizacao = "-";

    [ObservableProperty]
    private int _quantidadeProdutos;

    [ObservableProperty]
    private int _quantidadeImagens;

    [ObservableProperty]
    private string _nomeEmpresaSelecionada = "GVC Mobile";

    [ObservableProperty]
    private ImageSource? _logoEmpresaSelecionada;

    public HomeViewModel(
        ISincronizacaoService sincronizacaoService,
        IProdutoRepository produtoRepository,
        IEmpresaRepository empresaRepository,
        IAppSettingsService settingsService,
        DatabaseService databaseService)
    {
        _sincronizacaoService =
            sincronizacaoService;

        _produtoRepository =
            produtoRepository;

        _empresaRepository =
            empresaRepository;

        _settingsService =
            settingsService;

        _databaseService =
            databaseService;
    }
    partial void OnEstaSincronizandoChanged( bool value)
    {
        OnPropertyChanged( nameof(PodeSincronizar));
    }
    public async Task CarregarResumoAsync()
    {
        try
        {
            var settings = _settingsService.Obter();

            var empresaSelecionada = settings.EmpresaID > 0
                ? await _empresaRepository.ObterPorIdAsync(
                    settings.EmpresaID)
                : null;

            NomeEmpresaSelecionada =
                empresaSelecionada?.NomeExibicao
                ?? "GVC Mobile";

            var logo = empresaSelecionada?.Logo;

            LogoEmpresaSelecionada = logo is { Length: > 0 }
                ? ImageSource.FromStream(
                    () => new MemoryStream(logo))
                : null;

            QuantidadeProdutos =
                await _produtoRepository.ContarAsync();

            var database =
                await _databaseService
                    .ObterConexaoAsync();

            var configuracoes =
                await database
                    .Table<ConfiguracaoLocal>()
                    .ToListAsync();

            string? ObterValor(string chave)
            {
                return configuracoes
                    .FirstOrDefault(
                        item => item.Chave == chave)?
                    .Valor;
            }

            VersaoSincronizacao =
                ObterValor(
                    "UltimaVersaoSincronizada")
                ?? "-";

            var ultimaUtc =
                ObterValor(
                    "UltimaSincronizacaoUtc");

            if (DateTime.TryParse(
                    ultimaUtc,
                    out var dataUtc))
            {
                UltimaSincronizacao =
                    dataUtc
                        .ToLocalTime()
                        .ToString(
                            "dd/MM/yyyy HH:mm");
            }
            else
            {
                UltimaSincronizacao =
                    "Nunca sincronizado";
            }

            var quantidadeImagens =
                ObterValor(
                    "QuantidadeImagens");

            _ = int.TryParse(
                quantidadeImagens,
                out var imagens);

            QuantidadeImagens = imagens;
        }
        catch (Exception ex)
        {
            MensagemSincronizacao =
                $"Não foi possível carregar o resumo: " +
                $"{ex.Message}";
        }
    }

    public async Task<SincronizacaoResult>
        SincronizarAsync()
    {
        if (EstaSincronizando)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem =
                    "Já existe uma sincronização em andamento."
            };
        }

        try
        {
            EstaSincronizando = true;
            Progresso = 0;
            Percentual = "0%";
            MensagemSincronizacao =
                "Preparando a sincronização...";

            _sincronizacaoCancellation =
                new CancellationTokenSource();

            var progresso =
                new Progress<double>(
                    valor =>
                    {
                        Progresso = valor;
                        Percentual = $"{valor:P0}";
                        MensagemSincronizacao =
                            ObterMensagemEtapa(valor);
                    });

            var resultado =
                await _sincronizacaoService
                    .SincronizarAsync(
                        progresso,
                        _sincronizacaoCancellation.Token);

            MensagemSincronizacao =
                resultado.Mensagem;

            if (resultado.Sucesso)
            {
                await CarregarResumoAsync();
            }

            return resultado;
        }
        finally
        {
            _sincronizacaoCancellation?.Dispose();
            _sincronizacaoCancellation = null;

            EstaSincronizando = false;
        }
    }

    public void CancelarSincronizacao()
    {
        _sincronizacaoCancellation?.Cancel();
    }

    private static string ObterMensagemEtapa(
        double progresso)
    {
        return progresso switch
        {
            < 0.05 =>
                "Verificando o servidor...",

            < 0.50 =>
                "Baixando produtos e imagens...",

            < 0.70 =>
                "Extraindo e validando o pacote...",

            < 0.80 =>
                "Preparando as imagens...",

            < 0.93 =>
                "Atualizando o banco local...",

            < 1 =>
                "Finalizando...",

            _ =>
                "Sincronização concluída."
        };
    }
}
