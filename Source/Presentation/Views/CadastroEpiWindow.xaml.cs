using System.Windows;
using System.Windows.Controls;
using CEB.Domain.Entities;
using CEB.Infrastructure.Data;

namespace CEB.Presentation.Views;

/// <summary>
/// Janela de cadastro e edição de EPIs (Equipamentos de Proteção Individual).
/// </summary>
public partial class CadastroEpiWindow : Window
{
    private readonly DatabaseService _db;
    private readonly Epi? _editando;

    /// <summary>EPI salvo com sucesso; <see langword="null"/> enquanto não confirmado.</summary>
    public Epi? EpiSalvo { get; private set; }

    /// <summary>
    /// Inicializa a janela de cadastro, opcionalmente no modo de edição.
    /// </summary>
    public CadastroEpiWindow(DatabaseService db, Epi? editando = null)
    {
        InitializeComponent();
        _db = db;
        _editando = editando;

        DpValidadeCa.SelectedDate = DateTime.Today.AddYears(1);

        if (editando is not null)
        {
            TxtTituloJanela.Text = "Editar EPI";
            TxtCodigo.Text = editando.Codigo;
            TxtNome.Text = editando.Nome;
            TxtNumeroCa.Text = editando.NumeroCa;
            DpValidadeCa.SelectedDate = editando.ValidadeCa;
            TxtQuantidade.Text = editando.Quantidade.ToString("G");
            TxtResponsavel.Text = editando.Responsavel;
            TxtSetor.Text = editando.Setor;
            TxtDescricao.Text = editando.Descricao;
            TxtObservacao.Text = editando.Observacao;

            // Selecionar o estado de conservação correto
            var idx = editando.EstadoConservacao switch
            {
                EstadoConservacao.Otimo => 0,
                EstadoConservacao.Bom => 1,
                EstadoConservacao.Regular => 2,
                EstadoConservacao.Danificado => 3,
                EstadoConservacao.Descartado => 4,
                _ => 1,
            };
            CboEstadoConservacao.SelectedIndex = idx;
        }
    }

    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (
            string.IsNullOrWhiteSpace(TxtCodigo.Text)
            || string.IsNullOrWhiteSpace(TxtNome.Text)
            || string.IsNullOrWhiteSpace(TxtNumeroCa.Text)
            || DpValidadeCa.SelectedDate is null
        )
        {
            MessageBox.Show(
                "Código, Nome, Nº CA e Validade CA são obrigatórios.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        if (
            !decimal.TryParse(
                TxtQuantidade.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var qtd
            )
        )
        {
            MessageBox.Show(
                "Informe uma quantidade válida.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        var conservacao = CboEstadoConservacao.SelectedIndex switch
        {
            0 => EstadoConservacao.Otimo,
            1 => EstadoConservacao.Bom,
            2 => EstadoConservacao.Regular,
            3 => EstadoConservacao.Danificado,
            4 => EstadoConservacao.Descartado,
            _ => EstadoConservacao.Bom,
        };

        var epi = new Epi
        {
            Id = _editando?.Id ?? 0,
            Codigo = TxtCodigo.Text.Trim(),
            Nome = TxtNome.Text.Trim(),
            NumeroCa = TxtNumeroCa.Text.Trim(),
            ValidadeCa = DpValidadeCa.SelectedDate!.Value,
            Quantidade = qtd,
            EstadoConservacao = conservacao,
            Responsavel = TxtResponsavel.Text.Trim(),
            Setor = TxtSetor.Text.Trim(),
            Descricao = TxtDescricao.Text.Trim(),
            Observacao = TxtObservacao.Text.Trim(),
        };

        try
        {
            _db.SalvarEpi(epi);
            EpiSalvo = epi;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar EPI:\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
