using System.Globalization;
using CEB.Domain.Aggregates;
using CEB.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace CEB.Infrastructure.Data;

/// <summary>
/// Serviço de acesso a dados responsável por todas as operações de persistência
/// no banco de dados SQLite local do sistema de controle de estoque.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    /// <summary>
    /// Inicializa o serviço de banco de dados e cria as tabelas necessárias caso não existam.
    /// </summary>
    /// <param name="dbPath">Caminho completo para o arquivo do banco de dados SQLite.</param>
    public DatabaseService(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// Cria as tabelas do banco de dados caso ainda não existam e aplica migrações necessárias.
    /// </summary>
    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Ativa o modo WAL (persiste no arquivo do banco — executado uma única vez)
        // e configura PRAGMAs de desempenho para esta sessão
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"PRAGMA journal_mode = WAL;
              PRAGMA foreign_keys = ON;
              PRAGMA synchronous   = NORMAL;
              PRAGMA cache_size    = -8000;
              PRAGMA temp_store    = MEMORY;";
        cmd.ExecuteNonQuery();

        // Cria as tabelas e índices caso ainda não existam
        cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Produtos (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo          TEXT NOT NULL UNIQUE,
                Nome            TEXT NOT NULL,
                Descricao       TEXT,
                Unidade         TEXT NOT NULL,
                Categoria       TEXT,
                Patrimonio      TEXT,
                DataCadastro    TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
                DataAtualizacao TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime'))
            );

            CREATE TABLE IF NOT EXISTS Enderecos (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Setor           TEXT NOT NULL,
                Rua             TEXT NOT NULL,
                Coluna          TEXT NOT NULL,
                Nivel           TEXT NOT NULL,
                DataCadastro    TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
                DataAtualizacao TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
                UNIQUE (Setor, Rua, Coluna, Nivel)
            );

            CREATE TABLE IF NOT EXISTS Estoque (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                ProdutoId  INTEGER NOT NULL REFERENCES Produtos(Id),
                EnderecoId INTEGER NOT NULL REFERENCES Enderecos(Id),
                Quantidade REAL    NOT NULL DEFAULT 0,
                UNIQUE (ProdutoId, EnderecoId)
            );

            CREATE TABLE IF NOT EXISTS Lotes (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                ProdutoId       INTEGER NOT NULL REFERENCES Produtos(Id),
                Lote            TEXT    NOT NULL,
                DataFabricacao  TEXT    NOT NULL,
                DataValidade    TEXT    NOT NULL,
                Quantidade      REAL    NOT NULL DEFAULT 0,
                Observacao      TEXT,
                DataCadastro    TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
                DataAtualizacao TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime'))
            );

            CREATE TABLE IF NOT EXISTS Epis (
                Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo            TEXT NOT NULL UNIQUE,
                Nome              TEXT NOT NULL,
                Descricao         TEXT,
                NumeroCa          TEXT,
                ValidadeCa        TEXT NOT NULL,
                Quantidade        REAL NOT NULL DEFAULT 0,
                EstadoConservacao TEXT NOT NULL DEFAULT 'Bom',
                Responsavel       TEXT,
                Setor             TEXT,
                Observacao        TEXT,
                DataCadastro      TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
                DataAtualizacao   TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime'))
            );

            CREATE INDEX IF NOT EXISTS IX_Produtos_Nome      ON Produtos(Nome);
            CREATE INDEX IF NOT EXISTS IX_Produtos_Categoria ON Produtos(Categoria);
            CREATE INDEX IF NOT EXISTS IX_Lotes_ProdutoId    ON Lotes(ProdutoId);
            CREATE INDEX IF NOT EXISTS IX_Lotes_DataValidade ON Lotes(DataValidade);
            CREATE INDEX IF NOT EXISTS IX_Estoque_ProdutoId  ON Estoque(ProdutoId);
            CREATE INDEX IF NOT EXISTS IX_Estoque_EnderecoId ON Estoque(EnderecoId);
            CREATE INDEX IF NOT EXISTS IX_Epis_Nome          ON Epis(Nome);
            CREATE INDEX IF NOT EXISTS IX_Epis_ValidadeCa    ON Epis(ValidadeCa);

            CREATE TABLE IF NOT EXISTS Historico (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Tipo      TEXT NOT NULL DEFAULT '',
                Acao      TEXT NOT NULL DEFAULT '',
                Codigo    TEXT NOT NULL DEFAULT '',
                Nome      TEXT NOT NULL DEFAULT '',
                Quantidade REAL,
                Detalhes  TEXT NOT NULL DEFAULT '',
                DataHora  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime'))
            );

            CREATE INDEX IF NOT EXISTS IX_Historico_DataHora ON Historico(DataHora);
            CREATE INDEX IF NOT EXISTS IX_Historico_Tipo     ON Historico(Tipo);
            CREATE INDEX IF NOT EXISTS IX_Historico_Nome     ON Historico(Nome);
        ";
        cmd.ExecuteNonQuery();

        // Migrações: adiciona colunas em bancos de dados existentes
        var migrations = new[]
        {
            "ALTER TABLE Produtos   ADD COLUMN Patrimonio      TEXT",
            "ALTER TABLE Produtos   ADD COLUMN DataCadastro    TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Produtos   ADD COLUMN DataAtualizacao TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Enderecos  ADD COLUMN DataCadastro    TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Enderecos  ADD COLUMN DataAtualizacao TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Lotes      ADD COLUMN DataCadastro    TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Lotes      ADD COLUMN DataAtualizacao TEXT NOT NULL DEFAULT ''",
        };
        foreach (var sql in migrations)
        {
            try
            {
                var alt = conn.CreateCommand();
                alt.CommandText = sql;
                alt.ExecuteNonQuery();
            }
            catch
            { /* coluna já existe */
            }
        }

        // Garante que a tabela Historico exista mesmo em bancos criados antes desta versão
        var cmdHistorico = conn.CreateCommand();
        cmdHistorico.CommandText =
            @"CREATE TABLE IF NOT EXISTS Historico (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Tipo      TEXT NOT NULL DEFAULT '',
                Acao      TEXT NOT NULL DEFAULT '',
                Codigo    TEXT NOT NULL DEFAULT '',
                Nome      TEXT NOT NULL DEFAULT '',
                Quantidade REAL,
                Detalhes  TEXT NOT NULL DEFAULT '',
                DataHora  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime'))
            );
            CREATE INDEX IF NOT EXISTS IX_Historico_DataHora ON Historico(DataHora);
            CREATE INDEX IF NOT EXISTS IX_Historico_Tipo     ON Historico(Tipo);
            CREATE INDEX IF NOT EXISTS IX_Historico_Nome     ON Historico(Nome);";
        cmdHistorico.ExecuteNonQuery();
    }

    // ── Auxiliar de conexão ───────────────────────────────────────────────

    /// <summary>
    /// Abre uma nova conexão SQLite e aplica PRAGMAs de desempenho por sessão.
    /// </summary>
    private SqliteConnection AbrirConexao()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"PRAGMA foreign_keys = ON;
              PRAGMA synchronous   = NORMAL;
              PRAGMA cache_size    = -8000;
              PRAGMA temp_store    = MEMORY;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // ── Produtos ──────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna a lista de produtos, opcionalmente filtrada por código, nome, categoria ou patrimônio.
    /// </summary>
    /// <param name="filtro">Texto parcial para filtrar resultados. Use vazio para listar todos.</param>
    public List<Produto> ListarProdutos(string filtro = "")
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(filtro)
            ? "SELECT Id, Codigo, Nome, Descricao, Unidade, Categoria, Patrimonio, DataCadastro, DataAtualizacao FROM Produtos ORDER BY Nome"
            : "SELECT Id, Codigo, Nome, Descricao, Unidade, Categoria, Patrimonio, DataCadastro, DataAtualizacao FROM Produtos WHERE Codigo LIKE $f OR Nome LIKE $f OR Categoria LIKE $f OR Patrimonio LIKE $f ORDER BY Nome";
        if (!string.IsNullOrWhiteSpace(filtro))
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");

        var lista = new List<Produto>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lista.Add(MapProduto(reader));
        return lista;
    }

    /// <summary>
    /// Busca um produto pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <returns>O produto encontrado, ou <see langword="null"/> se não existir.</returns>
    public Produto? BuscarProduto(int id)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Codigo, Nome, Descricao, Unidade, Categoria, Patrimonio, DataCadastro, DataAtualizacao FROM Produtos WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapProduto(reader) : null;
    }

    /// <summary>
    /// Insere um novo produto ou atualiza um existente no banco de dados.
    /// </summary>
    /// <param name="p">Produto a ser salvo. Se <c>Id == 0</c>, será inserido; caso contrário, atualizado.</param>
    public void SalvarProduto(Produto p)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        var agora = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        var acao = p.Id == 0 ? "Cadastro" : "Atualização";
        if (p.Id == 0)
        {
            cmd.CommandText =
                @"INSERT INTO Produtos (Codigo, Nome, Descricao, Unidade, Categoria, Patrimonio, DataCadastro, DataAtualizacao)
                                VALUES ($c,$n,$d,$u,$cat,$pat,$dc,$da)";
            cmd.Parameters.AddWithValue("$dc", agora);
        }
        else
        {
            cmd.CommandText =
                @"UPDATE Produtos SET Codigo=$c, Nome=$n, Descricao=$d,
                                Unidade=$u, Categoria=$cat, Patrimonio=$pat, DataAtualizacao=$da WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", p.Id);
        }
        cmd.Parameters.AddWithValue("$c", p.Codigo);
        cmd.Parameters.AddWithValue("$n", p.Nome);
        cmd.Parameters.AddWithValue("$d", p.Descricao);
        cmd.Parameters.AddWithValue("$u", p.Unidade);
        cmd.Parameters.AddWithValue("$cat", p.Categoria);
        cmd.Parameters.AddWithValue("$pat", p.Patrimonio);
        cmd.Parameters.AddWithValue("$da", agora);
        cmd.ExecuteNonQuery();
        InsertHistorico(
            conn,
            "Produto",
            acao,
            p.Codigo,
            p.Nome,
            null,
            string.IsNullOrWhiteSpace(p.Categoria) ? "" : $"Categoria: {p.Categoria}"
        );
    }

    /// <summary>
    /// Remove um produto e todos os seus registros de estoque associados.
    /// </summary>
    /// <param name="id">Identificador do produto a excluir.</param>
    public void ExcluirProduto(int id)
    {
        using var conn = AbrirConexao();
        using var tr = conn.BeginTransaction();
        var cmd = conn.CreateCommand();
        cmd.Transaction = tr;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.CommandText = "DELETE FROM Lotes   WHERE ProdutoId=$id";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DELETE FROM Estoque WHERE ProdutoId=$id";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DELETE FROM Produtos WHERE Id=$id";
        cmd.ExecuteNonQuery();
        tr.Commit();
    }

    // ── Endereços ─────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna a lista de endereços, opcionalmente filtrada pelo código de endereçamento.
    /// </summary>
    /// <param name="filtro">Texto parcial para filtrar pelo código Setor-Rua-Coluna-Nível.</param>
    public List<Endereco> ListarEnderecos(string filtro = "")
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(filtro)
            ? "SELECT Id, Setor, Rua, Coluna, Nivel, DataCadastro, DataAtualizacao FROM Enderecos ORDER BY Setor, Rua, Coluna, Nivel"
            : @"SELECT Id, Setor, Rua, Coluna, Nivel, DataCadastro, DataAtualizacao FROM Enderecos
                WHERE (Setor || '-' || Rua || '-' || Coluna || '-' || Nivel) LIKE $f
                ORDER BY Setor, Rua, Coluna, Nivel";
        if (!string.IsNullOrWhiteSpace(filtro))
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");

        var lista = new List<Endereco>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lista.Add(MapEndereco(reader));
        return lista;
    }

    /// <summary>
    /// Insere um novo endereço ou atualiza um existente no banco de dados.
    /// </summary>
    /// <param name="e">Endereço a ser salvo. Se <c>Id == 0</c>, será inserido; caso contrário, atualizado.</param>
    public void SalvarEndereco(Endereco e)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        var agora = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        if (e.Id == 0)
        {
            cmd.CommandText =
                "INSERT INTO Enderecos (Setor, Rua, Coluna, Nivel, DataCadastro, DataAtualizacao) VALUES ($s,$r,$c,$n,$dc,$da)";
            cmd.Parameters.AddWithValue("$dc", agora);
        }
        else
        {
            cmd.CommandText =
                "UPDATE Enderecos SET Setor=$s, Rua=$r, Coluna=$c, Nivel=$n, DataAtualizacao=$da WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", e.Id);
        }
        cmd.Parameters.AddWithValue("$s", e.Setor);
        cmd.Parameters.AddWithValue("$r", e.Rua);
        cmd.Parameters.AddWithValue("$c", e.Coluna);
        cmd.Parameters.AddWithValue("$n", e.Nivel);
        cmd.Parameters.AddWithValue("$da", agora);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Remove um endereço e todos os seus registros de estoque associados.
    /// </summary>
    /// <param name="id">Identificador do endereço a excluir.</param>
    public void ExcluirEndereco(int id)
    {
        using var conn = AbrirConexao();
        using var tr = conn.BeginTransaction();
        var cmd = conn.CreateCommand();
        cmd.Transaction = tr;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.CommandText = "DELETE FROM Estoque   WHERE EnderecoId=$id";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DELETE FROM Enderecos WHERE Id=$id";
        cmd.ExecuteNonQuery();
        tr.Commit();
    }

    // ── Estoque ───────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna os itens de estoque com dados de produto e endereço, opcionalmente filtrados.
    /// </summary>
    /// <param name="filtro">Texto parcial para filtrar por código/nome de produto ou código de endereço.</param>
    public List<ItemEstoque> ListarEstoque(string filtro = "")
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"
            SELECT e.Id, e.ProdutoId, e.EnderecoId, e.Quantidade,
                   p.Codigo, p.Nome, p.Unidade,
                   (en.Setor || '-' || en.Rua || '-' || en.Coluna || '-' || en.Nivel) AS EndCod
            FROM Estoque e
            JOIN Produtos  p  ON p.Id  = e.ProdutoId
            JOIN Enderecos en ON en.Id = e.EnderecoId";

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            cmd.CommandText +=
                @"
            WHERE p.Codigo LIKE $f OR p.Nome LIKE $f
               OR (en.Setor || '-' || en.Rua || '-' || en.Coluna || '-' || en.Nivel) LIKE $f";
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");
        }
        cmd.CommandText += " ORDER BY p.Nome, EndCod";

        var lista = new List<ItemEstoque>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(
                new ItemEstoque
                {
                    Id = reader.GetInt32(0),
                    ProdutoId = reader.GetInt32(1),
                    EnderecoId = reader.GetInt32(2),
                    Quantidade = reader.GetDecimal(3),
                    ProdutoCodigo = reader.GetString(4),
                    ProdutoNome = reader.GetString(5),
                    ProdutoUnidade = reader.GetString(6),
                    EnderecoCode = reader.GetString(7),
                }
            );
        }
        return lista;
    }

    /// <summary>
    /// Registra uma entrada ou saída de estoque para um produto em um endereço.
    /// Utiliza INSERT OR UPDATE (upsert) para acumular quantidade.
    /// </summary>
    /// <param name="produtoId">Identificador do produto.</param>
    /// <param name="enderecoId">Identificador do endereço de armazenamento.</param>
    /// <param name="quantidade">Quantidade a movimentar. Use valor negativo para saída.</param>
    public void MovimentarEstoque(int produtoId, int enderecoId, decimal quantidade)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"
            INSERT INTO Estoque (ProdutoId, EnderecoId, Quantidade)
            VALUES ($p, $e, $q)
            ON CONFLICT(ProdutoId, EnderecoId)
            DO UPDATE SET Quantidade = Quantidade + excluded.Quantidade";
        cmd.Parameters.AddWithValue("$p", produtoId);
        cmd.Parameters.AddWithValue("$e", enderecoId);
        cmd.Parameters.AddWithValue("$q", quantidade);
        cmd.ExecuteNonQuery();
        var (movProdCod, movProdNome) = GetProdutoInfo(conn, produtoId);
        var movEndCode = GetEnderecoCode(conn, enderecoId);
        InsertHistorico(
            conn,
            "Estoque",
            quantidade >= 0 ? "Entrada" : "Saída",
            movProdCod,
            movProdNome,
            Math.Abs(quantidade),
            $"Endereço: {movEndCode}"
        );
    }

    /// <summary>
    /// Ajusta diretamente a quantidade de um item de estoque para o valor informado.
    /// </summary>
    /// <param name="itemId">Identificador do registro de estoque.</param>
    /// <param name="novaQuantidade">Nova quantidade absoluta a ser definida.</param>
    public void AjustarEstoque(int itemId, decimal novaQuantidade)
    {
        using var conn = AbrirConexao();

        // Busca informações do item para registrar no histórico
        var infoCmd = conn.CreateCommand();
        infoCmd.CommandText =
            @"SELECT p.Codigo, p.Nome, en.Setor||'-'||en.Rua||'-'||en.Coluna||'-'||en.Nivel
              FROM Estoque e
              JOIN Produtos  p  ON p.Id  = e.ProdutoId
              JOIN Enderecos en ON en.Id = e.EnderecoId
              WHERE e.Id=$id";
        infoCmd.Parameters.AddWithValue("$id", itemId);
        string ajProdCod = "",
            ajProdNome = "",
            ajEndCode = "";
        using (var r = infoCmd.ExecuteReader())
        {
            if (r.Read())
            {
                ajProdCod = r.GetString(0);
                ajProdNome = r.GetString(1);
                ajEndCode = r.GetString(2);
            }
        }

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Estoque SET Quantidade=$q WHERE Id=$id";
        cmd.Parameters.AddWithValue("$q", novaQuantidade);
        cmd.Parameters.AddWithValue("$id", itemId);
        cmd.ExecuteNonQuery();
        InsertHistorico(
            conn,
            "Estoque",
            "Ajuste",
            ajProdCod,
            ajProdNome,
            novaQuantidade,
            $"Endereço: {ajEndCode}"
        );
    }

    /// <summary>
    /// Remove um registro específico da tabela de estoque.
    /// </summary>
    /// <param name="id">Identificador do item de estoque a excluir.</param>
    public void ExcluirItemEstoque(int id)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Estoque WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Auxiliares de mapeamento ──────────────────────────────────────────

    /// <summary>Mapeia uma linha do <see cref="SqliteDataReader"/> para um <see cref="Produto"/>.</summary>
    private static Produto MapProduto(SqliteDataReader r) =>
        new()
        {
            Id = r.GetInt32(0),
            Codigo = r.GetString(1),
            Nome = r.GetString(2),
            Descricao = r.IsDBNull(3) ? "" : r.GetString(3),
            Unidade = r.GetString(4),
            Categoria = r.IsDBNull(5) ? "" : r.GetString(5),
            Patrimonio = r.IsDBNull(6) ? "" : r.GetString(6),
            DataCadastro = DateTime.TryParse(r.IsDBNull(7) ? "" : r.GetString(7), out var dcP)
                ? dcP
                : DateTime.MinValue,
            DataAtualizacao = DateTime.TryParse(r.IsDBNull(8) ? "" : r.GetString(8), out var daP)
                ? daP
                : DateTime.MinValue,
        };

    /// <summary>Mapeia uma linha do <see cref="SqliteDataReader"/> para um <see cref="Endereco"/>.</summary>
    private static Endereco MapEndereco(SqliteDataReader r) =>
        new()
        {
            Id = r.GetInt32(0),
            Setor = r.GetString(1),
            Rua = r.GetString(2),
            Coluna = r.GetString(3),
            Nivel = r.GetString(4),
            DataCadastro = DateTime.TryParse(r.IsDBNull(5) ? "" : r.GetString(5), out var dcE)
                ? dcE
                : DateTime.MinValue,
            DataAtualizacao = DateTime.TryParse(r.IsDBNull(6) ? "" : r.GetString(6), out var daE)
                ? daE
                : DateTime.MinValue,
        };

    // ── Lotes / Validade ─────────────────────────────────────────────────

    /// <summary>
    /// Retorna a lista de lotes de produtos, com opções de filtro por texto e por status de validade.
    /// </summary>
    /// <param name="filtro">Texto parcial para filtrar por código/nome de produto ou número de lote.</param>
    /// <param name="somenteAtivos">Se <see langword="true"/>, retorna apenas lotes ainda dentro da validade.</param>
    public List<LoteProduto> ListarLotes(string filtro = "", bool somenteAtivos = false)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"
            SELECT l.Id, l.ProdutoId, l.Lote, l.DataFabricacao, l.DataValidade,
                   l.Quantidade, l.Observacao, l.DataCadastro, l.DataAtualizacao,
                   p.Codigo, p.Nome, p.Unidade
            FROM Lotes l
            JOIN Produtos p ON p.Id = l.ProdutoId
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            cmd.CommandText += " AND (p.Codigo LIKE $f OR p.Nome LIKE $f OR l.Lote LIKE $f)";
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");
        }
        if (somenteAtivos)
            cmd.CommandText += " AND date(l.DataValidade) >= date('now')";

        cmd.CommandText += " ORDER BY l.DataValidade, p.Nome";

        var lista = new List<LoteProduto>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lista.Add(MapLote(reader));
        return lista;
    }

    /// <summary>
    /// Insere um novo lote ou atualiza um existente no banco de dados.
    /// </summary>
    /// <param name="l">Lote a ser salvo. Se <c>Id == 0</c>, será inserido; caso contrário, atualizado.</param>
    public void SalvarLote(LoteProduto l)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        var agora = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        var acao = l.Id == 0 ? "Cadastro" : "Atualização";
        if (l.Id == 0)
        {
            cmd.CommandText =
                @"INSERT INTO Lotes (ProdutoId, Lote, DataFabricacao, DataValidade, Quantidade, Observacao, DataCadastro, DataAtualizacao)
                                VALUES ($p,$lo,$df,$dv,$q,$obs,$dc,$da)";
            cmd.Parameters.AddWithValue("$dc", agora);
        }
        else
        {
            cmd.CommandText =
                @"UPDATE Lotes SET ProdutoId=$p, Lote=$lo, DataFabricacao=$df,
                                DataValidade=$dv, Quantidade=$q, Observacao=$obs, DataAtualizacao=$da WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", l.Id);
        }
        cmd.Parameters.AddWithValue("$p", l.ProdutoId);
        cmd.Parameters.AddWithValue("$lo", l.Lote);
        cmd.Parameters.AddWithValue("$df", l.DataFabricacao.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$dv", l.DataValidade.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$q", l.Quantidade);
        cmd.Parameters.AddWithValue("$obs", l.Observacao);
        cmd.Parameters.AddWithValue("$da", agora);
        cmd.ExecuteNonQuery();
        var (loteProdCod, loteProdNome) = GetProdutoInfo(conn, l.ProdutoId);
        InsertHistorico(
            conn,
            "Validade",
            acao,
            loteProdCod,
            loteProdNome,
            l.Quantidade,
            $"Lote: {l.Lote}, Validade: {l.DataValidade:dd/MM/yyyy}"
        );
    }

    /// <summary>
    /// Remove um lote de produto pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador do lote a excluir.</param>
    public void ExcluirLote(int id)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Lotes WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Mapeia uma linha do <see cref="SqliteDataReader"/> para um <see cref="LoteProduto"/>.</summary>
    private static LoteProduto MapLote(SqliteDataReader r) =>
        new()
        {
            Id = r.GetInt32(0),
            ProdutoId = r.GetInt32(1),
            Lote = r.GetString(2),
            DataFabricacao = DateTime.Parse(r.GetString(3)),
            DataValidade = DateTime.Parse(r.GetString(4)),
            Quantidade = r.GetDecimal(5),
            Observacao = r.IsDBNull(6) ? "" : r.GetString(6),
            DataCadastro = DateTime.TryParse(r.IsDBNull(7) ? "" : r.GetString(7), out var dcL)
                ? dcL
                : DateTime.MinValue,
            DataAtualizacao = DateTime.TryParse(r.IsDBNull(8) ? "" : r.GetString(8), out var daL)
                ? daL
                : DateTime.MinValue,
            ProdutoCodigo = r.GetString(9),
            ProdutoNome = r.GetString(10),
            ProdutoUnidade = r.GetString(11),
        };

    // ── EPIs ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna a lista de EPIs, opcionalmente filtrada por código, nome, CA ou responsável.
    /// </summary>
    public List<Epi> ListarEpis(string filtro = "")
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(filtro)
            ? @"SELECT Id, Codigo, Nome, Descricao, NumeroCa, ValidadeCa, Quantidade,
                       EstadoConservacao, Responsavel, Setor, Observacao, DataCadastro, DataAtualizacao
                FROM Epis ORDER BY Nome"
            : @"SELECT Id, Codigo, Nome, Descricao, NumeroCa, ValidadeCa, Quantidade,
                       EstadoConservacao, Responsavel, Setor, Observacao, DataCadastro, DataAtualizacao
                FROM Epis
                WHERE Codigo LIKE $f OR Nome LIKE $f OR NumeroCa LIKE $f OR Responsavel LIKE $f OR Setor LIKE $f
                ORDER BY Nome";
        if (!string.IsNullOrWhiteSpace(filtro))
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");

        var lista = new List<Epi>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lista.Add(MapEpi(reader));
        return lista;
    }

    /// <summary>
    /// Insere um novo EPI ou atualiza um existente no banco de dados.
    /// </summary>
    public void SalvarEpi(Epi epi)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        var agora = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        var acao = epi.Id == 0 ? "Cadastro" : "Atualização";
        if (epi.Id == 0)
        {
            cmd.CommandText =
                @"INSERT INTO Epis (Codigo, Nome, Descricao, NumeroCa, ValidadeCa, Quantidade,
                                    EstadoConservacao, Responsavel, Setor, Observacao, DataCadastro, DataAtualizacao)
                             VALUES ($co,$n,$d,$ca,$vca,$q,$ec,$resp,$set,$obs,$dc,$da)";
            cmd.Parameters.AddWithValue("$dc", agora);
        }
        else
        {
            cmd.CommandText =
                @"UPDATE Epis SET Codigo=$co, Nome=$n, Descricao=$d, NumeroCa=$ca, ValidadeCa=$vca,
                                  Quantidade=$q, EstadoConservacao=$ec, Responsavel=$resp,
                                  Setor=$set, Observacao=$obs, DataAtualizacao=$da
                  WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", epi.Id);
        }
        cmd.Parameters.AddWithValue("$co", epi.Codigo);
        cmd.Parameters.AddWithValue("$n", epi.Nome);
        cmd.Parameters.AddWithValue("$d", epi.Descricao);
        cmd.Parameters.AddWithValue("$ca", epi.NumeroCa);
        cmd.Parameters.AddWithValue("$vca", epi.ValidadeCa.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$q", epi.Quantidade);
        cmd.Parameters.AddWithValue("$ec", epi.EstadoConservacao.ToString());
        cmd.Parameters.AddWithValue("$resp", epi.Responsavel);
        cmd.Parameters.AddWithValue("$set", epi.Setor);
        cmd.Parameters.AddWithValue("$obs", epi.Observacao);
        cmd.Parameters.AddWithValue("$da", agora);
        cmd.ExecuteNonQuery();
        InsertHistorico(
            conn,
            "EPI",
            acao,
            epi.Codigo,
            epi.Nome,
            epi.Quantidade,
            $"CA: {epi.NumeroCa}, Setor: {epi.Setor}"
        );
    }

    /// <summary>
    /// Remove um EPI pelo seu identificador.
    /// </summary>
    public void ExcluirEpi(int id)
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Epis WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Mapeia uma linha do <see cref="SqliteDataReader"/> para um <see cref="Epi"/>.</summary>
    private static Epi MapEpi(SqliteDataReader r) =>
        new()
        {
            Id = r.GetInt32(0),
            Codigo = r.GetString(1),
            Nome = r.GetString(2),
            Descricao = r.IsDBNull(3) ? "" : r.GetString(3),
            NumeroCa = r.IsDBNull(4) ? "" : r.GetString(4),
            ValidadeCa = DateTime.Parse(r.GetString(5)),
            Quantidade = r.GetDecimal(6),
            EstadoConservacao = Enum.TryParse<EstadoConservacao>(
                r.IsDBNull(7) ? "" : r.GetString(7),
                out var ec
            )
                ? ec
                : EstadoConservacao.Bom,
            Responsavel = r.IsDBNull(8) ? "" : r.GetString(8),
            Setor = r.IsDBNull(9) ? "" : r.GetString(9),
            Observacao = r.IsDBNull(10) ? "" : r.GetString(10),
            DataCadastro = r.IsDBNull(11) ? DateTime.MinValue : DateTime.Parse(r.GetString(11)),
            DataAtualizacao = r.IsDBNull(12) ? DateTime.MinValue : DateTime.Parse(r.GetString(12)),
        };

    // ── Histórico ─────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna os registros de histórico de operações, com filtros opcionais por texto,
    /// tipo de operação e intervalo de datas.
    /// </summary>
    public List<RegistroHistorico> ListarHistorico(
        string filtro = "",
        string tipo = "",
        DateTime? dataInicio = null,
        DateTime? dataFim = null
    )
    {
        using var conn = AbrirConexao();
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Tipo, Acao, Codigo, Nome, Quantidade, Detalhes, DataHora FROM Historico WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            cmd.CommandText +=
                " AND (Codigo LIKE $f OR Nome LIKE $f OR Detalhes LIKE $f OR Acao LIKE $f)";
            cmd.Parameters.AddWithValue("$f", $"%{filtro}%");
        }
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            cmd.CommandText += " AND Tipo=$tipo";
            cmd.Parameters.AddWithValue("$tipo", tipo);
        }
        if (dataInicio.HasValue)
        {
            cmd.CommandText += " AND DataHora >= $di";
            cmd.Parameters.AddWithValue("$di", dataInicio.Value.ToString("yyyy-MM-dd"));
        }
        if (dataFim.HasValue)
        {
            cmd.CommandText += " AND DataHora < $df";
            cmd.Parameters.AddWithValue("$df", dataFim.Value.AddDays(1).ToString("yyyy-MM-dd"));
        }
        cmd.CommandText += " ORDER BY DataHora DESC";

        var lista = new List<RegistroHistorico>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(
                new RegistroHistorico
                {
                    Id = reader.GetInt32(0),
                    Tipo = reader.GetString(1),
                    Acao = reader.GetString(2),
                    Codigo = reader.GetString(3),
                    Nome = reader.GetString(4),
                    Quantidade = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    Detalhes = reader.GetString(6),
                    DataHora = DateTime.Parse(reader.GetString(7)),
                }
            );
        }
        return lista;
    }

    // ── Auxiliares internos de histórico ──────────────────────────────────

    /// <summary>Busca código e nome de um produto pelo seu Id (conexão já aberta).</summary>
    private static (string Codigo, string Nome) GetProdutoInfo(SqliteConnection conn, int produtoId)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Codigo, Nome FROM Produtos WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", produtoId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? (r.GetString(0), r.GetString(1)) : ("", "");
    }

    /// <summary>Retorna o código completo de um endereço pelo seu Id (conexão já aberta).</summary>
    private static string GetEnderecoCode(SqliteConnection conn, int enderecoId)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Setor||'-'||Rua||'-'||Coluna||'-'||Nivel FROM Enderecos WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", enderecoId);
        return cmd.ExecuteScalar()?.ToString() ?? "";
    }

    /// <summary>Insere um registro na tabela Historico usando a conexão fornecida.</summary>
    private static void InsertHistorico(
        SqliteConnection conn,
        string tipo,
        string acao,
        string codigo,
        string nome,
        decimal? quantidade,
        string detalhes
    )
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"INSERT INTO Historico (Tipo, Acao, Codigo, Nome, Quantidade, Detalhes)
              VALUES ($t,$a,$c,$n,$q,$d)";
        cmd.Parameters.AddWithValue("$t", tipo);
        cmd.Parameters.AddWithValue("$a", acao);
        cmd.Parameters.AddWithValue("$c", codigo);
        cmd.Parameters.AddWithValue("$n", nome);
        cmd.Parameters.AddWithValue("$q", (object?)quantidade ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$d", detalhes);
        cmd.ExecuteNonQuery();
    }
}
