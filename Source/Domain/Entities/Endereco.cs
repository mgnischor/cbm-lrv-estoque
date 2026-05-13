namespace CEB.Domain.Entities;

/// <summary>
/// Representa um endereço físico de armazenamento dentro do almoxarifado,
/// identificado pela combinação de Setor, Rua, Coluna e Nível.
/// </summary>
public class Endereco
{
    /// <summary>Identificador único do endereço no banco de dados.</summary>
    public int Id { get; set; }

    /// <summary>Setor do almoxarifado (ex.: A, B).</summary>
    public string Setor { get; set; } = string.Empty;

    /// <summary>Rua dentro do setor (ex.: 01, 02).</summary>
    public string Rua { get; set; } = string.Empty;

    /// <summary>Coluna da prateleira (ex.: A, B, C).</summary>
    public string Coluna { get; set; } = string.Empty;

    /// <summary>Nível vertical da prateleira (ex.: 1, 2, 3).</summary>
    public string Nivel { get; set; } = string.Empty;

    /// <summary>
    /// Código de endereçamento no formato <c>Setor-Rua-Coluna-Nível</c>.
    /// </summary>
    public string Codigo => $"{Setor}-{Rua}-{Coluna}-{Nivel}";

    /// <inheritdoc/>
    public override string ToString() => Codigo;
}
