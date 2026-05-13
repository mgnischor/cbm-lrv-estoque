using System.Windows;
using CEB.Domain.Entities;
using CEB.Infrastructure.Data;

namespace CEB.Presentation.Views;

/// <summary>
/// Janela de cadastro e edição de produtos do sistema de controle de estoque.
/// </summary>
public partial class CadastroProdutoWindow : Window
{
    private readonly DatabaseService _db;
    private readonly Produto? _editando;

    /// <summary>Produto salvo com sucesso; <see langword="null"/> enquanto não confirmado.</summary>
    public Produto? ProdutoSalvo { get; private set; }

    /// <summary>
    /// Inicializa a janela de cadastro, opcionalmente no modo de edição.
    /// </summary>
    /// <param name="db">Serviço de acesso ao banco de dados.</param>
    /// <param name="editando">Produto a ser editado, ou <see langword="null"/> para novo cadastro.</param>
    public CadastroProdutoWindow(DatabaseService db, Produto? editando = null)
    {
        InitializeComponent();
        _db = db;
        _editando = editando;

        if (editando is not null)
        {
            TxtTituloJanela.Text = "Editar Produto";
            TxtCodigo.Text = editando.Codigo;
            TxtNome.Text = editando.Nome;
            TxtUnidade.Text = editando.Unidade;
            TxtCategoria.Text = editando.Categoria;
            TxtPatrimonio.Text = editando.Patrimonio;
            TxtDescricao.Text = editando.Descricao;
        }
    }

    /// <summary>
    /// Valida os campos obrigatórios e persiste o produto no banco de dados.
    /// </summary>
    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (
            string.IsNullOrWhiteSpace(TxtCodigo.Text)
            || string.IsNullOrWhiteSpace(TxtNome.Text)
            || string.IsNullOrWhiteSpace(TxtUnidade.Text)
        )
        {
            MessageBox.Show(
                "Código, Nome e Unidade são obrigatórios.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        var produto = new Produto
        {
            Id = _editando?.Id ?? 0,
            Codigo = TxtCodigo.Text.Trim(),
            Nome = TxtNome.Text.Trim(),
            Unidade = TxtUnidade.Text.Trim(),
            Categoria = TxtCategoria.Text.Trim(),
            Patrimonio = TxtPatrimonio.Text.Trim(),
            Descricao = TxtDescricao.Text.Trim(),
        };

        try
        {
            _db.SalvarProduto(produto);
            ProdutoSalvo = produto;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar produto:\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Cancela a operação e fecha a janela sem salvar.
    /// </summary>
    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
