# Gestão de Incidentes

API REST para registro, acompanhamento e gestão de chamados de TI.

Status: 🚧 Em desenvolvimento

# Sobre o projeto

O Gestão de Incidentes é um projeto de desenvolvimento backend que tem como objetivo centralizar o gerenciamento de chamados de Tecnologia da Informação, permitindo o registro de ocorrências, acompanhamento de solicitações, controle de prioridades e rastreabilidade dos atendimentos.

A aplicação foi idealizada para simular processos de sustentação de sistemas utilizados em ambientes corporativos, aplicando conceitos de desenvolvimento de APIs REST, modelagem de banco de dados relacional, SQL, regras de negócio e tratamento de erros.

# Funcionalidades

Estrutura inicial do banco de dados.

Estruturação inicial das classes e rotas da API.

Cadastro e consulta de usuários.

Abertura, listagem e consulta de chamados.

Atualização do status dos incidentes.

Registro do histórico de atendimento.

Consultas SQL para indicadores operacionais.

Gestão de solicitações de mudanças.

Dashboard interativo no Power BI.

Testes automatizados e documentação da API.


# Regras de negócio

O sistema prevê validação de dados obrigatórios, associação dos chamados aos usuários, controle de estados e registro das movimentações relevantes.

As atualizações que envolvam múltiplas operações no banco deverão utilizar transações para preservar a consistência das informações.

# Análise de dados com Power BI

Uma etapa futura do projeto prevê um dashboard para acompanhamento de:

Total de chamados.

Quantidade de incidentes resolvidos.

Taxa de resolução.

Distribuição por departamento.

Distribuição por prioridade e status.

# Objetivo de aprendizagem

Consolidar conhecimentos em desenvolvimento backend, SQL, bancos de dados, APIs REST, testes de software e análise de dados, aproximando a prática de programação dos processos utilizados por equipes de tecnologia em ambientes corporativos.

Projeto em desenvolvimento contínuo.

## Estrutura do repositório

```text
src/
  GestaoIncedentes.Api/
    Program.cs                  # Configuração e registro das rotas
    GestaoIncedentes.csproj      # Dependências e configuração do projeto
    appsettings.json            # Configurações públicas, sem segredos
    Controller/                 # Rotas da API
    Model/                      # Entidades e conexão com o banco
    Properties/                 # Perfil de execução local
requests/
  rotas.http                    # Exemplos para testar os endpoints
```

## Executar localmente

Requisitos: .NET SDK 8 e MySQL com as tabelas do projeto.

Antes de iniciar, configure uma chave JWT exclusiva com pelo menos 32 caracteres
aleatórios e a conexão do seu banco. No PowerShell, use variáveis de ambiente:

```powershell
$env:Jwt__Key = '<sua-chave-aleatoria-exclusiva>'
$env:ConnectionStrings__GestaoIncedentes = 'Server=localhost;Port=3306;Database=GestaoIncedentes;User ID=<usuario>;Password=<senha>;'
dotnet run --project src/GestaoIncedentes.Api/GestaoIncedentes.csproj
```

A API estará disponível em `http://localhost:5070`, com Swagger em `/swagger`.
Os exemplos de `requests/rotas.http` usam essa mesma URL.

Para compilar:

```powershell
dotnet build src/GestaoIncedentes.Api/GestaoIncedentes.csproj
```

## Configurações privadas

Nunca publique chaves JWT reais, senhas, tokens ou configurações privadas.
O `appsettings.json` versionado deixa a chave e a conexão vazias: a aplicação
exige esses valores na configuração antes de iniciar.

Opcionalmente, no ambiente Development, esses valores podem ser definidos em
`src/GestaoIncedentes.Api/appsettings.Development.json`, exclusivamente local.
Mantenha esse arquivo fora do Git. Como este repositório não versiona `.gitignore`,
configure as exclusões no arquivo local `.git/info/exclude` de cada clone:

```text
.gitignore
**/bin/
**/obj/
**/appsettings.Development.json
.vs/
*.user
*.suo
```

Colocar arquivos em pastas organiza o projeto, mas não impede que sejam lidos
em um repositório público. Uma chave já publicada deve ser substituída; remover
seu valor da versão atual não apaga os commits anteriores.
