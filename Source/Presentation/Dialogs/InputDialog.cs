using System.Windows;
using System.Windows.Controls;

namespace CEB.Presentation.Dialogs;

/// <summary>
/// Janela de diálogo simples que exibe um campo de texto para entrada de dados pelo usuário.
/// </summary>
public class InputDialog : Window
{
    private readonly TextBox _textBox;

    /// <summary>Texto digitado pelo usuário após confirmar o diálogo.</summary>
    public string ResponseText { get; private set; } = string.Empty;

    /// <summary>
    /// Inicializa uma nova instância do diálogo de entrada de dados.
    /// </summary>
    /// <param name="title">Título exibido na barra da janela.</param>
    /// <param name="prompt">Mensagem de instrução exibida acima do campo de texto.</param>
    /// <param name="defaultValue">Valor inicial pré-preenchido no campo de texto.</param>
    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Title = title;
        Width = 380;
        Height = 160;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var sp = new StackPanel { Margin = new Thickness(16) };

        sp.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) });

        _textBox = new TextBox { Text = defaultValue };
        _textBox.SelectAll();
        sp.Children.Add(_textBox);

        var btns = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };

        var ok = new Button
        {
            Content = "OK",
            IsDefault = true,
            MinWidth = 70,
            Margin = new Thickness(0, 0, 8, 0),
        };
        ok.Click += (_, _) =>
        {
            ResponseText = _textBox.Text;
            DialogResult = true;
        };

        var cancel = new Button
        {
            Content = "Cancelar",
            IsCancel = true,
            MinWidth = 70,
        };

        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        sp.Children.Add(btns);

        Content = sp;
        Loaded += (_, _) => _textBox.Focus();
    }
}
