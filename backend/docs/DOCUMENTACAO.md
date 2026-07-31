# 📖 Documentação do Backend (Gerenciamento de Endereços)

Bem-vindo à documentação da API de Gerenciamento de Endereços! Este documento descreve a estrutura do projeto, as bibliotecas utilizadas e como executá-lo.

## 🏗️ Arquitetura e Tecnologias

O backend foi construído utilizando as seguintes tecnologias:

- **Framework**: ASP.NET Core MVC (.NET 8)
- **Linguagem**: C# 12
- **ORM**: Entity Framework Core 8
- **Banco de Dados**: SQL Server
- **Autenticação**: OpenID Connect via Keycloak (configurado na pasta `/auth`)

## 📦 Pacotes Principais

As dependências já configuradas no arquivo `.csproj` são:
- **`Microsoft.EntityFrameworkCore.SqlServer`**: É o "provider" (provedor) do Entity Framework Core específico para o Microsoft SQL Server. Ele contém todas as lógicas necessárias para traduzir o código C# (LINQ) para comandos SQL compreendidos nativamente pelo banco de dados SQL Server.
- **`Microsoft.EntityFrameworkCore.Design`**: É um pacote fundamental para o funcionamento das ferramentas de tempo de desenvolvimento (design-time). Ele permite que a ferramenta de linha de comando (`dotnet ef`) consiga analisar o seu projeto, entender as configurações do `DbContext` e gerar os códigos das Migrations.
- **`Microsoft.EntityFrameworkCore.Tools`**: Habilita e fornece os comandos específicos para gerenciamento do banco de dados no terminal, como o `dotnet ef migrations add` (para criar o histórico de alterações das tabelas) e o `dotnet ef database update` (para aplicar de fato as alterações da Migration dentro do seu SQL Server).

## 🚀 Como Executar Localmente

### 1. Configurar Banco de Dados
Certifique-se de que o SQL Server está rodando localmente (ou via Docker). 
Em breve, a string de conexão deverá ser configurada no arquivo `appsettings.json`.

### 2. Criar o Banco de Dados (Migrations)
Com o terminal aberto na pasta `backend`, execute os comandos abaixo para criar as tabelas no banco de dados:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Rodar a API
Para iniciar a aplicação em modo de desenvolvimento, utilize:
```bash
dotnet run
```
A API estará disponível por padrão nas portas `http://localhost:5000` e `https://localhost:5001`.

## 📌 Próximos Passos (Checklist Interno)

1. **Configurar o DbContext**: Criar a classe que fará a ponte com o SQL Server.
2. **Criar as Entidades**: `Endereco` e `Usuario`.
3. **Mapeamento de Rotas (Controllers)**:
   - `GET /enderecos`
   - `POST /enderecos`
   - `GET /enderecos/exportar`
4. **Integração Externa**: Consumir a API do **ViaCEP** no `ViaCepService`.
