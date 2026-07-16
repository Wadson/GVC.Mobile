using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.ViewModels;

public partial class ClientesContasViewModel : ObservableObject
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaReceberRepository _contaRepository;
    private readonly IAppSettingsService _settingsService;

    private List<ClienteContaResumo> _resumosCache = [];
    private CancellationTokenSource? _pesquisaCancellation;

    public ObservableCollection<ClienteContaResumo> Clientes { get; } = [];

    [ObservableProperty]
    private string _termoPesquisa = string.Empty;

    [ObservableProperty]
    private bool _estaCarregando;

    [ObservableProperty]
    private string _mensagemStatus =
        "Carregando contas a receber...";

    [ObservableProperty]
    private string _resumoGeral = string.Empty;

    [ObservableProperty]
    private decimal _totalEmAberto;

    [ObservableProperty]
    private decimal _totalVencido;

    [ObservableProperty]
    private int _quantidadeClientes;

    [ObservableProperty]
    private ClienteContaResumo? _clienteSelecionado;

    public bool PodeLimparPesquisa =>
        !string.IsNullOrWhiteSpace(TermoPesquisa);

    public ClientesContasViewModel(
        IClienteRepository clienteRepository,
        IContaReceberRepository contaRepository,
        IAppSettingsService settingsService)
    {
        _clienteRepository = clienteRepository;
        _contaRepository = contaRepository;
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
                "Carregando clientes e parcelas...";

            var settings =
                _settingsService.Obter();

            var empresaId =
                settings.EmpresaID;

            if (empresaId <= 0)
            {
                throw new InvalidOperationException(
                    "Nenhuma empresa válida foi selecionada nas configurações.");
            }

            var clientesTask =
                _clienteRepository.ObterTodosAsync(
                    empresaId);

            var contasTask =
                _contaRepository.ObterTodasAsync(
                    empresaId);

            await Task.WhenAll(
                clientesTask,
                contasTask);

            var clientes =
                await clientesTask;

            var contas =
                await contasTask;

            var contasPorCliente = contas
                .GroupBy(conta => conta.ClienteID)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.ToList());

            _resumosCache = [];

            foreach (var cliente in clientes)
            {
                if (!contasPorCliente.TryGetValue(
                        cliente.ClienteID,
                        out var contasCliente))
                {
                    continue;
                }

                _resumosCache.Add(
                    CriarResumo(
                        cliente,
                        contasCliente));
            }

            _resumosCache = _resumosCache
                .OrderBy(item => item.Nome)
                .ToList();

            AtualizarResumoGeral();

            AplicarFiltro(
                TermoPesquisa);
        }
        catch (Exception ex)
        {
            Clientes.Clear();

            QuantidadeClientes = 0;
            TotalEmAberto = 0;
            TotalVencido = 0;
            ResumoGeral = string.Empty;

            MensagemStatus =
                $"Não foi possível carregar as contas: {ex.Message}";
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    [RelayCommand]
    private Task LimparPesquisaAsync()
    {
        TermoPesquisa = string.Empty;

        AplicarFiltro(
            string.Empty);

        return Task.CompletedTask;
    }

    partial void OnTermoPesquisaChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(PodeLimparPesquisa));

        _ = PesquisarComAtrasoAsync(
            value);
    }

    private async Task PesquisarComAtrasoAsync(
        string termo)
    {
        _pesquisaCancellation?.Cancel();
        _pesquisaCancellation?.Dispose();

        _pesquisaCancellation =
            new CancellationTokenSource();

        try
        {
            await Task.Delay(
                300,
                _pesquisaCancellation.Token);

            AplicarFiltro(
                termo);
        }
        catch (OperationCanceledException)
        {
            // Nova digitação substituiu a pesquisa anterior.
        }
    }

    private void AplicarFiltro(
        string? termo)
    {
        var termoNormalizado =
            SomenteNumerosOuTexto(
                termo);

        IEnumerable<ClienteContaResumo> resultado =
            _resumosCache;

        if (!string.IsNullOrWhiteSpace(
                termoNormalizado))
        {
            resultado = _resumosCache.Where(
                item =>
                    Contem(
                        item.Nome,
                        termoNormalizado)
                    ||
                    Contem(
                        item.Cpf,
                        termoNormalizado)
                    ||
                    Contem(
                        item.Cnpj,
                        termoNormalizado)
                    ||
                    Contem(
                        item.Telefone,
                        termoNormalizado)
                    ||
                    item.ClienteID.ToString() ==
                    termoNormalizado);
        }

        Clientes.Clear();

        foreach (var cliente in resultado.Take(200))
        {
            Clientes.Add(
                cliente);
        }

        MensagemStatus = Clientes.Count switch
        {
            0 =>
                "Nenhum cliente encontrado.",

            1 =>
                "1 cliente encontrado.",

            _ =>
                $"{Clientes.Count:N0} clientes encontrados."
        };
    }

    private void AtualizarResumoGeral()
    {
        QuantidadeClientes =
            _resumosCache.Count;

        TotalEmAberto =
            _resumosCache.Sum(
                item => item.SaldoEmAberto);

        TotalVencido =
            _resumosCache.Sum(
                item => item.SaldoVencido);

        ResumoGeral =
            $"{QuantidadeClientes:N0} cliente(s) com movimentação financeira";
    }

    private static ClienteContaResumo CriarResumo(
        Cliente cliente,
        IReadOnlyCollection<ContaReceber> contas)
    {
        var contasValidas = contas
            .Where(conta =>
                !EhCancelada(
                    conta.StatusParcela))
            .ToList();

        var contasEmAberto = contasValidas
            .Where(conta =>
                conta.Saldo > 0 &&
                !EhPaga(
                    conta.StatusParcela))
            .ToList();

        var contasAtrasadas = contasEmAberto
            .Where(EhAtrasada)
            .ToList();

        return new ClienteContaResumo
        {
            ClienteID =
                cliente.ClienteID,

            Nome =
                cliente.Nome,

            Cpf =
                cliente.Cpf,

            Cnpj =
                cliente.Cnpj,

            Telefone =
                cliente.Telefone,

            QuantidadeParcelas =
                contasValidas.Count,

            QuantidadeAtrasadas =
                contasAtrasadas.Count,

            TotalParcelas =
                contasValidas.Sum(
                    conta => conta.ValorParcela),

            TotalRecebido =
                contasValidas.Sum(
                    conta => conta.ValorRecebido),

            SaldoEmAberto =
                contasEmAberto.Sum(
                    conta => conta.Saldo),

            SaldoVencido =
                contasAtrasadas.Sum(
                    conta => conta.Saldo)
        };
    }

    private static bool EhAtrasada(
        ContaReceber conta)
    {
        return conta.Saldo > 0 &&
               !EhPaga(
                   conta.StatusParcela) &&
               !EhCancelada(
                   conta.StatusParcela) &&
               (
                   string.Equals(
                       conta.StatusParcela,
                       "Atrasada",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   conta.DataVencimento.Date <
                   DateTime.Today
               );
    }

    private static bool EhPaga(
        string? status)
    {
        return string.Equals(
            status,
            "Pago",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhCancelada(
        string? status)
    {
        return string.Equals(
                   status,
                   "Cancelada",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   status,
                   "Cancelado",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contem(
        string? valor,
        string termo)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return false;
        }

        var valorNormalizado =
            SomenteNumerosOuTexto(
                valor);

        return valorNormalizado.Contains(
            termo,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string SomenteNumerosOuTexto(
        string? valor)
    {
        return valor?
            .Trim()
            .Replace(".", "")
            .Replace("-", "")
            .Replace("/", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(" ", "")
            ?? string.Empty;
    }
}