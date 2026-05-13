namespace CEB.Infrastructure.Labels;

/// <summary>
/// Codificador <b>Code 128</b> (subconjunto B) — padrão amplamente utilizado em
/// etiquetas logísticas (GS1-128, EAN-128) por aceitar todo o conjunto ASCII
/// imprimível (caracteres 32..126).
/// </summary>
/// <remarks>
/// A simbologia Code 128 está definida na norma ISO/IEC 15417 e é a base das
/// etiquetas logísticas GS1-128 utilizadas em rastreabilidade de produtos
/// (NBR 15486), endereçamento de armazenagem (NBR 14937) e controle de
/// validade. Cada caractere é representado por 11 módulos (alternando barras
/// e espaços), exceto o símbolo de parada que possui 13 módulos.
/// </remarks>
public static class Code128
{
    /// <summary>
    /// Tabela de padrões dos 107 valores de Code 128 + parada (índice 106).
    /// Cada padrão é uma sequência de larguras (bar, space, bar, space, …)
    /// começando sempre por uma barra. O índice 106 (STOP) possui sete
    /// elementos; todos os demais possuem seis.
    /// </summary>
    private static readonly string[] Patterns =
    {
        "212222",
        "222122",
        "222221",
        "121223",
        "121322",
        "131222",
        "122213",
        "122312",
        "132212",
        "221213",
        "221312",
        "231212",
        "112232",
        "122132",
        "122231",
        "113222",
        "123122",
        "123221",
        "223211",
        "221132",
        "221231",
        "213212",
        "223112",
        "312131",
        "311222",
        "321122",
        "321221",
        "312212",
        "322112",
        "322211",
        "212123",
        "212321",
        "232121",
        "111323",
        "131123",
        "131321",
        "112313",
        "132113",
        "132311",
        "211313",
        "231113",
        "231311",
        "112133",
        "112331",
        "132131",
        "113123",
        "113321",
        "133121",
        "313121",
        "211331",
        "231131",
        "213113",
        "213311",
        "213131",
        "311123",
        "311321",
        "331121",
        "312113",
        "312311",
        "332111",
        "314111",
        "221411",
        "431111",
        "111224",
        "111422",
        "121124",
        "121421",
        "141122",
        "141221",
        "112214",
        "112412",
        "122114",
        "122411",
        "142112",
        "142211",
        "241211",
        "221114",
        "413111",
        "241112",
        "134111",
        "111242",
        "121142",
        "121241",
        "114212",
        "124112",
        "124211",
        "411212",
        "421112",
        "421211",
        "212141",
        "214121",
        "412121",
        "111143",
        "111341",
        "131141",
        "114113",
        "114311",
        "411113",
        "411311",
        "113141",
        "114131",
        "311141",
        "411131",
        "211412",
        "211214",
        "211232",
        "2331112",
    };

    /// <summary>Valor do código de início para o subconjunto B.</summary>
    private const int StartB = 104;

    /// <summary>Valor do código de parada.</summary>
    private const int Stop = 106;

    /// <summary>
    /// Codifica o texto informado em um vetor de módulos onde <see langword="true"/>
    /// representa uma barra (preta) e <see langword="false"/> um espaço (branco).
    /// </summary>
    /// <param name="text">Texto ASCII imprimível (32..126).</param>
    /// <returns>Sequência de módulos correspondente ao código de barras.</returns>
    /// <exception cref="ArgumentException">Quando <paramref name="text"/> é nulo,
    /// vazio ou contém caracteres fora do intervalo ASCII imprimível.</exception>
    public static bool[] Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Texto vazio.", nameof(text));

        var values = new List<int>(text.Length + 3) { StartB };
        foreach (var c in text)
        {
            if (c < 32 || c > 126)
                throw new ArgumentException(
                    $"Caractere inválido para Code 128B: 0x{(int)c:X2}",
                    nameof(text)
                );
            values.Add(c - 32);
        }

        // Soma de verificação: (start + Σ i·valuei) mod 103
        long sum = StartB;
        for (int i = 1; i < values.Count; i++)
            sum += (long)i * values[i];
        values.Add((int)(sum % 103));
        values.Add(Stop);

        var modules = new List<bool>(values.Count * 11 + 2);
        foreach (var v in values)
        {
            var pattern = Patterns[v];
            bool bar = true;
            foreach (var w in pattern)
            {
                int width = w - '0';
                for (int k = 0; k < width; k++)
                    modules.Add(bar);
                bar = !bar;
            }
        }
        // Barra final de terminação (parte do STOP, já incluída em "2331112").
        return modules.ToArray();
    }
}
