namespace CEB.Domain.Entities;

/// <summary>
/// Representa um produto cadastrado no sistema de controle de estoque.
/// </summary>
public class Produto
{
    /// <summary>Identificador único do produto no banco de dados.</summary>
    public int Id { get; set; }

    /// <summary>Código alfanumérico de identificação do produto (único).</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nome descritivo do produto.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Descrição detalhada do produto.</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Unidade de medida utilizada para quantificação (ex.: UN, KG, L).</summary>
    public string Unidade { get; set; } = string.Empty;

    /// <summary>Categoria à qual o produto pertence.</summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Número de patrimônio associado ao produto, quando aplicável.</summary>
    public string Patrimonio { get; set; } = string.Empty;

    /// <summary>Data de cadastro do produto no sistema.</summary>
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    /// <summary>Data da última atualização do registro do produto.</summary>
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
}
