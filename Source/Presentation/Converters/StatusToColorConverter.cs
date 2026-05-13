using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CEB.Domain.Aggregates;

namespace CEB.Presentation.Converters;

/// <summary>
/// Conversor WPF que transforma um valor <see cref="StatusValidade"/> em uma
/// <see cref="Brush"/> de cor correspondente ao estado de validade do lote.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    /// <summary>
    /// Converte um <see cref="StatusValidade"/> em uma <see cref="SolidColorBrush"/>:
    /// vermelho claro para vencido, amarelo claro para a vencer e verde claro para válido.
    /// </summary>
    /// <param name="value">Valor de origem; deve ser do tipo <see cref="StatusValidade"/>.</param>
    /// <param name="targetType">Tipo alvo da ligação de dados (não utilizado).</param>
    /// <param name="parameter">Parâmetro adicional (não utilizado).</param>
    /// <param name="culture">Cultura atual (não utilizada).</param>
    /// <returns>Uma <see cref="SolidColorBrush"/> representando o status de validade.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StatusValidade s)
            return s switch
            {
                StatusValidade.Vencido => Brushes.LightCoral,
                StatusValidade.AVencer => Brushes.LightYellow,
                _ => Brushes.LightGreen,
            };
        return Brushes.Transparent;
    }

    /// <summary>
    /// Conversão inversa não suportada por este conversor.
    /// </summary>
    /// <exception cref="NotSupportedException">Sempre lançada.</exception>
    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
