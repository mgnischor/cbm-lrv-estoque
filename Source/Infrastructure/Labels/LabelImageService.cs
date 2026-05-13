using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CEB.Domain.Aggregates;
using CEB.Domain.Entities;

namespace CEB.Infrastructure.Labels;

/// <summary>
/// Serviço de geração de etiquetas (imagens PNG) compactas para impressoras
/// térmicas, voltado a produtos, endereços de armazenagem e lotes com
/// controle de validade.
/// </summary>
/// <remarks>
/// As etiquetas são monocromáticas (preto sobre branco) e dimensionadas para
/// o formato 50 × 30 mm — tamanho de bobina mais comum em impressoras
/// térmicas de transferência (Argox, Zebra, Elgin, Bematech). A resolução de
/// saída é 203 DPI (8 dots/mm), padrão dessa classe de impressoras.
/// <para>
/// O código de barras utilizado é o <b>Code 128</b> (ISO/IEC 15417), base do
/// padrão GS1-128 adotado em rastreabilidade (NBR 15486), endereçamento
/// (NBR 14937) e controle de validade. A zona de silêncio horizontal mínima
/// equivale a 10 módulos em cada lado do código.
/// </para>
/// </remarks>
public class LabelImageService
{
    // ── Dimensões físicas da etiqueta (mm) ────────────────────────────────

    /// <summary>Largura da etiqueta em milímetros.</summary>
    private const double WidthMm = 50;

    /// <summary>Altura da etiqueta em milímetros.</summary>
    private const double HeightMm = 30;

    /// <summary>Margem interna em milímetros.</summary>
    private const double MarginMm = 1.2;

    /// <summary>Resolução de saída em pontos por polegada (térmica padrão).</summary>
    private const double Dpi = 203;

    /// <summary>Conversão de milímetros para px lógicos do WPF (96 dpi).</summary>
    private const double MmToPx = 96.0 / 25.4;

    // ── Dimensões em unidades lógicas ─────────────────────────────────────

    private static readonly double LabelW = WidthMm * MmToPx;
    private static readonly double LabelH = HeightMm * MmToPx;
    private static readonly double M = MarginMm * MmToPx;

    // ── Tipografia ────────────────────────────────────────────────────────

    /// <summary>Família tipográfica padrão (sans-serif).</summary>
    private static readonly Typeface Sans = new(
        new FontFamily("Arial"),
        FontStyles.Normal,
        FontWeights.Normal,
        FontStretches.Normal
    );

    /// <summary>Família tipográfica padrão em negrito.</summary>
    private static readonly Typeface SansBold = new(
        new FontFamily("Arial"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal
    );

    /// <summary>Fonte monoespaçada usada no texto interpretativo do código de barras.</summary>
    private static readonly Typeface Mono = new(
        new FontFamily("Consolas"),
        FontStyles.Normal,
        FontWeights.Normal,
        FontStretches.Normal
    );

    // ══════════════════════════════════════════════════════════════════════
    // API PÚBLICA
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera a etiqueta de identificação de um produto e grava em
    /// <paramref name="path"/> como arquivo PNG.
    /// </summary>
    public void GerarEtiquetaProduto(Produto produto, string path)
    {
        ArgumentNullException.ThrowIfNull(produto);
        if (string.IsNullOrWhiteSpace(produto.Codigo))
            throw new ArgumentException("Produto sem código.", nameof(produto));

        RenderLabel(
            path,
            "PRODUTO",
            dc =>
            {
                // Código grande, centralizado
                DrawText(dc, produto.Codigo, SansBold, 14, M, M + 8, LabelW - 2 * M, center: true);
                // Nome truncado, menor
                DrawText(
                    dc,
                    Truncate(produto.Nome, 32),
                    Sans,
                    7,
                    M,
                    M + 26,
                    LabelW - 2 * M,
                    center: true
                );
                // Linha auxiliar: UN · Categoria · Patrimônio
                var aux = JoinAux(produto.Unidade, produto.Categoria, produto.Patrimonio);
                if (!string.IsNullOrEmpty(aux))
                    DrawText(dc, aux, Sans, 6, M, M + 38, LabelW - 2 * M, center: true);
            },
            produto.Codigo
        );
    }

    /// <summary>
    /// Gera a etiqueta de endereçamento de armazenagem e grava em
    /// <paramref name="path"/> como arquivo PNG.
    /// </summary>
    public void GerarEtiquetaEndereco(Endereco endereco, string path)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        RenderLabel(
            path,
            "ENDEREÇO",
            dc =>
            {
                // Código completo bem grande
                DrawText(dc, endereco.Codigo, SansBold, 22, M, M + 8, LabelW - 2 * M, center: true);
                // Legenda hierárquica
                var leg =
                    $"SET {Dash(endereco.Setor)}  RUA {Dash(endereco.Rua)}  "
                    + $"COL {Dash(endereco.Coluna)}  NV {Dash(endereco.Nivel)}";
                DrawText(dc, leg, Sans, 6, M, M + 36, LabelW - 2 * M, center: true);
            },
            endereco.Codigo
        );
    }

    /// <summary>
    /// Gera a etiqueta de controle de validade de um lote e grava em
    /// <paramref name="path"/> como arquivo PNG.
    /// </summary>
    public void GerarEtiquetaValidade(LoteProduto lote, string path)
    {
        ArgumentNullException.ThrowIfNull(lote);

        var barcode = $"{lote.ProdutoCodigo}-{lote.Lote}";

        RenderLabel(
            path,
            "VALIDADE",
            dc =>
            {
                // Linha 1: produto (truncado)
                DrawText(
                    dc,
                    Truncate(lote.ProdutoNome, 28),
                    SansBold,
                    8,
                    M,
                    M + 8,
                    LabelW - 2 * M,
                    center: true
                );
                // Linha 2: lote + qtd
                var qtd = lote.Quantidade.ToString("0.###", CultureInfo.InvariantCulture);
                DrawText(
                    dc,
                    $"LOTE {lote.Lote}   QTD {qtd} {lote.ProdutoUnidade}".Trim(),
                    Sans,
                    7,
                    M,
                    M + 18,
                    LabelW - 2 * M,
                    center: true
                );
                // Linha 3: FAB e VAL lado a lado (negrito)
                var datas =
                    $"FAB {lote.DataFabricacao:dd/MM/yy}   VAL {lote.DataValidade:dd/MM/yy}";
                DrawText(dc, datas, SansBold, 9, M, M + 28, LabelW - 2 * M, center: true);
                // Linha 4: status (somente se vencido / a vencer)
                if (lote.Status != StatusValidade.Ok)
                {
                    var st =
                        lote.Status == StatusValidade.Vencido ? "*** VENCIDO ***" : "* A VENCER *";
                    DrawText(dc, st, SansBold, 7, M, M + 40, LabelW - 2 * M, center: true);
                }
            },
            barcode
        );
    }

    // ══════════════════════════════════════════════════════════════════════
    // Renderização base
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renderiza a estrutura comum da etiqueta (cabeçalho compacto, conteúdo
    /// e código de barras na base) e grava o PNG em <paramref name="path"/>.
    /// </summary>
    private static void RenderLabel(
        string path,
        string tipo,
        Action<DrawingContext> drawBody,
        string barcodeText
    )
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Caminho de destino inválido.", nameof(path));

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Fundo branco
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, LabelW, LabelH));

            // Cabeçalho enxuto: "CBM-LRV · <TIPO>"
            DrawText(
                dc,
                $"CBM-LRV · {tipo}",
                SansBold,
                6,
                M,
                M - 0.5,
                LabelW - 2 * M,
                center: true
            );
            // Linha separadora
            var sepY = M + 6;
            dc.DrawLine(
                new Pen(Brushes.Black, 0.5),
                new Point(M, sepY),
                new Point(LabelW - M, sepY)
            );

            // Conteúdo específico
            drawBody(dc);

            // Código de barras na base (10 mm de altura)
            var barH = 10 * MmToPx;
            var barY = LabelH - M - barH;
            DrawBarcode(dc, barcodeText, M, barY, LabelW - 2 * M, barH);
        }

        // Conversão para bitmap em 203 DPI (impressora térmica padrão)
        var scale = Dpi / 96.0;
        var pixelW = (int)Math.Ceiling(LabelW * scale);
        var pixelH = (int)Math.Ceiling(LabelH * scale);
        var rtb = new RenderTargetBitmap(pixelW, pixelH, Dpi, Dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        encoder.Save(fs);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Primitivas de desenho
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Desenha texto utilizando <see cref="FormattedText"/>.</summary>
    private static void DrawText(
        DrawingContext dc,
        string text,
        Typeface tf,
        double size,
        double x,
        double y,
        double maxWidth,
        bool center = false
    )
    {
        var ft = new FormattedText(
            text ?? string.Empty,
            CultureInfo.GetCultureInfo("pt-BR"),
            FlowDirection.LeftToRight,
            tf,
            size,
            Brushes.Black,
            1.0
        )
        {
            MaxTextWidth = maxWidth,
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        var dx = center ? x + (maxWidth - ft.Width) / 2.0 : x;
        dc.DrawText(ft, new Point(dx, y));
    }

    /// <summary>
    /// Desenha um código de barras Code 128 ajustado à área disponível, com
    /// largura de módulo mínima compatível com impressão térmica a 203 DPI,
    /// e imprime o texto interpretativo logo abaixo.
    /// </summary>
    private static void DrawBarcode(
        DrawingContext dc,
        string text,
        double x,
        double y,
        double maxWidth,
        double height
    )
    {
        var modules = Code128.Encode(text);

        // Largura disponível com pequena folga lateral
        var availableWidth = maxWidth - 2;
        // Largura ideal do módulo: 1 dot a 203 DPI ≈ 0,125 mm.
        // Mantemos pelo menos ~0,17 mm (≈ 1,4 dots) para impressão confiável,
        // mas reduzimos proporcionalmente se o conteúdo não couber.
        var moduleW = availableWidth / modules.Length;
        var minModulePx = 0.17 * MmToPx;
        if (moduleW > minModulePx + 0.6)
            moduleW = minModulePx + 0.6;

        var barsWidth = modules.Length * moduleW;
        var startX = x + (maxWidth - barsWidth) / 2.0;
        var barH = height - 6; // espaço para texto interpretativo

        for (int i = 0; i < modules.Length; i++)
        {
            if (!modules[i])
                continue;
            dc.DrawRectangle(
                Brushes.Black,
                null,
                new Rect(startX + i * moduleW, y, moduleW + 0.05, barH)
            );
        }
        // Texto interpretativo centralizado
        DrawText(dc, text, Mono, 6, x, y + barH + 0.5, maxWidth, center: true);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Auxiliares
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Trunca <paramref name="s"/> em <paramref name="max"/> caracteres.</summary>
    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    /// <summary>Devolve <c>—</c> quando vazio, ou o valor original.</summary>
    private static string Dash(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s;

    /// <summary>Junta partes não vazias com <c> · </c>.</summary>
    private static string JoinAux(params string?[] parts) =>
        string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
