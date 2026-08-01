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
- **Área administrativa** para gerenciar usuários — criar, editar e excluir
  contas direto na aplicação, sem abrir o painel do Keycloak
- **Alterar senha e configurar 2FA** pela própria aplicação, usando o fluxo
  nativo do Keycloak (sem tela de terceiro)
- **API REST completa** dos endereços (consultar, criar, editar e excluir), com documentação interativa
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

A verificação não é só "está logado ou não": existem **permissões separadas por
ação** — ler, criar/editar, excluir, exportar, ver a documentação e gerenciar
usuários. Assim é possível, no futuro, liberar leitura para um perfil sem
liberar exclusão, sem reescrever o controle de acesso.

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
| Registro de atividade | Log estruturado de toda requisição, sem gravar dado sensível |
| Sessão | Chaves de criptografia guardadas no banco — reiniciar ou escalar a aplicação não desloga ninguém |

### Sobre os logs

A aplicação usa **Serilog** com log estruturado:

- Toda requisição HTTP é registrada (rota, código de resposta e tempo de execução)
- Níveis ajustados por ambiente — mais detalhe em desenvolvimento, menos em produção
- **Proteção de dado sensível:** a exibição de informação pessoal nos logs
  (`ShowPII`) fica desligada em produção, evitando que token ou dado do usuário
  apareça no registro
- Erros são registrados com a pilha completa, facilitando o diagnóstico

Hoje a saída é o console do container, o que atende o estágio atual. Centralizar
os logs num destino pesquisável está mapeado na
[issue #30](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues/30).

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
├── GerenciamentoEndereco.Tests/   Testes automatizados (39 testes)
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
| Documentação da API (Swagger) | http://localhost:5000/swagger |
| Verificação de saúde | http://localhost:5000/health |
| Keycloak | http://localhost:8089 |

### API REST

Além das telas, a aplicação expõe uma API REST completa para os endereços:

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/enderecos` | Lista os endereços do usuário autenticado (todos, se ADMIN) |
| `GET` | `/api/enderecos/{id}` | Retorna um endereço específico |
| `POST` | `/api/enderecos` | Cadastra um novo endereço |
| `PUT` | `/api/enderecos/{id}` | Atualiza um endereço existente |
| `DELETE` | `/api/enderecos/{id}` | Exclui um endereço |

A API usa a **mesma sessão e as mesmas permissões** da interface — um usuário
comum só enxerga e altera os próprios endereços, mesmo chamando a API
diretamente com o id de outra pessoa.

Dois cuidados na escrita: o **dono do endereço nunca vem da requisição** (é sempre
deduzido de quem está autenticado, para não ser possível gravar na conta de
outro), e a atualização **não transfere titularidade**.

A documentação é gerada dos comentários do código e pode ser testada pelo
Swagger. Há também uma página de referência dentro da aplicação, em
**Administração → Documentação da API**.

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

O projeto é acompanhado por [issues organizadas em fases](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/milestones),
seguindo a ordem em que cada etapa faz sentido ser feita.

**Fase 4 — Evolução da arquitetura**
- Front-end em **Next.js (React)**, separando interface e API, com base para mobile
- **Auditoria** — registrar quem alterou o quê, quando, e os valores antes e depois
- **Registro de decisões de arquitetura (ADR)**

**Fase 5 — Escala e operação**
- **Kubernetes** — réplicas, escala automática e atualização sem indisponibilidade
- **Balanceamento de carga e gateway de entrada** (NGINX/Ingress ou YARP), com WAF
- **Migrações de banco sem indisponibilidade** — pré-requisito para escalar
- **Logs centralizados** (Seq ou Grafana Loki) — hoje eles somem com o container
- **Observabilidade** — métricas e alertas com Prometheus e Grafana
- **Cache com Redis** — reduzir consultas repetidas ao ViaCEP
- **Processamento em segundo plano** — exportações grandes via fila
- **Testes end-to-end** — jornada completa do usuário no navegador

**Fase 6 — Conformidade e proteção de dados**
- **LGPD** — direitos do titular (acessar, exportar, corrigir, excluir e
  anonimizar), política de privacidade, retenção e criptografia dos dados no banco.
  *A base legal aqui é execução de contrato, não consentimento — o consentimento
  só passa a ser exigido se entrarem marketing, rastreamento ou compartilhamento
  com terceiros ([issue #28](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues/28)).*
- **Backup automatizado** e plano de recuperação, com teste de restauração
- **Cofre de segredos** (Vault ou Key Vault), com rotação automática
- **Segurança contínua no pipeline** — análise do código (SAST), varredura das
  imagens Docker e atualização automática de dependências

**Fase 7 — Resiliência e performance**
- **Tolerância a falhas** — novas tentativas, disjuntor e tempos limite (Polly)
- **Rastreamento distribuído** com OpenTelemetry, seguindo a requisição ponta a ponta
- **Escalabilidade de dados** — paginação, índices e réplicas de leitura
- **Testes de carga** com k6, para medir a capacidade real
- **Implantação gradual** (canary) e feature flags
- **Alta disponibilidade do banco** — replicação com troca automática em caso de falha
- **Acessibilidade (WCAG)** e suporte a múltiplos idiomas

---

## Documentação técnica

- [Arquitetura](backend/docs/ARCHITECTURE.md)
- [Documentação geral](backend/docs/DOCUMENTACAO.md)
- [Configuração do Keycloak](backend/docs/SETUP_KEYCLOAK.md)
- [Manual de deploy](backend/docs/DEPLOY.md)
- [Scripts do banco](backend/scripts/tabelas.sql)

---

Desenvolvido por **Leonardo Vieira Guimarães**
