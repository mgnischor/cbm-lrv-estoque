using System.Windows;
using System.Windows.Threading;

namespace CEB;

/// <summary>
/// Ponto de entrada da aplicação WPF de controle de estoque com endereçamento.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        MessageBox.Show(
            $"Erro inesperado:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Erro — Controle de Estoque",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        e.Handled = true;
    }
}
