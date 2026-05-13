namespace CEB.Domain.Aggregates;

/// <summary>
/// Representa um lote de produto com controle de validade e quantidade.
/// </summary>
public class LoteProduto
{
    /// <summary>Identificador único do lote no banco de dados.</summary>
    public int Id { get; set; }

    /// <summary>Identificador do produto ao qual o lote pertence.</summary>
    public int ProdutoId { get; set; }

    /// <summary>Número ou código do lote fornecido pelo fabricante.</summary>
    public string Lote { get; set; } = string.Empty;

    /// <summary>Data de fabricação do lote.</summary>
    public DateTime DataFabricacao { get; set; }

    /// <summary>Data de validade do lote.</summary>
    public DateTime DataValidade { get; set; }

    /// <summary>Quantidade disponível neste lote.</summary>
    public decimal Quantidade { get; set; }

    /// <summary>Observações adicionais sobre o lote.</summary>
    public string Observacao { get; set; } = string.Empty;

    // ── Propriedades de navegação (leitura) ──────────────────────────────

    /// <summary>Código do produto (preenchido por consulta).</summary>
    public string ProdutoCodigo { get; set; } = string.Empty;

    /// <summary>Nome do produto (preenchido por consulta).</summary>
    public string ProdutoNome { get; set; } = string.Empty;

    /// <summary>Unidade de medida do produto (preenchido por consulta).</summary>
    public string ProdutoUnidade { get; set; } = string.Empty;

    // ── Propriedades calculadas ───────────────────────────────────────────

    /// <summary>
    /// Quantidade de dias restantes até a data de validade.
    /// Valor negativo indica que o lote já está vencido.
    /// </summary>
    public int DiasParaVencer => (DataValidade.Date - DateTime.Today).Days;

    /// <summary>
    /// Status de validade do lote calculado com base em <see cref="DiasParaVencer"/>.
    /// </summary>
    public StatusValidade Status =>
        DiasParaVencer < 0 ? StatusValidade.Vencido
        : DiasParaVencer <= 30 ? StatusValidade.AVencer
        : StatusValidade.Ok;
}

/// <summary>
/// Indica o estado de validade de um lote de produto.
/// </summary>
public enum StatusValidade
{
    /// <summary>Lote dentro do prazo de validade.</summary>
    Ok,

    /// <summary>Lote com validade próxima do vencimento (≤ 30 dias).</summary>
    AVencer,

    /// <summary>Lote com data de validade já expirada.</summary>
    Vencido,
}
