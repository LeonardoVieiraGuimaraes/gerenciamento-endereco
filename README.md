# Gerenciamento de Endereços 🚀

Um sistema completo de gerenciamento de endereços construído com as melhores e mais modernas práticas do ecossistema .NET, contando com arquitetura em microsserviços usando Docker, autenticação OIDC moderna e uma interface de usuário super limpa e responsiva (Glassmorphism).

## 🛠️ Tecnologias Utilizadas

- **Backend:** .NET 8 (ASP.NET Core MVC & Web API)
- **Banco de Dados API:** SQL Server 2022
- **Banco de Dados Autenticação:** PostgreSQL 15
- **Autenticação (IAM):** Keycloak 26.1 (OpenID Connect / OAuth 2.0)
- **Containers e Orquestração:** Docker & Docker Compose
- **Documentação de API:** Swagger (Swashbuckle.AspNetCore v6.5.0)
- **Manipulação de Dados (Opcionais):** CsvHelper (Para exportação e importação otimizada)
- **Design:** CSS Vanilla Moderno (Glassmorphism, Inter font, Micro-animações)

## 🐳 Como Executar (Tudo em um único passo!)

O projeto foi refatorado para que **toda a infraestrutura** rode a partir de um único `docker-compose.yml` localizado na pasta `backend`.

1. Certifique-se de ter o [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando.
2. Navegue até a pasta `backend`:
   ```bash
   cd backend
   ```
3. Suba toda a stack com o Docker Compose:
   ```bash
   docker-compose up -d --build
   ```

Isso irá subir 4 containers integrados na mesma rede (`backend_default`):
- `sqlserver-gerenciamento-endereco`: O banco de dados da aplicação.
- `keycloak-db`: O banco de dados do Keycloak (PostgreSQL).
- `keycloak-gerenciamento-endereco`: O servidor de autenticação Keycloak, já com o *realm* e *theme* importados automaticamente.
- `backend-mvc`: A aplicação web principal e API.

## 🔑 Acessos e URLs

Após os containers subirem (o Keycloak pode levar em torno de 15 segundos na primeira inicialização), os seguintes serviços estarão disponíveis:

### 1. Aplicação Principal (ASP.NET MVC)
- **URL:** [http://localhost:5000](http://localhost:5000)
- Aqui você tem a interface final do usuário e os fluxos de login protegidos pelo IdentityModel.

### 2. Documentação da API (Swagger)
- **URL:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- Interface interativa do Swagger UI para testar as rotas (caso expostas no formato de API).

### 3. Keycloak Admin Console
- **URL:** [http://localhost:8089](http://localhost:8089)
- **Usuário:** `admin`
- **Senha:** `admin123`
- *O Realm "gerenciamento-endereco" já vem pré-configurado!*

## 📚 Melhorias na Documentação e Implementação

Atendendo aos requisitos solicitados:
1. **Bibliotecas Opcionais:** 
   - Foi integrado o `CsvHelper` e `Swashbuckle.AspNetCore`. O CsvHelper facilita a leitura/escrita de relatórios CSV caso sua aplicação precise importar dados governamentais de endereços (Correios, IBGE, etc).
   - O `Swagger` está instalado para documentar os endpoints.
2. **Git Ignore:**
   - Adicionado o arquivo `.gitignore` global e padronizado na raiz do projeto para impedir commits de pastas como `bin/`, `obj/`, `sqldata/` e `keycloak_data/`.
3. **Orquestração Única:**
   - Todo o ecossistema (Keycloak, Postgres, SqlServer e API) foi movido e centralizado em um único arquivo `docker-compose.yml` na pasta `backend`. Agora você só precisa de um comando para subir todo o ambiente de ponta a ponta.
4. **Design Glassmorphism:**
   - A interface web MVC (`Views/Home/Index.cshtml`) foi redesenhada usando um CSS limpo, moderno, com transparências, desfoques e animações atraentes.

## 🐛 Solução de Problemas Comuns

- Se a página redirecionar para um login e der **"Server Not Found"**, aguarde 15 a 30 segundos, pois o container do Keycloak pode ainda estar subindo.
- Se no console aparecer o erro `IDX20807` ou `IDX20803`, isso foi corrigido! Era o conflito de hostnames entre as redes Docker. O projeto já está configurado com `OpenIdConnectEvents` que faz o rewrite transparente das URIs, permitindo comunicação server-to-server (`keycloak-gerenciamento-endereco:8080`) e browser-to-server (`localhost:8089`).

---
Desenvolvido com 💚 e as melhores tecnologias.
