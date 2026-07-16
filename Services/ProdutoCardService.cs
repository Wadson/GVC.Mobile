using System.Globalization;
using GVC.Mobile.Models;
using GVC.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace GVC.Mobile.Services;

public sealed class ProdutoCardService : IProdutoCardService
{
    private const int LarguraCard = 1080;
    private const int AlturaCard = 1350;

    private readonly record struct TextoStyle(SKFont Font, SKPaint Paint);

    private readonly ILogger<ProdutoCardService> _logger;

    public ProdutoCardService(ILogger<ProdutoCardService> logger)
    {
        _logger = logger;
    }

    public Task<string> GerarCardAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(produto);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string nomeArquivo = $"GVC_PRODUTO_{produto.ProdutoID}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string caminhoArquivo = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

            var imageInfo = new SKImageInfo(LarguraCard, AlturaCard, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(imageInfo);
            using var canvas = new SKCanvas(bitmap);

            DesenharFundo(canvas);
            DesenharCabecalho(canvas);
            DesenharImagemProduto(canvas, produto);
            DesenharDadosProduto(canvas, produto);
            DesenharRodape(canvas);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(caminhoArquivo);
            data.SaveTo(file);

            return Task.FromResult(caminhoArquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar card do produto {ProdutoID}.", produto.ProdutoID);
            throw;
        }
    }

    private static void DesenharFundo(SKCanvas canvas)
    {
        canvas.Clear(SKColor.Parse("#F3F5F7"));

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        canvas.DrawRoundRect(
            new SKRoundRect(new SKRect(55, 50, LarguraCard - 55, AlturaCard - 50), 36, 36),
            paint);
    }

    private static void DesenharCabecalho(SKCanvas canvas)
    {
        using var fundo = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.Parse("#173B57")
        };

        canvas.DrawRoundRect(
            new SKRoundRect(new SKRect(55, 50, LarguraCard - 55, 220), 36, 36),
            fundo);

        var titulo = CriarTexto(54, SKColors.White, bold: true);
        canvas.DrawText("GVC Mobile", 105, 130, SKTextAlign.Left, titulo.Font, titulo.Paint);

        var subtitulo = CriarTexto(27, SKColor.Parse("#D9E6EF"));
        canvas.DrawText("Consulta de preços e estoque", 105, 180, SKTextAlign.Left, subtitulo.Font, subtitulo.Paint);
    }

    private static void DesenharImagemProduto(SKCanvas canvas, Produto produto)
    {
        var areaImagem = new SKRect(105, 260, LarguraCard - 105, 740);

        using var fundo = new SKPaint
        {
            Color = SKColor.Parse("#F0F2F4"),
            IsAntialias = true
        };

        canvas.DrawRoundRect(new SKRoundRect(areaImagem, 28, 28), fundo);

        if (string.IsNullOrWhiteSpace(produto.ImagemLocal) || !File.Exists(produto.ImagemLocal))
        {
            DesenharSemImagem(canvas, areaImagem);
            return;
        }

        try
        {
            using var bitmap = SKBitmap.Decode(produto.ImagemLocal);
            if (bitmap == null)
            {
                DesenharSemImagem(canvas, areaImagem);
                return;
            }

            var destino = CalcularAreaAspectFit(bitmap.Width, bitmap.Height, areaImagem, 25);
            canvas.DrawBitmap(bitmap, destino);
        }
        catch
        {
            DesenharSemImagem(canvas, areaImagem);
        }
    }

    private static void DesenharSemImagem(SKCanvas canvas, SKRect areaImagem)
    {
        var estilo = CriarTexto(34, SKColor.Parse("#7C858D"), bold: true);
        canvas.DrawText(
            "PRODUTO SEM IMAGEM",
            areaImagem.MidX,
            areaImagem.MidY,
            SKTextAlign.Center,
            estilo.Font,
            estilo.Paint);
    }

    private static void DesenharDadosProduto(SKCanvas canvas, Produto produto)
    {
        var y = 810f;

        var nomeEstilo = CriarTexto(41, SKColor.Parse("#20252A"), bold: true);
        y = DesenharTextoQuebrado(
            canvas,
            produto.NomeProduto,
            nomeEstilo,
            x: 105,
            y,
            larguraMaxima: LarguraCard - 210,
            alturaLinha: 50,
            maximoLinhas: 3);

        y += 18;

        var rotuloEstilo = CriarTexto(25, SKColor.Parse("#68747D"));
        var valorEstilo = CriarTexto(28, SKColor.Parse("#20252A"), bold: true);

        // Referência
        canvas.DrawText("Referência", 105, y, SKTextAlign.Left, rotuloEstilo.Font, rotuloEstilo.Paint);
        canvas.DrawText(ValorOuTraco(produto.Referencia), 335, y, SKTextAlign.Left, valorEstilo.Font, valorEstilo.Paint);
        y += 50;

        // Marca
        canvas.DrawText("Marca", 105, y, SKTextAlign.Left, rotuloEstilo.Font, rotuloEstilo.Paint);
        canvas.DrawText(ValorOuTraco(produto.Marca), 335, y, SKTextAlign.Left, valorEstilo.Font, valorEstilo.Paint);
        y += 50;

        // Estoque
        canvas.DrawText("Estoque", 105, y, SKTextAlign.Left, rotuloEstilo.Font, rotuloEstilo.Paint);
        canvas.DrawText($"{produto.Estoque:N0} unidade(s)", 335, y, SKTextAlign.Left, valorEstilo.Font, valorEstilo.Paint);
        y += 82;

        // Bloco de preço
        using var precoFundo = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.Parse("#EAF7F0")
        };

        canvas.DrawRoundRect(
            new SKRoundRect(new SKRect(105, y - 45, LarguraCard - 105, y + 95), 24, 24),
            precoFundo);

        var precoRotulo = CriarTexto(25, SKColor.Parse("#4D7560"), bold: true);
        canvas.DrawText("PREÇO DE VENDA", 140, y, SKTextAlign.Left, precoRotulo.Font, precoRotulo.Paint);

        var precoValor = CriarTexto(57, SKColor.Parse("#16734A"), bold: true);
        var precoStr = produto.PrecoDeVenda.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
        canvas.DrawText(precoStr, LarguraCard - 140, y + 52, SKTextAlign.Right, precoValor.Font, precoValor.Paint);
    }

    private static void DesenharRodape(SKCanvas canvas)
    {
        using var linha = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.Parse("#DDE2E6"),
            StrokeWidth = 2
        };

        canvas.DrawLine(105, AlturaCard - 125, LarguraCard - 105, AlturaCard - 125, linha);

        var rodape = CriarTexto(22, SKColor.Parse("#7C858D"));
        canvas.DrawText(
            $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
            105,
            AlturaCard - 78,
            SKTextAlign.Left,
            rodape.Font,
            rodape.Paint);

        var sistema = CriarTexto(22, SKColor.Parse("#173B57"), bold: true);
        canvas.DrawText(
            "Sistema GVC",
            LarguraCard - 105,
            AlturaCard - 78,
            SKTextAlign.Right,
            sistema.Font,
            sistema.Paint);
    }

    private static TextoStyle CriarTexto(float tamanho, SKColor cor, bool bold = false)
    {
        var typeface = SKTypeface.FromFamilyName(
            null,
            bold ? SKFontStyle.Bold : SKFontStyle.Normal);

        return new TextoStyle(
            new SKFont(typeface, tamanho),
            new SKPaint
            {
                Color = cor,
                IsAntialias = true
            });
    }

    private static float DesenharTextoQuebrado(
        SKCanvas canvas,
        string? texto,
        TextoStyle estilo,
        float x,
        float y,
        float larguraMaxima,
        float alturaLinha,
        int maximoLinhas)
    {
        var palavras = (string.IsNullOrWhiteSpace(texto) ? "Produto sem descrição" : texto.Trim())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var linhaAtual = string.Empty;
        var linhasDesenhadas = 0;

        foreach (var palavra in palavras)
        {
            var teste = string.IsNullOrEmpty(linhaAtual) ? palavra : $"{linhaAtual} {palavra}";

            if (estilo.Font.MeasureText(teste) <= larguraMaxima)
            {
                linhaAtual = teste;
                continue;
            }

            canvas.DrawText(linhaAtual, x, y, SKTextAlign.Left, estilo.Font, estilo.Paint);
            y += alturaLinha;
            linhasDesenhadas++;

            if (linhasDesenhadas >= maximoLinhas)
                return y;

            linhaAtual = palavra;
        }

        if (!string.IsNullOrWhiteSpace(linhaAtual) && linhasDesenhadas < maximoLinhas)
        {
            canvas.DrawText(linhaAtual, x, y, SKTextAlign.Left, estilo.Font, estilo.Paint);
            y += alturaLinha;
        }

        return y;
    }

    private static SKRect CalcularAreaAspectFit(int larguraImagem, int alturaImagem, SKRect area, float margem)
    {
        var areaUtil = new SKRect(
            area.Left + margem,
            area.Top + margem,
            area.Right - margem,
            area.Bottom - margem);

        var escala = Math.Min(areaUtil.Width / larguraImagem, areaUtil.Height / alturaImagem);
        var largura = larguraImagem * escala;
        var altura = alturaImagem * escala;

        return new SKRect(
            areaUtil.MidX - largura / 2,
            areaUtil.MidY - altura / 2,
            areaUtil.MidX + largura / 2,
            areaUtil.MidY + altura / 2);
    }

    private static string ValorOuTraco(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
}