# Gerenciamento de Endereços

Projeto de Backend C# utilizando ASP.NET Core MVC e Web API, banco de dados SQL Server (via Docker) e Keycloak como Provedor de Identidade (IdP).

## Requisitos
- **Docker** e **Docker Compose**
- **.NET 8 SDK** (se for rodar fora do Docker)

## Como Rodar

O jeito mais fácil de rodar todo o ecossistema (Banco + App) é via Docker Compose.

1. Na pasta `backend` (onde está o `docker-compose.yml`), execute:
   ```bash
   docker-compose up -d --build
   ```

2. Acesse a aplicação C#:
   ```
   http://localhost:5000
   ```

3. Para ver a documentação da API via **Swagger**, acesse:
   ```
   http://localhost:5000/swagger
   ```

## Bibliotecas Principais Utilizadas
- `Entity Framework Core`: ORM para banco de dados.
- `Swashbuckle.AspNetCore`: Geração do Swagger para a API REST.
- `Serilog`: Logs estruturados e elegantes no console.
- `CsvHelper`: Exportação segura e limpa de dados para CSV.
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`: Integração com o Keycloak.

## Documentações Adicionais
- [Arquitetura do Projeto](ARCHITECTURE.md)
- [Como Configurar o Keycloak](SETUP_KEYCLOAK.md)
