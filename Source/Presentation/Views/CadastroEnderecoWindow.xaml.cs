using System.Windows;
using CEB.Domain.Entities;
using CEB.Infrastructure.Data;

namespace CEB.Presentation.Views;

/// <summary>
/// Janela de cadastro e edição de endereços de armazenamento do almoxarifado.
/// </summary>
public partial class CadastroEnderecoWindow : Window
{
    private readonly DatabaseService _db;
    private readonly Endereco? _editando;

    /// <summary>Endereço salvo com sucesso; <see langword="null"/> enquanto não confirmado.</summary>
    public Endereco? EnderecoSalvo { get; private set; }

    /// <summary>
    /// Inicializa a janela de cadastro, opcionalmente no modo de edição.
    /// </summary>
    /// <param name="db">Serviço de acesso ao banco de dados.</param>
    /// <param name="editando">Endereço a ser editado, ou <see langword="null"/> para novo cadastro.</param>
    public CadastroEnderecoWindow(DatabaseService db, Endereco? editando = null)
    {
        InitializeComponent();
        _db = db;
        _editando = editando;

        TxtSetor.TextChanged += (_, _) => AtualizarPreview();
        TxtRua.TextChanged += (_, _) => AtualizarPreview();
        TxtColuna.TextChanged += (_, _) => AtualizarPreview();
        TxtNivel.TextChanged += (_, _) => AtualizarPreview();

        if (editando is not null)
        {
            TxtTitulo.Text = "Editar Endereço";
            TxtSetor.Text = editando.Setor;
            TxtRua.Text = editando.Rua;
            TxtColuna.Text = editando.Coluna;
            TxtNivel.Text = editando.Nivel;
        }

        AtualizarPreview();
        TxtSetor.Focus();
    }

    /// <summary>
    /// Atualiza o campo de pré-visualização com o código de endereçamento montado a partir dos campos preenchidos.
    /// </summary>
    private void AtualizarPreview()
    {
        var partes = new[] { TxtSetor.Text, TxtRua.Text, TxtColuna.Text, TxtNivel.Text };
        TxtPreview.Text = string.Join("-", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Valida os campos obrigatórios e persiste o endereço no banco de dados.
    /// </summary>
    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSetor.Text))
        {
            MessageBox.Show("Informe o Setor.", "Aviso");
            TxtSetor.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtRua.Text))
        {
            MessageBox.Show("Informe a Rua.", "Aviso");
            TxtRua.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtColuna.Text))
        {
            MessageBox.Show("Informe a Coluna.", "Aviso");
            TxtColuna.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtNivel.Text))
        {
            MessageBox.Show("Informe o Nível.", "Aviso");
            TxtNivel.Focus();
            return;
        }

        try
        {
            var endereco = new Endereco
            {
                Id = _editando?.Id ?? 0,
                Setor = TxtSetor.Text.Trim().ToUpper(),
                Rua = TxtRua.Text.Trim().ToUpper(),
                Coluna = TxtColuna.Text.Trim().ToUpper(),
                Nivel = TxtNivel.Text.Trim().ToUpper(),
            };
            _db.SalvarEndereco(endereco);
            EnderecoSalvo = endereco;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar endereço:\n{ex.Message}", "Erro");
        }
    }

    /// <summary>
    /// Cancela a operação e fecha a janela sem salvar.
    /// </summary>
    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
