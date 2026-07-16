using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.ViewModels;

public partial class ProdutosViewModel : ObservableObject
{
    private readonly IProdutoRepository _produtoRepository;
    private CancellationTokenSource? _pesquisaCancellation;
    private readonly IAppSettingsService  _settingsService;
    [ObservableProperty]
    private IReadOnlyList<Produto> _produtos = [];

    [ObservableProperty]
    private string _termoPesquisa = string.Empty;

    [ObservableProperty]
    private bool _estaCarregando;

    [ObservableProperty]
    private bool _possuiProdutos;

    [ObservableProperty]
    private string _mensagemStatus =
        "Carregando produtos...";

    [ObservableProperty]
    private string _resumoProdutos = string.Empty;

    [ObservableProperty]
    private Produto? _produtoSelecionado;

    public ProdutosViewModel(  IProdutoRepository produtoRepository, IAppSettingsService settingsService)
    {
        _produtoRepository = produtoRepository;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        if (EstaCarregando)
            return;

        try
        {
            EstaCarregando = true;

            MensagemStatus =
                "Carregando produtos...";

            await ExecutarPesquisaAsync(
                TermoPesquisa);

            var total =
                await _produtoRepository.ContarAsync();

            ResumoProdutos = total == 1
                ? "1 produto disponível offline"
                : $"{total:N0} produtos disponíveis offline";
        }
        catch (Exception ex)
        {
            PossuiProdutos = false;

            MensagemStatus =
                $"Não foi possível carregar os produtos: {ex.Message}";
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    [RelayCommand]
    private async Task LimparPesquisaAsync()
    {
        TermoPesquisa = string.Empty;

        await ExecutarPesquisaAsync(
            string.Empty);
    }

    partial void OnTermoPesquisaChanged( string value)
    {
        _ = PesquisarComAtrasoAsync(value);
    }

    private async Task PesquisarComAtrasoAsync( string termo)
    {
        _pesquisaCancellation?.Cancel();
        _pesquisaCancellation?.Dispose();

        _pesquisaCancellation =  new CancellationTokenSource();

        try
        {
            await Task.Delay(
                350,
                _pesquisaCancellation.Token);

            await ExecutarPesquisaAsync(
                termo,
                _pesquisaCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Nova digitação cancelou a pesquisa anterior.
        }
    }

    private async Task ExecutarPesquisaAsync( string? termo,  CancellationToken cancellationToken = default)
    {
        try
        {
            EstaCarregando = true;

            var settings =  _settingsService.Obter();

            var produtos = await _produtoRepository.PesquisarAsync(
                    termo,
                    settings.EmpresaID,
                    200);

            cancellationToken.ThrowIfCancellationRequested();

            // Publica a lista inteira de uma vez. Isso evita centenas de eventos
            // CollectionChanged e recriações de layout na abertura da tela.
            Produtos = produtos;

            PossuiProdutos =
                Produtos.Count > 0;

            MensagemStatus =
                CriarMensagemResultado(
                    termo,
                    Produtos.Count);
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    private static string CriarMensagemResultado( string? termo, int quantidade)
    {
        if (quantidade == 0)
        {
            return string.IsNullOrWhiteSpace(termo)
                ? "Nenhum produto foi sincronizado."
                : "Nenhum produto encontrado para a pesquisa.";
        }

        if (string.IsNullOrWhiteSpace(termo))
        {
            return quantidade == 1
                ? "1 produto exibido"
                : $"{quantidade:N0} produtos exibidos";
        }

        return quantidade == 1
            ? "1 resultado encontrado"
            : $"{quantidade:N0} resultados encontrados";
    }
}
