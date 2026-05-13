# CBM LRV Estoque

Sistema desktop de controle de estoque com endereçamento e gerenciamento de validade, desenvolvido para o **Corpo de Bombeiros Militar de Lucas do Rio Verde**.

## Descrição

Aplicação Windows (WPF) para gestão de materiais e suprimentos do CBM-LRV. Permite controlar o inventário com endereçamento físico (setor/rua/coluna/nível), registrar movimentações de entrada e saída, cadastrar lotes com data de validade e emitir alertas de vencimento.

Os dados são persistidos localmente em banco de dados SQLite, sem necessidade de servidor ou conexão de rede.

## Funcionalidades

- **Controle de estoque**: movimentação de entrada e saída por produto e endereço; ajuste direto de quantidade
- **Endereçamento**: localização física de itens por Setor → Rua → Coluna → Nível
- **Cadastro de produtos**: código, nome, descrição, unidade, categoria e patrimônio
- **Lotes e validade**: registro de lote, data de fabricação, data de validade e quantidade; alerta visual de itens vencidos ou próximos ao vencimento
- **Filtros**: pesquisa por texto em todas as telas

## Tecnologias

| Camada       | Tecnologia                                      |
| ------------ | ----------------------------------------------- |
| Interface    | WPF (.NET 10, Windows)                          |
| Persistência | SQLite via `Microsoft.Data.Sqlite` 10.0.8       |
| Arquitetura  | Domain-Driven Design (DDD)                      |
| Publicação   | Single-file, self-contained (win-x86 / win-x64) |

## Estrutura do Projeto

```
Source/
├── Domain/
│   ├── Entities/          # Produto, Endereco
│   └── Aggregates/        # ItemEstoque, LoteProduto
├── Infrastructure/
│   └── Data/              # DatabaseService (SQLite)
└── Presentation/
    ├── Windows/           # MainWindow
    ├── Views/             # CadastroProdutoWindow, CadastroEnderecoWindow, ValidadeWindow
    ├── Converters/        # StatusToColorConverter
    └── Dialogs/           # InputDialog
```

A saída de build segue o padrão:

```
Build/
├── Debug/   x86/  e  x64/    ← builds de desenvolvimento
├── Release/ x86/  e  x64/    ← builds de produção
└── Publish/ Release/ x86/ e x64/   ← executável único auto-contido
```

## Compilação

Requer o **.NET 10 SDK** instalado.

```powershell
# Build x64 Debug
dotnet build -p:Platform=x64 -c Debug

# Build x86 Debug
dotnet build -p:Platform=x86 -c Debug

# Publicar executável único x64 (self-contained)
dotnet publish -p:Platform=x64 -c Release
```

O executável publicado estará em `Build\Publish\Release\x64\cbm-lrv-estoque.exe` e não exige instalação do .NET na máquina de destino.

## Dados

O banco de dados é criado automaticamente na primeira execução em:

```
%LOCALAPPDATA%\controle-estoque\estoque.db
```

---

**Desenvolvido para o Corpo de Bombeiros Militar de Lucas do Rio Verde**
