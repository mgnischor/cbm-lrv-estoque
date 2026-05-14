using System.IO;
using System.Windows;
using System.Windows.Controls;
using CEB.Domain.Aggregates;
using CEB.Domain.Entities;
using CEB.Infrastructure.Data;
using CEB.Presentation.Dialogs;
using CEB.Presentation.Views;

namespace CEB.Presentation.Windows;

/// <summary>
/// Janela principal do sistema de controle de estoque com endereçamento.
/// Centraliza as operações de estoque, produtos e endereços.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DatabaseService _db;

    /// <summary>Identificador do produto em edição no painel lateral. Zero indica nenhum selecionado.</summary>
    private int _produtoEditandoId = 0;

    /// <summary>
    /// Inicializa a janela principal, configura o banco de dados e carrega todos os dados.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "controle-estoque",
            "estoque.db"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _db = new DatabaseService(dbPath);

        CarregarEstoque();
        CarregarProdutos();
        CarregarEnderecos();
        CarregarEpis();
    }

    // ══════════════════════════════════════════════════════════════════════
    // ESTOQUE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Recarrega o grid de estoque e as listas dos combos de produto e endereço.
    /// </summary>
    private void CarregarEstoque()
    {
        DgEstoque.ItemsSource = _db.ListarEstoque(TxtFiltroEstoque.Text.Trim());
        CboProdutoEstoque.ItemsSource = _db.ListarProdutos();
        CboEnderecoEstoque.ItemsSource = _db.ListarEnderecos();
    }

    /// <summary>Recarrega o estoque ao digitar no campo de filtro.</summary>
    private void TxtFiltroEstoque_TextChanged(object sender, TextChangedEventArgs e) =>
        CarregarEstoque();

    /// <summary>Recarrega o estoque ao clicar no botão de atualizar.</summary>
    private void BtnAtualizarEstoque_Click(object sender, RoutedEventArgs e) => CarregarEstoque();

    /// <summary>Registra uma entrada de estoque.</summary>
    private void BtnEntrada_Click(object sender, RoutedEventArgs e) => Movimentar(positivo: true);

    /// <summary>Registra uma saída de estoque.</summary>
    private void BtnSaida_Click(object sender, RoutedEventArgs e) => Movimentar(positivo: false);

    /// <summary>
    /// Valida os campos de movimentação e chama <see cref="DatabaseService.MovimentarEstoque"/>.
    /// </summary>
    /// <param name="positivo">
    /// <see langword="true"/> para entrada (adicionar quantidade);
    /// <see langword="false"/> para saída (subtrair quantidade).
    /// </param>
    private void Movimentar(bool positivo)
    {
        if (CboProdutoEstoque.SelectedValue is not int prodId)
        {
            MessageBox.Show("Selecione um produto.", "Aviso");
            return;
        }

        if (CboEnderecoEstoque.SelectedValue is not int endId)
        {
            MessageBox.Show("Selecione um endereço.", "Aviso");
            return;
        }

        if (
            !decimal.TryParse(
                TxtQtdEstoque.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var qtd
            )
            || qtd <= 0
        )
        {
            MessageBox.Show("Informe uma quantidade válida.", "Aviso");
            return;
        }

        _db.MovimentarEstoque(prodId, endId, positivo ? qtd : -qtd);
        TxtQtdEstoque.Clear();
        CarregarEstoque();
    }

    /// <summary>
    /// Abre o diálogo de ajuste de quantidade para o item de estoque selecionado.
    /// </summary>
    private void MenuAjustarQtd_Click(object sender, RoutedEventArgs e)
    {
        if (DgEstoque.SelectedItem is not ItemEstoque item)
            return;

        var dlg = new InputDialog(
            "Ajustar Quantidade",
            $"Nova quantidade para\n{item.ProdutoNome} @ {item.EnderecoCode}:",
            item.Quantidade.ToString("G")
        )
        {
            Owner = this,
        };

        if (
            dlg.ShowDialog() == true
            && decimal.TryParse(
                dlg.ResponseText.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var nova
            )
        )
        {
            _db.AjustarEstoque(item.Id, nova);
            CarregarEstoque();
        }
    }

    /// <summary>
    /// Solicita confirmação e exclui o item de estoque selecionado.
    /// </summary>
    private void MenuExcluirItemEstoque_Click(object sender, RoutedEventArgs e)
    {
        if (DgEstoque.SelectedItem is not ItemEstoque item)
            return;
        if (
            MessageBox.Show(
                $"Excluir o registro de {item.ProdutoNome} em {item.EnderecoCode}?",
                "Confirmar",
                MessageBoxButton.YesNo
            ) == MessageBoxResult.Yes
        )
        {
            _db.ExcluirItemEstoque(item.Id);
            CarregarEstoque();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // PRODUTOS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Recarrega o grid de produtos aplicando o filtro atual.</summary>
    private void CarregarProdutos() =>
        DgProdutos.ItemsSource = _db.ListarProdutos(TxtFiltroProduto.Text.Trim());

    /// <summary>Recarrega os produtos ao digitar no campo de filtro.</summary>
    private void TxtFiltroProduto_TextChanged(object sender, TextChangedEventArgs e) =>
        CarregarProdutos();

    /// <summary>Preenche o painel lateral com os dados do produto selecionado no grid.</summary>
    private void DgProdutos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgProdutos.SelectedItem is not Produto p)
            return;
        _produtoEditandoId = p.Id;
        TxtProdCodigo.Text = p.Codigo;
        TxtProdNome.Text = p.Nome;
        TxtProdUnidade.Text = p.Unidade;
        TxtProdCategoria.Text = p.Categoria;
        TxtProdDescricao.Text = p.Descricao;
        TxtProdPatrimonio.Text = p.Patrimonio;
    }

    /// <summary>Abre a janela de novo cadastro de produto.</summary>
    private void BtnNovoProduto_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CadastroProdutoWindow(_db) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            CarregarProdutos();
            CarregarEstoque();
        }
    }

    /// <summary>Abre a janela de edição para o produto selecionado (botão).</summary>
    private void BtnEditarProduto_Click(object sender, RoutedEventArgs e) => AbrirEdicaoProduto();

    /// <summary>Abre a janela de edição para o produto selecionado (duplo clique no grid).</summary>
    private void DgProdutos_DoubleClick(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e
    ) => AbrirEdicaoProduto();

    /// <summary>
    /// Abre a janela <see cref="CadastroProdutoWindow"/> no modo de edição para o produto
    /// atualmente selecionado no grid.
    /// </summary>
    private void AbrirEdicaoProduto()
    {
        if (DgProdutos.SelectedItem is not Produto p)
        {
            MessageBox.Show("Selecione um produto para editar.", "Aviso");
            return;
        }
        var dlg = new CadastroProdutoWindow(_db, p) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            CarregarProdutos();
            CarregarEstoque();
        }
    }

    /// <summary>Deseleciona o produto atual no grid.</summary>
    private void BtnLimparProduto_Click(object sender, RoutedEventArgs e) =>
        DgProdutos.SelectedItem = null;

    /// <summary>Limpa todos os campos do painel de produto e deseleciona o item no grid.</summary>
    private void LimparFormProduto()
    {
        _produtoEditandoId = 0;
        TxtProdCodigo.Clear();
        TxtProdNome.Clear();
        TxtProdUnidade.Clear();
        TxtProdCategoria.Clear();
        TxtProdDescricao.Clear();
        TxtProdPatrimonio.Clear();
        DgProdutos.SelectedItem = null;
    }

    /// <summary>
    /// Valida os campos obrigatórios e salva o produto (inserção ou atualização).
    /// </summary>
    private void BtnSalvarProduto_Click(object sender, RoutedEventArgs e)
    {
        if (
            string.IsNullOrWhiteSpace(TxtProdCodigo.Text)
            || string.IsNullOrWhiteSpace(TxtProdNome.Text)
            || string.IsNullOrWhiteSpace(TxtProdUnidade.Text)
        )
        {
            MessageBox.Show("Código, Nome e Unidade são obrigatórios.", "Aviso");
            return;
        }

        try
        {
            _db.SalvarProduto(
                new Produto
                {
                    Id = _produtoEditandoId,
                    Codigo = TxtProdCodigo.Text.Trim(),
                    Nome = TxtProdNome.Text.Trim(),
                    Unidade = TxtProdUnidade.Text.Trim(),
                    Categoria = TxtProdCategoria.Text.Trim(),
                    Descricao = TxtProdDescricao.Text.Trim(),
                    Patrimonio = TxtProdPatrimonio.Text.Trim(),
                }
            );
            LimparFormProduto();
            CarregarProdutos();
            CarregarEstoque();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro");
        }
    }

    /// <summary>
    /// Solicita confirmação e exclui o produto selecionado junto com todos os seus registros de estoque.
    /// </summary>
    private void BtnExcluirProduto_Click(object sender, RoutedEventArgs e)
    {
        if (_produtoEditandoId == 0)
        {
            MessageBox.Show("Selecione um produto.");
            return;
        }
        if (
            MessageBox.Show(
                "Excluir produto e todos os seus registros de estoque?",
                "Confirmar",
                MessageBoxButton.YesNo
            ) == MessageBoxResult.Yes
        )
        {
            _db.ExcluirProduto(_produtoEditandoId);
            LimparFormProduto();
            CarregarProdutos();
            CarregarEstoque();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENDEREÇOS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Recarrega o grid de endereços aplicando o filtro atual.</summary>
    private void CarregarEnderecos() =>
        DgEnderecos.ItemsSource = _db.ListarEnderecos(TxtFiltroEndereco.Text.Trim());

    /// <summary>Recarrega os endereços ao digitar no campo de filtro.</summary>
    private void TxtFiltroEndereco_TextChanged(object sender, TextChangedEventArgs e) =>
        CarregarEnderecos();

    private void AtualizarPreviewEndereco() { }

    private void DgEnderecos_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    /// <summary>Abre a janela de novo cadastro de endereço.</summary>
    private void BtnNovoEndereco_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CadastroEnderecoWindow(_db) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            CarregarEnderecos();
            CarregarEstoque();
        }
    }

    /// <summary>Abre a janela de edição para o endereço selecionado (botão).</summary>
    private void BtnEditarEndereco_Click(object sender, RoutedEventArgs e) => AbrirEdicaoEndereco();

    /// <summary>Abre a janela de edição para o endereço selecionado (duplo clique no grid).</summary>
    private void DgEnderecos_DoubleClick(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e
    ) => AbrirEdicaoEndereco();

    /// <summary>
    /// Abre a janela <see cref="CadastroEnderecoWindow"/> no modo de edição para o endereço
    /// atualmente selecionado no grid.
    /// </summary>
    private void AbrirEdicaoEndereco()
    {
        if (DgEnderecos.SelectedItem is not Endereco en)
        {
            MessageBox.Show("Selecione um endereço para editar.", "Aviso");
            return;
        }
        var dlg = new CadastroEnderecoWindow(_db, en) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            CarregarEnderecos();
            CarregarEstoque();
        }
    }

    /// <summary>Deseleciona o endereço atual no grid.</summary>
    private void BtnLimparEndereco_Click(object sender, RoutedEventArgs e) =>
        DgEnderecos.SelectedItem = null;

    private void BtnSalvarEndereco_Click(object sender, RoutedEventArgs e) { }

    /// <summary>
    /// Solicita confirmação e exclui o endereço selecionado junto com todos os seus registros de estoque.
    /// </summary>
    private void BtnExcluirEndereco_Click(object sender, RoutedEventArgs e)
    {
        if (DgEnderecos.SelectedItem is not Endereco en)
        {
            MessageBox.Show("Selecione um endereço para excluir.", "Aviso");
            return;
        }
        if (
            MessageBox.Show(
                $"Excluir endereço {en.Codigo} e todos os seus registros de estoque?",
                "Confirmar",
                MessageBoxButton.YesNo
            ) == MessageBoxResult.Yes
        )
        {
            _db.ExcluirEndereco(en.Id);
            DgEnderecos.SelectedItem = null;
            CarregarEnderecos();
            CarregarEstoque();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // EPIs
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Recarrega o grid de EPIs aplicando o filtro atual.</summary>
    private void CarregarEpis() => DgEpis.ItemsSource = _db.ListarEpis(TxtFiltroEpi.Text.Trim());

    private void TxtFiltroEpi_TextChanged(
        object sender,
        System.Windows.Controls.TextChangedEventArgs e
    ) => CarregarEpis();

    private void BtnAtualizarEpi_Click(object sender, RoutedEventArgs e) => CarregarEpis();

    private void DgEpis_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e
    ) { }

    private void DgEpis_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        AbrirEdicaoEpi();

    /// <summary>Abre a janela de novo cadastro de EPI.</summary>
    private void BtnNovoEpi_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CEB.Presentation.Views.CadastroEpiWindow(_db) { Owner = this };
        if (dlg.ShowDialog() == true)
            CarregarEpis();
    }

    /// <summary>Abre a janela de edição para o EPI selecionado.</summary>
    private void BtnEditarEpi_Click(object sender, RoutedEventArgs e) => AbrirEdicaoEpi();

    private void AbrirEdicaoEpi()
    {
        if (DgEpis.SelectedItem is not CEB.Domain.Entities.Epi epi)
        {
            MessageBox.Show("Selecione um EPI para editar.", "Aviso");
            return;
        }
        var dlg = new CEB.Presentation.Views.CadastroEpiWindow(_db, epi) { Owner = this };
        if (dlg.ShowDialog() == true)
            CarregarEpis();
    }

    /// <summary>Solicita confirmação e exclui o EPI selecionado.</summary>
    private void BtnExcluirEpi_Click(object sender, RoutedEventArgs e)
    {
        if (DgEpis.SelectedItem is not CEB.Domain.Entities.Epi epi)
        {
            MessageBox.Show("Selecione um EPI para excluir.", "Aviso");
            return;
        }
        if (
            MessageBox.Show(
                $"Excluir o EPI '{epi.Nome}' (CA: {epi.NumeroCa})?",
                "Confirmar",
                MessageBoxButton.YesNo
            ) == MessageBoxResult.Yes
        )
        {
            _db.ExcluirEpi(epi.Id);
            CarregarEpis();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // SOBRE / VALIDADE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Exibe a caixa de diálogo com informações sobre o aplicativo.</summary>
    private void BtnSobre_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Desenvolvido por Miguel Nischor <miguel@nischor.com.br>\nDesenvolvido gratuitamente para o Corpo de Bombeiros\nde Lucas do Rio Verde",
            "Sobre o aplicativo",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    /// <summary>Abre a janela de controle de validade de lotes.</summary>
    private void BtnAbrirValidade_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ValidadeWindow(_db) { Owner = this };
        dlg.ShowDialog();
    }

    // ══════════════════════════════════════════════════════════════════════
    // EXPORTAÇÃO
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Abre o seletor de pasta de destino para exportação.</summary>
    private void BtnExportProcurar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Selecione a pasta de destino para exportação",
        };
        if (dlg.ShowDialog() == true)
            TxtExportPasta.Text = dlg.FolderName;
    }

    /// <summary>
    /// Valida as opções e exporta os dados selecionados para CSV ou XLSX
    /// na pasta indicada, com timestamp no nome dos arquivos.
    /// </summary>
    private void BtnExportar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtExportPasta.Text))
        {
            MessageBox.Show(
                "Selecione a pasta de destino.",
                "Exportação",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        if (
            ChkExportEstoque.IsChecked != true
            && ChkExportProdutos.IsChecked != true
            && ChkExportEnderecos.IsChecked != true
            && ChkExportLotes.IsChecked != true
        )
        {
            MessageBox.Show(
                "Selecione ao menos um tipo de dado.",
                "Exportação",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        TxtExportStatus.Foreground = System.Windows.Media.Brushes.Gray;
        TxtExportStatus.Text = "Exportando…";

        try
        {
            var svc = new CEB.Infrastructure.Export.ExportService();
            var pasta = TxtExportPasta.Text;
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var arquivos = new System.Collections.Generic.List<string>();

            if (RbXlsx.IsChecked == true)
            {
                var path = System.IO.Path.Combine(pasta, $"estoque_{ts}.xlsx");
                svc.ExportarXlsx(
                    path,
                    ChkExportEstoque.IsChecked == true ? _db.ListarEstoque() : null,
                    ChkExportProdutos.IsChecked == true ? _db.ListarProdutos() : null,
                    ChkExportEnderecos.IsChecked == true ? _db.ListarEnderecos() : null,
                    ChkExportLotes.IsChecked == true ? _db.ListarLotes() : null
                );
                arquivos.Add(path);
            }
            else
            {
                if (ChkExportEstoque.IsChecked == true)
                {
                    var f = System.IO.Path.Combine(pasta, $"estoque_{ts}.csv");
                    svc.ExportarCsvEstoque(f, _db.ListarEstoque());
                    arquivos.Add(f);
                }
                if (ChkExportProdutos.IsChecked == true)
                {
                    var f = System.IO.Path.Combine(pasta, $"produtos_{ts}.csv");
                    svc.ExportarCsvProdutos(f, _db.ListarProdutos());
                    arquivos.Add(f);
                }
                if (ChkExportEnderecos.IsChecked == true)
                {
                    var f = System.IO.Path.Combine(pasta, $"enderecos_{ts}.csv");
                    svc.ExportarCsvEnderecos(f, _db.ListarEnderecos());
                    arquivos.Add(f);
                }
                if (ChkExportLotes.IsChecked == true)
                {
                    var f = System.IO.Path.Combine(pasta, $"lotes_{ts}.csv");
                    svc.ExportarCsvLotes(f, _db.ListarLotes());
                    arquivos.Add(f);
                }
            }

            TxtExportStatus.Foreground = System.Windows.Media.Brushes.Green;
            TxtExportStatus.Text =
                $"✔ {arquivos.Count} arquivo(s) exportado(s):\n{string.Join("\n", arquivos)}";
        }
        catch (Exception ex)
        {
            TxtExportStatus.Foreground = System.Windows.Media.Brushes.Red;
            TxtExportStatus.Text = $"Erro na exportação: {ex.Message}";
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ETIQUETAS (geração de imagens PNG)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Gera o PNG da etiqueta de um produto após escolha do destino.</summary>
    private void MenuEtiquetaProduto_Click(object sender, RoutedEventArgs e)
    {
        if (DgProdutos.SelectedItem is not Produto p)
        {
            MessageBox.Show("Selecione um produto.", "Aviso");
            return;
        }
        GerarEtiqueta(
            sugestao: $"etiqueta_produto_{Sanitizar(p.Codigo)}.png",
            executar: path =>
                new CEB.Infrastructure.Labels.LabelImageService().GerarEtiquetaProduto(p, path)
        );
    }

    /// <summary>Gera o PNG da etiqueta de um endereço após escolha do destino.</summary>
    private void MenuEtiquetaEndereco_Click(object sender, RoutedEventArgs e)
    {
        if (DgEnderecos.SelectedItem is not Endereco en)
        {
            MessageBox.Show("Selecione um endereço.", "Aviso");
            return;
        }
        GerarEtiqueta(
            sugestao: $"etiqueta_endereco_{Sanitizar(en.Codigo)}.png",
            executar: path =>
                new CEB.Infrastructure.Labels.LabelImageService().GerarEtiquetaEndereco(en, path)
        );
    }

    /// <summary>Atalho: gera etiqueta do produto a partir do item de estoque selecionado.</summary>
    private void MenuEtiquetaProdutoDoEstoque_Click(object sender, RoutedEventArgs e)
    {
        if (DgEstoque.SelectedItem is not ItemEstoque it)
            return;
        var prod = _db.ListarProdutos().FirstOrDefault(x => x.Id == it.ProdutoId);
        if (prod is null)
        {
            MessageBox.Show("Produto não encontrado.", "Aviso");
            return;
        }
        GerarEtiqueta(
            sugestao: $"etiqueta_produto_{Sanitizar(prod.Codigo)}.png",
            executar: path =>
                new CEB.Infrastructure.Labels.LabelImageService().GerarEtiquetaProduto(prod, path)
        );
    }

    /// <summary>Atalho: gera etiqueta do endereço a partir do item de estoque selecionado.</summary>
    private void MenuEtiquetaEnderecoDoEstoque_Click(object sender, RoutedEventArgs e)
    {
        if (DgEstoque.SelectedItem is not ItemEstoque it)
            return;
        var end = _db.ListarEnderecos().FirstOrDefault(x => x.Id == it.EnderecoId);
        if (end is null)
        {
            MessageBox.Show("Endereço não encontrado.", "Aviso");
            return;
        }
        GerarEtiqueta(
            sugestao: $"etiqueta_endereco_{Sanitizar(end.Codigo)}.png",
            executar: path =>
                new CEB.Infrastructure.Labels.LabelImageService().GerarEtiquetaEndereco(end, path)
        );
    }

    /// <summary>
    /// Exibe o diálogo de salvamento de PNG e executa a ação <paramref name="executar"/>
    /// para gerar a etiqueta, tratando eventuais exceções com mensagem ao usuário.
    /// </summary>
    private void GerarEtiqueta(string sugestao, Action<string> executar)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Salvar etiqueta como…",
            FileName = sugestao,
            DefaultExt = ".png",
            Filter = "Imagem PNG (*.png)|*.png",
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            executar(dlg.FileName);
            MessageBox.Show(
                $"Etiqueta gerada com sucesso:\n{dlg.FileName}",
                "Etiqueta",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Falha ao gerar etiqueta:\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>Remove caracteres inválidos de nome de arquivo.</summary>
    private static string Sanitizar(string s)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var arr = s.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(arr).Trim();
    }
}
