using GVC.Mobile.Services.Interfaces;

namespace GVC.Mobile.Views;

public partial class TesteApiPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ISincronizacaoService _sincronizacaoService;

    private CancellationTokenSource? _downloadCancellation;

    public TesteApiPage( IApiService apiService, ISincronizacaoService sincronizacaoService)
    {
        InitializeComponent();

        _apiService = apiService;
        _sincronizacaoService = sincronizacaoService;
    }

    private async void btnTestarConexao_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            AlterarEstadoBotoes(false);

            lblResultado.Text =
                "Verificando comunicação com a API...";

            var conectado =
                await _apiService.VerificarConectividadeAsync();

            lblResultado.Text = conectado
                ? "Conexão com a API realizada com sucesso."
                : "Não foi possível acessar a API.";
        }
        catch (Exception ex)
        {
            lblResultado.Text =
                $"Erro ao testar a conexão: {ex.Message}";
        }
        finally
        {
            AlterarEstadoBotoes(true);
        }
    }
    private static string ObterMensagemEtapa( double progresso)
    {
        return progresso switch
        {
            < 0.05 =>
                "Verificando o servidor...",

            < 0.50 =>
                "Baixando o pacote...",

            < 0.70 =>
                "Extraindo e validando os arquivos...",

            < 0.80 =>
                "Preparando as imagens...",

            < 0.93 =>
                "Atualizando o banco local...",

            < 1 =>
                "Finalizando a sincronização...",

            _ =>
                "Sincronização concluída."
        };
    }
    private async void btnBaixarPacote_Clicked( object sender, EventArgs e)
    {
        try
        {
            AlterarEstadoBotoes(false);

            progressDownload.Progress = 0;
            progressDownload.IsVisible = true;

            lblPercentual.Text = "0%";
            lblResultado.Text =
                "Iniciando sincronização...";

            _downloadCancellation =
                new CancellationTokenSource();

            var progresso = new Progress<double>(
                valor =>
                {
                    progressDownload.Progress = valor;

                    lblPercentual.Text =
                        $"{valor:P0}";

                    lblResultado.Text =
                        ObterMensagemEtapa(valor);
                });

            var resultado =
                await _sincronizacaoService.SincronizarAsync(
                    progresso,
                    _downloadCancellation.Token);

            if (!resultado.Sucesso)
            {
                lblResultado.Text =
                    resultado.Mensagem;

                return;
            }

            lblResultado.Text =
                $"Sincronização concluída.\n\n" +
                $"Versão: {resultado.Versao}\n" +
                $"Produtos: {resultado.QuantidadeProdutos}\n" +
                $"Imagens: {resultado.QuantidadeImagens}\n" +
                $"Imagens ausentes: {resultado.QuantidadeImagensAusentes}";
        }
        catch (Exception ex)
        {
            lblResultado.Text =
                $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;

            AlterarEstadoBotoes(true);
        }
    }

    private void AlterarEstadoBotoes(
        bool habilitado)
    {
        btnTestarConexao.IsEnabled = habilitado;
        btnBaixarPacote.IsEnabled = habilitado;
    }
}