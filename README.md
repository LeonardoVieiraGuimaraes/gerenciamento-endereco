# Gerenciamento de Endereços

Aplicação web para cadastro e consulta de endereços, com login corporativo,
busca automática por CEP e exportação para CSV.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Keycloak](https://img.shields.io/badge/Keycloak-26-blue?style=flat&logo=keycloak&logoColor=white)](https://www.keycloak.org/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat&logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/sql-server)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker&logoColor=white)](https://www.docker.com/)

**Aplicação no ar:** https://enderecos.leoproti.com.br
**Tela de login:** https://auth-enderecos.leoproti.com.br

> Projeto criado a partir do teste prático para desenvolvedor C#. Os requisitos
> originais — login, CRUD de endereços, integração com ViaCEP, exportação CSV e
> scripts de banco — foram atendidos e, a partir deles, o sistema foi evoluído
> com autenticação profissional, publicação automatizada e reforço de segurança.

---

## O que o sistema faz

- **Login único (SSO)** com opção de verificação em duas etapas (2FA)
- **Cadastro de endereços** — criar, listar, editar e excluir
- **Busca por CEP** — informe o CEP e os demais campos são preenchidos sozinhos
- **Filtros de busca** por nome, CEP, logradouro, cidade e UF
- **Exportação para CSV** dos endereços cadastrados
- **Área administrativa** para gerenciar usuários
- **Tema claro e escuro**, inclusive nas telas de login

---

## Perfis de acesso

| Perfil | O que pode fazer |
|---|---|
| **USUARIO** | Gerencia apenas os próprios endereços |
| **ADMIN** | Tudo do usuário comum, mais gerenciar usuários e ver endereços de qualquer pessoa |

Quem se cadastra pelo site recebe o perfil **USUARIO** automaticamente.

Um ponto importante: esconder um botão na tela não protege nada. Por isso cada
ação também é conferida no servidor — se alguém digitar o endereço da página
direto no navegador sem ter permissão, o acesso é negado do mesmo jeito.

---

## Como está montado

```
Navegador
    │
    ├──► Aplicação web (.NET 8)  ──► Banco da aplicação (SQL Server)
    │            │
    │            └──► API ViaCEP (consulta de CEP)
    │
    └──► Keycloak (login)        ──► Banco do Keycloak (PostgreSQL)
```

A aplicação **não armazena senhas**. Toda a autenticação é delegada ao Keycloak,
um servidor de identidade usado no mercado. Ou seja: senha, 2FA e troca de senha
ficam a cargo de uma ferramenta especializada, e a aplicação apenas recebe a
confirmação de quem é o usuário e do que ele pode fazer.

Tudo roda em containers Docker, o que faz o ambiente local ser igual ao de produção.

---

## Tecnologias

**Aplicação**
- .NET 8 (ASP.NET Core MVC) — back-end e telas
- Entity Framework Core — acesso a dados e versionamento do banco
- SQL Server 2022 — banco da aplicação
- Bootstrap 5 e CSS próprio — interface
- Serilog — registro de logs
- CsvHelper — geração dos arquivos CSV

**Autenticação**
- Keycloak 26 — servidor de identidade (SSO, 2FA, cadastro)
- OpenID Connect / OAuth 2.0 — protocolo de autenticação
- PostgreSQL 16 — banco do Keycloak

**Infraestrutura**
- Docker e Docker Compose
- GitHub Actions — testes e publicação automática
- Cloudflare Tunnel — publicação sem expor portas do servidor

**Qualidade**
- xUnit, Moq e FluentAssertions — testes automatizados
- Swagger — documentação da API

---

## Segurança aplicada

| Item | O que foi feito |
|---|---|
| Senhas | Não passam pela aplicação — ficam sob responsabilidade do Keycloak |
| Autenticação | OpenID Connect, com validação da assinatura e do destinatário do token |
| Autorização | Conferida no servidor em toda ação, não apenas na interface |
| Formulários | Proteção contra falsificação de requisição (CSRF) |
| Limite de requisições | Bloqueio de excesso de tentativas, mais rígido no login |
| Cabeçalhos de segurança | Política de conteúdo (CSP), bloqueio de uso em iframe, entre outros |
| Cookies | Marcados como seguros e inacessíveis a scripts |
| HTTPS | Obrigatório em produção |
| Dependências | Sem pacotes com vulnerabilidade conhecida (checado a cada publicação) |
| Segredos | Fora do código — guardados como segredos do GitHub |
| Banco de dados | Não exposto para fora do servidor |

---

## Organização das pastas

```
gerenciamento-endereco/
├── backend/            Aplicação .NET (telas, API, regras e acesso a dados)
│   ├── Controllers/    Recebe as requisições e decide o que fazer
│   ├── Models/         Representação dos dados (Endereço, Usuário)
│   ├── Services/       Integrações isoladas (ViaCEP, CSV, Keycloak)
│   ├── Views/          Telas da aplicação
│   ├── Data/           Configuração do banco
│   ├── Migrations/     Histórico de mudanças no banco
│   ├── wwwroot/        Arquivos públicos (CSS, JS, imagens)
│   ├── docs/           Documentação técnica e manual de deploy
│   ├── docker-compose.yml       Ambiente completo de desenvolvimento
│   └── docker-compose.prod.yml  Ambiente de produção
│
├── auth-keycloak/      Configuração do servidor de login
│   ├── realm/          Usuários, perfis e aplicações (importado no boot)
│   ├── theme/          Tema visual das telas de login e cadastro
│   └── Dockerfile      Imagem do Keycloak já com tema e realm embutidos
│
├── GerenciamentoEndereco.Tests/   Testes automatizados
│
├── frontend/           Reservado para a interface em Next.js (ver roadmap)
│
└── .github/workflows/  Automação de testes e publicação
```

**Sobre a organização em containers:** existe um único `docker-compose.yml` (em
`backend/`) que sobe os quatro serviços de uma vez — inclusive o Keycloak, cuja
imagem é construída a partir da pasta `auth-keycloak/`. Ou seja, a configuração
de autenticação vive separada do código da aplicação, mas a subida do ambiente é
centralizada num comando só.

**Sobre o front-end:** hoje as telas são geradas pelo próprio .NET (padrão MVC).
A pasta `frontend/` está reservada para a futura interface em Next.js, que passará
a consumir a API já existente.

---

## Como rodar na sua máquina

Só é preciso ter o [Docker](https://www.docker.com/products/docker-desktop/) instalado.

```bash
cd backend
docker compose up -d --build
```

Um comando sobe os quatro serviços. Na primeira vez o Keycloak leva cerca de um
minuto para ficar pronto. As tabelas do banco são criadas automaticamente.

| Serviço | Endereço |
|---|---|
| Aplicação | http://localhost:5000 |
| Documentação da API | http://localhost:5000/swagger |
| Keycloak | http://localhost:8089 |

---

## Publicação automática

A cada envio de código para a branch `main`, o GitHub Actions:

1. Roda os testes automatizados
2. Verifica dependências com vulnerabilidades conhecidas
3. Publica no servidor e sobe os containers
4. Confirma que a aplicação respondeu com sucesso

Se qualquer etapa falhar, a publicação é interrompida e a versão anterior segue
no ar. O passo a passo está em [backend/docs/DEPLOY.md](backend/docs/DEPLOY.md).

---

## Próximos passos

- **Front-end em Next.js (React)** — separar a interface da API, com renderização
  no servidor e base preparada para uma versão mobile em React Native
- **Auditoria** — registrar quem alterou o quê e quando, com tela de consulta
- **Kubernetes** — orquestração com réplicas, escala automática e atualização sem
  indisponibilidade (hoje a orquestração é via Docker Compose)
- **Observabilidade** — métricas e alertas com Prometheus e Grafana
- **Cache com Redis** — reduzir consultas repetidas ao ViaCEP
- **Processamento em segundo plano** — exportações grandes via fila, sem travar a tela
- **Testes end-to-end** — cobrir a jornada completa do usuário no navegador

O acompanhamento das etapas está nas
[issues do repositório](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues).

---

## Documentação técnica

- [Arquitetura](backend/docs/ARCHITECTURE.md)
- [Documentação geral](backend/docs/DOCUMENTACAO.md)
- [Configuração do Keycloak](backend/docs/SETUP_KEYCLOAK.md)
- [Manual de deploy](backend/docs/DEPLOY.md)
- [Scripts do banco](backend/scripts/tabelas.sql)

---

Desenvolvido por **Leonardo Vieira Guimarães**
