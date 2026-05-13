using System.Windows;
using System.Windows.Controls;
using CEB.Domain.Aggregates;
using CEB.Infrastructure.Data;

namespace CEB.Presentation.Views;

/// <summary>
/// Janela de controle de validade de lotes de produtos.
/// Permite cadastrar, editar, filtrar e excluir lotes com controle de datas.
/// </summary>
public partial class ValidadeWindow : Window
{
    private readonly DatabaseService _db;

    /// <summary>Identificador do lote em edição. Valor 0 indica novo cadastro.</summary>
    private int _loteEditandoId = 0;

    /// <summary>
    /// Inicializa a janela de controle de validade.
    /// </summary>
    /// <param name="db">Serviço de acesso ao banco de dados.</param>
    public ValidadeWindow(DatabaseService db)
    {
        _db = db;
        InitializeComponent();
        CboProduto.ItemsSource = _db.ListarProdutos();
        CarregarLotes();
    }

    // ── Carregamento de dados ─────────────────────────────────────────────

    /// <summary>
    /// Recarrega a lista de lotes aplicando o filtro de texto e o filtro de status selecionados.
    /// </summary>
    private void CarregarLotes()
    {
        if (DgLotes is null)
            return;
        var filtroTexto = TxtFiltro?.Text.Trim() ?? "";
        var todos = _db.ListarLotes(filtroTexto);

        var lista = (CboFiltroStatus.SelectedIndex) switch
        {
            1 => todos.Where(l => l.Status != StatusValidade.Vencido).ToList(),
            2 => todos.Where(l => l.Status == StatusValidade.Vencido).ToList(),
            3 => todos.Where(l => l.Status == StatusValidade.AVencer).ToList(),
            _ => todos,
        };

        DgLotes.ItemsSource = lista;
    }

    /// <summary>Recarrega os lotes ao alterar o texto do filtro.</summary>
    private void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e) => CarregarLotes();

    /// <summary>Recarrega os lotes ao alterar o filtro de status.</summary>
    private void CboFiltroStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        CarregarLotes();

    /// <summary>Recarrega os lotes ao clicar no botão de atualizar.</summary>
    private void BtnAtualizar_Click(object sender, RoutedEventArgs e) => CarregarLotes();

    // ── Persistência de lotes ─────────────────────────────────────────────

    /// <summary>
    /// Valida os campos obrigatórios e persiste o lote no banco de dados.
    /// </summary>
    private void BtnSalvarLote_Click(object sender, RoutedEventArgs e)
    {
        if (CboProduto.SelectedValue is not int prodId)
        {
            MessageBox.Show("Selecione um produto.", "Aviso");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLote.Text))
        {
            MessageBox.Show("Informe o número do lote.", "Aviso");
            return;
        }

        if (DpFabricacao.SelectedDate is null)
        {
            MessageBox.Show("Informe a data de fabricação.", "Aviso");
            return;
        }

        if (DpValidade.SelectedDate is null)
        {
            MessageBox.Show("Informe a data de validade.", "Aviso");
            return;
        }

        if (
            !decimal.TryParse(
                TxtQtd.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var qtd
            )
            || qtd < 0
        )
        {
            MessageBox.Show("Informe uma quantidade válida.", "Aviso");
            return;
        }

        try
        {
            _db.SalvarLote(
                new LoteProduto
                {
                    Id = _loteEditandoId,
                    ProdutoId = prodId,
                    Lote = TxtLote.Text.Trim(),
                    DataFabricacao = DpFabricacao.SelectedDate!.Value,
                    DataValidade = DpValidade.SelectedDate!.Value,
                    Quantidade = qtd,
                    Observacao = TxtObservacao.Text.Trim(),
                }
            );
            LimparForm();
            CarregarLotes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar lote:\n{ex.Message}", "Erro");
        }
    }

    /// <summary>Limpa o formulário ao clicar no botão de limpar.</summary>
    private void BtnLimparLote_Click(object sender, RoutedEventArgs e) => LimparForm();

    /// <summary>
    /// Redefine todos os campos do formulário de lote para os valores padrão.
    /// </summary>
    private void LimparForm()
    {
        _loteEditandoId = 0;
        CboProduto.SelectedItem = null;
        TxtLote.Clear();
        DpFabricacao.SelectedDate = null;
        DpValidade.SelectedDate = null;
        TxtQtd.Clear();
        TxtObservacao.Clear();
        DgLotes.SelectedItem = null;
    }

    // ── Seleção no grid ───────────────────────────────────────────────────

    /// <summary>
    /// Preenche o formulário de edição com os dados do lote selecionado no grid.
    /// </summary>
    private void DgLotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgLotes.SelectedItem is not LoteProduto l)
            return;
        _loteEditandoId = l.Id;
        CboProduto.SelectedValue = l.ProdutoId;
        TxtLote.Text = l.Lote;
        DpFabricacao.SelectedDate = l.DataFabricacao;
        DpValidade.SelectedDate = l.DataValidade;
        TxtQtd.Text = l.Quantidade.ToString("G");
        TxtObservacao.Text = l.Observacao;
    }

    // ── Menu de contexto ──────────────────────────────────────────────────

    /// <summary>
    /// Abre o lote selecionado para edição (a seleção já preenche o formulário via
    /// <see cref="DgLotes_SelectionChanged"/>).
    /// </summary>
    private void MenuEditarLote_Click(object sender, RoutedEventArgs e)
    {
        if (DgLotes.SelectedItem is null)
            MessageBox.Show("Selecione um lote.", "Aviso");
    }

    /// <summary>
    /// Solicita confirmação e exclui o lote selecionado do banco de dados.
    /// </summary>
    private void MenuExcluirLote_Click(object sender, RoutedEventArgs e)
    {
        if (DgLotes.SelectedItem is not LoteProduto l)
            return;
        if (
            MessageBox.Show(
                $"Excluir lote {l.Lote} de {l.ProdutoNome}?",
                "Confirmar",
                MessageBoxButton.YesNo
            ) == MessageBoxResult.Yes
        )
        {
            _db.ExcluirLote(l.Id);
            LimparForm();
            CarregarLotes();
        }
    }
}
