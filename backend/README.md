# Gerenciamento de Endereços

Projeto de Backend C# utilizando ASP.NET Core MVC e Web API, banco de dados SQL Server (via Docker) e Keycloak como Provedor de Identidade (IdP).

## Requisitos
- **Docker** e **Docker Compose**
- **.NET 8 SDK** (se for rodar fora do Docker)

## Como Rodar

O jeito mais fácil de rodar todo o ecossistema (Banco + App) é via Docker Compose.

1. Na **raiz do repositório** (onde está o `docker-compose.yml`), execute:
   ```bash
   docker compose up -d --build
   ```
   O Compose fica na raiz porque sobe o conjunto todo — aplicação, Keycloak e
   os dois bancos —, não apenas esta pasta.

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
- [Arquitetura do Projeto](../docs/ARCHITECTURE.md)
- [Como Configurar o Keycloak](../docs/SETUP_KEYCLOAK.md)
- [Decisões de arquitetura (ADR)](../docs/adr/)
- [Servidor de identidade](../auth-keycloak/README.md)
