using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GVC.Mobile.Models;
using GVC.Mobile.Repositories.Interfaces;
using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.ViewModels;

public partial class ContasClienteViewModel : ObservableObject
{
    private readonly IContaReceberRepository _repository;
    private readonly IAppSettingsService _settingsService;

    private List<ContaReceber> _contasCache = [];

    public ObservableCollection<ContaReceber> Contas { get; } = [];

    public IReadOnlyList<string> FiltrosStatus { get; } =
    [
        "Todas",
        "Em aberto",
        "Atrasadas",
        "Pagas"
    ];

    [ObservableProperty]
    private ClienteContaResumo? _cliente;

    [ObservableProperty]
    private string _filtroSelecionado = "Todas";

    [ObservableProperty]
    private bool _estaCarregando;

    [ObservableProperty]
    private decimal _totalParcelas;

    [ObservableProperty]
    private decimal _totalRecebido;

    [ObservableProperty]
    private decimal _saldoEmAberto;

    [ObservableProperty]
    private decimal _saldoVencido;

    [ObservableProperty]
    private string _mensagemStatus = string.Empty;

    public ContasClienteViewModel(
        IContaReceberRepository repository,
        IAppSettingsService settingsService)
    {
        _repository = repository;
        _settingsService = settingsService;
    }

    public async Task CarregarAsync(
        ClienteContaResumo cliente)
    {
        try
        {
            EstaCarregando = true;
            Cliente = cliente;

            var settings =
                _settingsService.Obter();

            var empresaId =
                settings.EmpresaID;

            if (empresaId <= 0)
            {
                throw new InvalidOperationException(
                    "Nenhuma empresa válida foi selecionada nas configurações.");
            }

            _contasCache =
                await _repository
                    .PesquisarPorClienteAsync(
                        cliente.ClienteID,
                        empresaId);

            AtualizarResumo();

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            Contas.Clear();

            MensagemStatus =
                $"Não foi possível carregar as parcelas: {ex.Message}";
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    partial void OnFiltroSelecionadoChanged(
        string value)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        IEnumerable<ContaReceber> resultado =
            _contasCache;

        resultado = FiltroSelecionado switch
        {
            "Em aberto" =>
                resultado.Where(
                    conta =>
                        conta.Saldo > 0 &&
                        !EhPaga(conta) &&
                        !EhCancelada(conta)),

            "Atrasadas" =>
                resultado.Where(EhAtrasada),

            "Pagas" =>
                resultado.Where(EhPaga),

            _ => resultado
        };

        Contas.Clear();

        foreach (var conta in resultado
                     .OrderBy(item => item.DataVencimento)
                     .ThenBy(item => item.NumeroParcela))
        {
            Contas.Add(conta);
        }

        MensagemStatus = Contas.Count switch
        {
            0 => "Nenhuma parcela para o filtro selecionado.",
            1 => "1 parcela exibida.",
            _ => $"{Contas.Count:N0} parcelas exibidas."
        };
    }

    private void AtualizarResumo()
    {
        var contasValidas = _contasCache
            .Where(conta => !EhCancelada(conta))
            .ToList();

        TotalParcelas =
            contasValidas.Sum(
                conta => conta.ValorParcela);

        TotalRecebido =
            contasValidas.Sum(
                conta => conta.ValorRecebido);

        SaldoEmAberto =
            contasValidas
                .Where(conta =>
                    conta.Saldo > 0 &&
                    !EhPaga(conta))
                .Sum(conta => conta.Saldo);

        SaldoVencido =
            contasValidas
                .Where(EhAtrasada)
                .Sum(conta => conta.Saldo);
    }

    private static bool EhAtrasada(
        ContaReceber conta)
    {
        return conta.Saldo > 0 &&
               !EhPaga(conta) &&
               !EhCancelada(conta) &&
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
        ContaReceber conta)
    {
        return string.Equals(
            conta.StatusParcela,
            "Pago",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhCancelada(
        ContaReceber conta)
    {
        return string.Equals(
                   conta.StatusParcela,
                   "Cancelada",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   conta.StatusParcela,
                   "Cancelado",
                   StringComparison.OrdinalIgnoreCase);
    }
}