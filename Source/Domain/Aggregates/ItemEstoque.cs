namespace CEB.Domain.Aggregates;

/// <summary>
/// Representa um item de estoque, associando um produto a um endereço de armazenamento
/// com a respectiva quantidade disponível.
/// </summary>
public class ItemEstoque
{
    /// <summary>Identificador único do registro de estoque.</summary>
    public int Id { get; set; }

    /// <summary>Identificador do produto associado.</summary>
    public int ProdutoId { get; set; }

    /// <summary>Identificador do endereço de armazenamento.</summary>
    public int EnderecoId { get; set; }

    /// <summary>Quantidade atual do produto neste endereço.</summary>
    public decimal Quantidade { get; set; }

    // ── Propriedades de navegação (leitura) ──────────────────────────────

    /// <summary>Código do produto (preenchido por consulta).</summary>
    public string ProdutoCodigo { get; set; } = string.Empty;

    /// <summary>Nome do produto (preenchido por consulta).</summary>
    public string ProdutoNome { get; set; } = string.Empty;

    /// <summary>Unidade de medida do produto (preenchido por consulta).</summary>
    public string ProdutoUnidade { get; set; } = string.Empty;

    /// <summary>Código completo do endereço no formato Setor-Rua-Coluna-Nível (preenchido por consulta).</summary>
    public string EnderecoCode { get; set; } = string.Empty;
}
