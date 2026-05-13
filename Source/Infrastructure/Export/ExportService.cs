using System.IO;
using System.Text;
using CEB.Domain.Aggregates;
using CEB.Domain.Entities;
using ClosedXML.Excel;

namespace CEB.Infrastructure.Export;

/// <summary>
/// Serviço responsável por exportar dados do sistema nos formatos CSV e XLSX.
/// </summary>
public class ExportService
{
    // ══════════════════════════════════════════════════════════════════════
    // CSV
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta os itens de estoque para um arquivo CSV codificado em UTF-8 com BOM.
    /// </summary>
    /// <param name="caminho">Caminho completo do arquivo de destino.</param>
    /// <param name="dados">Lista de itens de estoque a ser exportada.</param>
    public void ExportarCsvEstoque(string caminho, List<ItemEstoque> dados)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Código Produto,Nome Produto,Endereço,Quantidade,Unidade");
        foreach (var item in dados)
            sb.AppendLine(
                Linha(
                    item.ProdutoCodigo,
                    item.ProdutoNome,
                    item.EnderecoCode,
                    item.Quantidade,
                    item.ProdutoUnidade
                )
            );
        File.WriteAllText(
            caminho,
            sb.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
    }

    /// <summary>
    /// Exporta o cadastro de produtos para um arquivo CSV codificado em UTF-8 com BOM.
    /// </summary>
    /// <param name="caminho">Caminho completo do arquivo de destino.</param>
    /// <param name="dados">Lista de produtos a ser exportada.</param>
    public void ExportarCsvProdutos(string caminho, List<Produto> dados)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Código,Nome,Unidade,Categoria,Patrimônio,Descrição");
        foreach (var p in dados)
            sb.AppendLine(
                Linha(p.Codigo, p.Nome, p.Unidade, p.Categoria, p.Patrimonio, p.Descricao)
            );
        File.WriteAllText(
            caminho,
            sb.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
    }

    /// <summary>
    /// Exporta o cadastro de endereços para um arquivo CSV codificado em UTF-8 com BOM.
    /// </summary>
    /// <param name="caminho">Caminho completo do arquivo de destino.</param>
    /// <param name="dados">Lista de endereços a ser exportada.</param>
    public void ExportarCsvEnderecos(string caminho, List<Endereco> dados)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Código,Setor,Rua,Coluna,Nível");
        foreach (var e in dados)
            sb.AppendLine(Linha(e.Codigo, e.Setor, e.Rua, e.Coluna, e.Nivel));
        File.WriteAllText(
            caminho,
            sb.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
    }

    /// <summary>
    /// Exporta os lotes de produto para um arquivo CSV codificado em UTF-8 com BOM.
    /// </summary>
    /// <param name="caminho">Caminho completo do arquivo de destino.</param>
    /// <param name="dados">Lista de lotes a ser exportada.</param>
    public void ExportarCsvLotes(string caminho, List<LoteProduto> dados)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Código Produto,Nome Produto,Lote,Data Fabricação,Data Validade,Quantidade,Unidade,Status,Observação"
        );
        foreach (var l in dados)
            sb.AppendLine(
                Linha(
                    l.ProdutoCodigo,
                    l.ProdutoNome,
                    l.Lote,
                    l.DataFabricacao.ToString("dd/MM/yyyy"),
                    l.DataValidade.ToString("dd/MM/yyyy"),
                    l.Quantidade,
                    l.ProdutoUnidade,
                    l.Status.ToString(),
                    l.Observacao
                )
            );
        File.WriteAllText(
            caminho,
            sb.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
    }

    // ══════════════════════════════════════════════════════════════════════
    // XLSX
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exporta os dados selecionados para um arquivo XLSX com uma aba por conjunto de dados.
    /// </summary>
    /// <param name="caminho">Caminho completo do arquivo de destino (.xlsx).</param>
    /// <param name="estoque">Itens de estoque; <c>null</c> para não incluir a aba.</param>
    /// <param name="produtos">Produtos; <c>null</c> para não incluir a aba.</param>
    /// <param name="enderecos">Endereços; <c>null</c> para não incluir a aba.</param>
    /// <param name="lotes">Lotes; <c>null</c> para não incluir a aba.</param>
    public void ExportarXlsx(
        string caminho,
        List<ItemEstoque>? estoque,
        List<Produto>? produtos,
        List<Endereco>? enderecos,
        List<LoteProduto>? lotes
    )
    {
        using var wb = new XLWorkbook();
        if (estoque != null)
            AdicionarAbaEstoque(wb, estoque);
        if (produtos != null)
            AdicionarAbaProdutos(wb, produtos);
        if (enderecos != null)
            AdicionarAbaEnderecos(wb, enderecos);
        if (lotes != null)
            AdicionarAbaLotes(wb, lotes);
        wb.SaveAs(caminho);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Auxiliares privados
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converte um valor para string CSV, aplicando aspas duplas quando necessário.
    /// </summary>
    private static string EscapeCsv(object? val)
    {
        if (val is null)
            return string.Empty;
        var s =
            Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            s = $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    /// <summary>Formata uma linha CSV a partir de um array de valores.</summary>
    private static string Linha(params object?[] campos) =>
        string.Join(",", campos.Select(EscapeCsv));

    /// <summary>Aplica o estilo de cabeçalho azul com texto branco à linha indicada.</summary>
    private static void EstiloHeader(IXLRow row)
    {
        row.Style.Font.Bold = true;
        row.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
        row.Style.Font.FontColor = XLColor.White;
    }

    private static void AdicionarAbaEstoque(XLWorkbook wb, List<ItemEstoque> dados)
    {
        var ws = wb.Worksheets.Add("Estoque");
        ws.Cell(1, 1).Value = "Código Produto";
        ws.Cell(1, 2).Value = "Nome Produto";
        ws.Cell(1, 3).Value = "Endereço";
        ws.Cell(1, 4).Value = "Quantidade";
        ws.Cell(1, 5).Value = "Unidade";
        EstiloHeader(ws.Row(1));

        for (int r = 0; r < dados.Count; r++)
        {
            var i = dados[r];
            ws.Cell(r + 2, 1).Value = i.ProdutoCodigo;
            ws.Cell(r + 2, 2).Value = i.ProdutoNome;
            ws.Cell(r + 2, 3).Value = i.EnderecoCode;
            ws.Cell(r + 2, 4).Value = i.Quantidade;
            ws.Cell(r + 2, 5).Value = i.ProdutoUnidade;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaProdutos(XLWorkbook wb, List<Produto> dados)
    {
        var ws = wb.Worksheets.Add("Produtos");
        ws.Cell(1, 1).Value = "Código";
        ws.Cell(1, 2).Value = "Nome";
        ws.Cell(1, 3).Value = "Unidade";
        ws.Cell(1, 4).Value = "Categoria";
        ws.Cell(1, 5).Value = "Patrimônio";
        ws.Cell(1, 6).Value = "Descrição";
        EstiloHeader(ws.Row(1));

        for (int r = 0; r < dados.Count; r++)
        {
            var p = dados[r];
            ws.Cell(r + 2, 1).Value = p.Codigo;
            ws.Cell(r + 2, 2).Value = p.Nome;
            ws.Cell(r + 2, 3).Value = p.Unidade;
            ws.Cell(r + 2, 4).Value = p.Categoria;
            ws.Cell(r + 2, 5).Value = p.Patrimonio;
            ws.Cell(r + 2, 6).Value = p.Descricao;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaEnderecos(XLWorkbook wb, List<Endereco> dados)
    {
        var ws = wb.Worksheets.Add("Endereços");
        ws.Cell(1, 1).Value = "Código";
        ws.Cell(1, 2).Value = "Setor";
        ws.Cell(1, 3).Value = "Rua";
        ws.Cell(1, 4).Value = "Coluna";
        ws.Cell(1, 5).Value = "Nível";
        EstiloHeader(ws.Row(1));

        for (int r = 0; r < dados.Count; r++)
        {
            var e = dados[r];
            ws.Cell(r + 2, 1).Value = e.Codigo;
            ws.Cell(r + 2, 2).Value = e.Setor;
            ws.Cell(r + 2, 3).Value = e.Rua;
            ws.Cell(r + 2, 4).Value = e.Coluna;
            ws.Cell(r + 2, 5).Value = e.Nivel;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaLotes(XLWorkbook wb, List<LoteProduto> dados)
    {
        var ws = wb.Worksheets.Add("Lotes");
        ws.Cell(1, 1).Value = "Código Produto";
        ws.Cell(1, 2).Value = "Nome Produto";
        ws.Cell(1, 3).Value = "Lote";
        ws.Cell(1, 4).Value = "Data Fabricação";
        ws.Cell(1, 5).Value = "Data Validade";
        ws.Cell(1, 6).Value = "Quantidade";
        ws.Cell(1, 7).Value = "Unidade";
        ws.Cell(1, 8).Value = "Status";
        ws.Cell(1, 9).Value = "Observação";
        EstiloHeader(ws.Row(1));

        for (int r = 0; r < dados.Count; r++)
        {
            var l = dados[r];
            ws.Cell(r + 2, 1).Value = l.ProdutoCodigo;
            ws.Cell(r + 2, 2).Value = l.ProdutoNome;
            ws.Cell(r + 2, 3).Value = l.Lote;
            ws.Cell(r + 2, 4).Value = l.DataFabricacao;
            ws.Cell(r + 2, 4).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(r + 2, 5).Value = l.DataValidade;
            ws.Cell(r + 2, 5).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(r + 2, 6).Value = l.Quantidade;
            ws.Cell(r + 2, 7).Value = l.ProdutoUnidade;
            ws.Cell(r + 2, 8).Value = l.Status.ToString();
            ws.Cell(r + 2, 9).Value = l.Observacao;
        }
        ws.Columns().AdjustToContents();
    }
}
