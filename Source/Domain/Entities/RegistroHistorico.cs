namespace CEB.Domain.Entities;

/// <summary>
/// Representa um registro de histórico de operações realizadas no sistema.
/// </summary>
public class RegistroHistorico
{
    /// <summary>Identificador único do registro.</summary>
    public int Id { get; set; }

    /// <summary>Categoria do lançamento: Estoque, Produto, EPI ou Validade.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Ação executada: Entrada, Saída, Ajuste, Cadastro, Atualização ou Exclusão.</summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>Código do item envolvido na operação.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nome do item envolvido na operação.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Quantidade envolvida na operação, quando aplicável.</summary>
    public decimal? Quantidade { get; set; }

    /// <summary>Informações adicionais sobre o lançamento.</summary>
    public string Detalhes { get; set; } = string.Empty;

    /// <summary>Data e hora em que o lançamento foi registrado.</summary>
    public DateTime DataHora { get; set; } = DateTime.Now;
}
