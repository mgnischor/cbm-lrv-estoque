using System.Windows;
using System.Windows.Controls;
using CEB.Infrastructure.Data;

namespace CEB.Presentation.Views;

/// <summary>
/// Janela de consulta do histórico de lançamentos do sistema.
/// Exibe todos os eventos de estoque, cadastros de produtos, EPIs e controle de validade.
/// </summary>
public partial class HistoricoWindow : Window
{
    private readonly DatabaseService _db;

    /// <summary>
    /// Inicializa a janela de histórico, com filtros pré-aplicados opcionais.
    /// </summary>
    /// <param name="db">Serviço de acesso ao banco de dados.</param>
    /// <param name="filtroInicial">Texto pré-preenchido no campo de busca.</param>
    /// <param name="tipoInicial">Tipo pré-selecionado no combo de filtro (Estoque, Produto, EPI, Validade).</param>
    public HistoricoWindow(
        DatabaseService db,
        string? filtroInicial = null,
        string? tipoInicial = null
    )
    {
        _db = db; // deve ser atribuído antes de InitializeComponent para evitar NullReferenceException
        InitializeComponent(); // dispara SelectionChanged do CboTipo ao aplicar IsSelected="True"

        if (!string.IsNullOrWhiteSpace(filtroInicial))
            TxtFiltro.Text = filtroInicial;

        if (!string.IsNullOrWhiteSpace(tipoInicial))
        {
            foreach (ComboBoxItem item in CboTipo.Items)
            {
                if (
                    string.Equals(
                        item.Content?.ToString(),
                        tipoInicial,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    CboTipo.SelectedItem = item;
                    break;
                }
            }
        }

        CarregarHistorico();
    }

    // ── Carregamento ──────────────────────────────────────────────────────

    private void CarregarHistorico()
    {
        // CboTipo_SelectionChanged pode disparar durante InitializeComponent (IsSelected="True")
        // antes dos controles posteriores (DpInicio, DpFim, DgHistorico) serem criados.
        if (DgHistorico is null)
            return;

        try
        {
            var filtro = TxtFiltro.Text.Trim();
            var tipo = (CboTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            var inicio = DpInicio.SelectedDate;
            var fim = DpFim.SelectedDate;

            var lista = _db.ListarHistorico(filtro, tipo == "Todos" ? "" : tipo, inicio, fim);

            DgHistorico.ItemsSource = lista;
            TxtStatus.Text = $"{lista.Count} registro(s) encontrado(s)";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Erro ao carregar histórico: {ex.Message}";
            MessageBox.Show(
                $"Erro ao carregar o histórico:\n\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    // ── Eventos de filtro ─────────────────────────────────────────────────

    private void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e) =>
        CarregarHistorico();

    private void CboTipo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        CarregarHistorico();

    private void Dp_DateChanged(object? sender, SelectionChangedEventArgs e) => CarregarHistorico();

    private void BtnLimparFiltros_Click(object sender, RoutedEventArgs e)
    {
        TxtFiltro.Clear();
        CboTipo.SelectedIndex = 0;
        DpInicio.SelectedDate = null;
        DpFim.SelectedDate = null;
    }
}
