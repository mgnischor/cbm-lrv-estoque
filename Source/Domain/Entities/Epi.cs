namespace CEB.Domain.Entities;

/// <summary>
/// Representa um Equipamento de Proteção Individual (EPI) cadastrado no sistema.
/// </summary>
public class Epi
{
    /// <summary>Identificador único do EPI no banco de dados.</summary>
    public int Id { get; set; }

    /// <summary>Código interno de identificação do EPI.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nome descritivo do EPI.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Descrição detalhada do EPI (tipo, modelo, fabricante etc.).</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Número do Certificado de Aprovação (CA) emitido pelo MTE.</summary>
    public string NumeroCa { get; set; } = string.Empty;

    /// <summary>Data de validade do Certificado de Aprovação (CA).</summary>
    public DateTime ValidadeCa { get; set; } = DateTime.Today.AddYears(1);

    /// <summary>Quantidade disponível em estoque.</summary>
    public decimal Quantidade { get; set; }

    /// <summary>Estado de conservação do EPI.</summary>
    public EstadoConservacao EstadoConservacao { get; set; } = EstadoConservacao.Bom;

    /// <summary>Nome do responsável pela guarda/uso do EPI.</summary>
    public string Responsavel { get; set; } = string.Empty;

    /// <summary>Setor ou localização onde o EPI está alocado.</summary>
    public string Setor { get; set; } = string.Empty;

    /// <summary>Observações adicionais sobre o EPI.</summary>
    public string Observacao { get; set; } = string.Empty;

    /// <summary>Data de cadastro do EPI no sistema.</summary>
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    /// <summary>Data da última atualização do registro do EPI.</summary>
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    // ── Propriedades calculadas ───────────────────────────────────────────

    /// <summary>
    /// Quantidade de dias restantes até a expiração do CA.
    /// Valor negativo indica que o CA já está vencido.
    /// </summary>
    public int DiasParaVencerCa => (ValidadeCa.Date - DateTime.Today).Days;

    /// <summary>
    /// Status de validade do CA calculado com base em <see cref="DiasParaVencerCa"/>.
    /// </summary>
    public StatusValidadeCa StatusCa =>
        DiasParaVencerCa < 0 ? StatusValidadeCa.Vencido
        : DiasParaVencerCa <= 60 ? StatusValidadeCa.AVencer
        : StatusValidadeCa.Ok;
}

/// <summary>
/// Indica o estado de conservação de um EPI.
/// </summary>
public enum EstadoConservacao
{
    /// <summary>EPI em ótimas condições, sem desgaste visível.</summary>
    Otimo,

    /// <summary>EPI em boas condições, com desgaste mínimo.</summary>
    Bom,

    /// <summary>EPI com desgaste moderado, ainda dentro dos padrões de uso.</summary>
    Regular,

    /// <summary>EPI com avaria ou dano que compromete sua eficácia — requer substituição.</summary>
    Danificado,

    /// <summary>EPI descartado e fora de uso.</summary>
    Descartado,
}

/// <summary>
/// Indica o estado de validade do CA de um EPI.
/// </summary>
public enum StatusValidadeCa
{
    /// <summary>CA dentro do prazo de validade.</summary>
    Ok,

    /// <summary>CA com validade próxima do vencimento (≤ 60 dias).</summary>
    AVencer,

    /// <summary>CA com data de validade já expirada.</summary>
    Vencido,
}
