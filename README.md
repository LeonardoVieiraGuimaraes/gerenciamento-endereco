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
>
> O escopo é pequeno de propósito, mas as decisões foram tomadas como num sistema
> que vai crescer: **segurança em primeiro lugar** e estrutura que aceita evoluir
> — front-end desacoplado, múltiplas réplicas, autorização mais granular — sem
> reescrever o que já existe.

---

## O que o sistema faz

- **Login único (SSO)** com verificação em duas etapas (2FA)
- **CRUD de endereços**, com busca automática por CEP e filtros
- **Exportação para CSV**
- **Área administrativa** para gerenciar usuários
- **API REST completa**, com documentação interativa
- **Tema claro e escuro**, inclusive nas telas de login

---

## Arquitetura e decisões

O escopo é pequeno, mas foi construído como um sistema que **precisa crescer**:
com ênfase em segurança e com as portas abertas para evoluir sem reescrita.

Cada escolha abaixo tem o motivo resumido. O raciocínio completo — alternativas
descartadas e o gatilho que indica quando revisar — está nos
[ADRs](backend/docs/adr/) e nas
[issues](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues).

| Escolha | Por quê |
|---|---|
| **Identidade delegada ao Keycloak** (OpenID Connect) | Autenticação é problema resolvido. Delegando, senha, 2FA e bloqueio por tentativa vêm prontos e auditados, em vez de virarem responsabilidade da aplicação |
| **Autorização em camadas** — papel no Keycloak, dono do registro no banco | São perguntas de granularidade diferente. Escala acrescentando um autorizador dedicado (OpenFGA, Cerbos), sem trocar o que já existe |
| **Permissões por ação**, não por "está logado" | Permite liberar leitura sem liberar exclusão, sem reescrever o controle de acesso |
| **Tabela local espelhando o Keycloak** | Chave estrangeira não atravessa bancos, e filtrar por nome precisa do nome no SQL |
| **Exclusão permanente**, não lógica | Nada referencia um endereço além do dono, e manter dado pessoal escondido conflita com a LGPD |
| **Docker Compose agora**, Kubernetes no roadmap | Kubernetes resolve problemas de escala que este sistema ainda não tem. As chaves de sessão já são persistidas no banco, então migrar não derrubaria sessão |
| **MVC hoje, API completa e independente** | Entrega valor agora sem fechar a porta: o front-end em Next.js previsto consome a API já pronta e testada |
| **Versões fixadas**, nunca `latest` | Aprendizado do próprio projeto: uma atualização automática do Keycloak quebrou o tema silenciosamente |

**O que "preparado para crescer" significa na prática, hoje:**

- Autenticação e autorização já separadas, prontas para um autorizador dedicado
- API independente das telas, pronta para um front-end desacoplado
- Sessão fora da memória do processo, pronta para múltiplas réplicas
- Ambiente idêntico em desenvolvimento e produção, pronto para orquestração
- Banco versionado por migrations, com histórico rastreável

---

## Arquitetura

```
Navegador
    │
    ├──► Aplicação web (.NET 8)  ──► Banco da aplicação (SQL Server)
    │            │
    │            └──► API ViaCEP (consulta de CEP)
    │
    └──► Keycloak (login)        ──► Banco do Keycloak (PostgreSQL)
```

| Responsabilidade | Onde fica |
|---|---|
| Identidade (senha, 2FA, perfis, sessões) | Keycloak |
| Regras de negócio e dados de endereço | Aplicação .NET |
| Consulta de CEP | API pública do ViaCEP |

Quatro containers, subindo com um comando. O mesmo arranjo roda em
desenvolvimento e em produção, o que elimina a classe de erro "funciona na minha
máquina".

---

## Engenharia e processo

**Código**
- Separação de responsabilidades entre controllers, serviços e acesso a dados
- Injeção de dependência com interfaces, permitindo testar cada parte isolada
- Contratos de entrada próprios (DTO) na API, para o cliente não gravar campos
  que não deveria controlar
- Banco versionado por migrations, junto do código

**Processo**
- Um commit por funcionalidade, com a mensagem explicando o motivo da mudança
- Issues organizadas em fases, registrando o que foi feito e o que falta
- ADRs documentando decisões e quando revisá-las
- Testes automatizados barrando a publicação em caso de falha

**Qualidade**
- 47 testes automatizados (xUnit, Moq, FluentAssertions)
- Revisão de segurança aplicada antes de expor a aplicação na internet
- Verificação de dependências vulneráveis a cada publicação

---

## Tecnologias

| Camada | Stack |
|---|---|
| **Aplicação** | .NET 8 (ASP.NET Core MVC), Entity Framework Core, Bootstrap 5 |
| **Dados** | SQL Server 2022 (aplicação), PostgreSQL 16 (Keycloak) |
| **Identidade** | Keycloak 26, OpenID Connect / OAuth 2.0 |
| **Infraestrutura** | Docker, Docker Compose, GitHub Actions, Cloudflare Tunnel |
| **Apoio** | Serilog (logs), CsvHelper (CSV), Swagger (documentação) |
| **Testes** | xUnit, Moq, FluentAssertions |

---

## Segurança aplicada

| Item | O que foi feito |
|---|---|
| Senhas | Não passam pela aplicação — responsabilidade do Keycloak |
| Autenticação | OpenID Connect, com validação de assinatura e destinatário do token |
| Autorização | Verificada no servidor em toda ação, não apenas na interface |
| Formulários | Proteção contra falsificação de requisição (CSRF) |
| Limite de requisições | Bloqueio de excesso de tentativas, mais rígido no login |
| Cabeçalhos | Política de conteúdo (CSP), bloqueio de uso em iframe, entre outros |
| Cookies | Marcados como seguros e inacessíveis a scripts |
| HTTPS | Obrigatório em produção |
| Dependências | Sem pacotes com vulnerabilidade conhecida, checado a cada publicação |
| Segredos | Fora do código — guardados como segredos do GitHub |
| Banco de dados | Não exposto para fora do servidor |
| Logs | Estruturados (Serilog), sem gravar dado pessoal em produção |
| Sessão | Chaves persistidas no banco — reiniciar ou escalar não desloga ninguém |

---

## Estrutura

```
gerenciamento-endereco/
├── docker-compose.yml       Ambiente de desenvolvimento
├── docker-compose.prod.yml  Ambiente de produção
│
├── backend/            Aplicação .NET — telas, API, regras e acesso a dados
│   ├── Controllers/    Entrada das requisições
│   ├── Services/       Regras e integrações isoladas (ViaCEP, CSV, Keycloak)
│   ├── Models/         Domínio e contratos de entrada
│   ├── Views/          Telas
│   ├── Data/           Contexto e configuração do banco
│   ├── Migrations/     Histórico versionado do schema
│   └── docs/           Documentação técnica, ADRs e manual de deploy
│
├── auth-keycloak/      Servidor de identidade — realm e tema versionados
├── GerenciamentoEndereco.Tests/   Testes automatizados
├── frontend/           Reservado para a interface em Next.js
└── .github/workflows/  Testes e publicação automática
```

A configuração de autenticação fica separada do código da aplicação, mas o
ambiente sobe com um comando só. Os arquivos de Compose ficam na raiz porque
orquestram o conjunto inteiro — aplicação, Keycloak e os dois bancos — e não
apenas o backend: cada serviço aponta para a pasta que o constrói.

---

## Como rodar

Só é preciso ter o [Docker](https://www.docker.com/products/docker-desktop/) instalado.

```bash
docker compose up -d --build
```

Na primeira vez o Keycloak leva cerca de um minuto. As tabelas são criadas
automaticamente.

| Serviço | Endereço |
|---|---|
| Aplicação | http://localhost:5000 |
| Documentação da API (Swagger) | http://localhost:5000/swagger |
| Verificação de saúde | http://localhost:5000/health |
| Keycloak | http://localhost:8089 |

### Dados de demonstração

O ambiente sobe com **5 usuários e 14 endereços** já cadastrados, para poder
navegar sem precisar criar dados na mão:

| Usuário | Senha | Perfil | Endereços |
|---|---|---|---|
| `admin` | `Admin@123` | ADMIN | 0 |
| `maria` | `Teste@123` | USUARIO | 3 |
| `joao` | `Teste@123` | USUARIO | 3 |
| `ana` | `Teste@123` | USUARIO | 4 |
| `carlos` | `Teste@123` | USUARIO | 2 |
| `leonardo` | `Teste@123` | USUARIO | 2 |

> Credenciais de demonstração, para avaliação do projeto. Num ambiente real
> essas contas não existiriam e a carga de dados estaria desligada.

Entrar com um usuário comum mostra apenas os endereços dele; entrar como ADMIN
mostra todos e libera a área de gerenciamento de usuários.

A carga só acontece **em banco vazio** e é controlada pela configuração
`Seed:Enabled` — ligada em desenvolvimento e homologação, desligada por padrão,
para nunca inserir dado fictício junto de dado real.

### API REST

| Método | Rota |
|---|---|
| `GET` | `/api/enderecos` |
| `GET` | `/api/enderecos/{id}` |
| `POST` | `/api/enderecos` |
| `PUT` | `/api/enderecos/{id}` |
| `DELETE` | `/api/enderecos/{id}` |

A API compartilha sessão e permissões com a interface: um usuário comum só
enxerga e altera os próprios endereços, mesmo chamando diretamente com o id de
outra pessoa. Detalhes e exemplos no Swagger.

---

## Publicação automática

A cada envio para a branch `main`, o GitHub Actions roda os testes, verifica
dependências vulneráveis, publica no servidor e confirma que a aplicação
respondeu. Se qualquer etapa falhar, a publicação é interrompida e a versão
anterior segue no ar.

Passo a passo em [backend/docs/DEPLOY.md](backend/docs/DEPLOY.md).

---

## Próximos passos

Acompanhados por [issues organizadas em fases](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/milestones),
na ordem em que cada etapa faz sentido ser feita.

**Fase 4 — Evolução da arquitetura**
Front-end em Next.js · trilha de auditoria · demais ADRs

**Fase 5 — Escala e operação**
Kubernetes · balanceamento de carga e WAF · migrações sem indisponibilidade ·
logs centralizados · métricas e alertas · cache com Redis · processamento
assíncrono · testes end-to-end

**Fase 6 — Conformidade e proteção de dados**
LGPD (direitos do titular, retenção, criptografia) · backup com teste de
restauração · cofre de segredos · análise de código e imagens no pipeline

**Fase 7 — Resiliência e performance**
Tolerância a falhas (Polly) · rastreamento distribuído · paginação e réplicas de
leitura · testes de carga · implantação gradual · alta disponibilidade do banco ·
acessibilidade

---

## Documentação técnica

- [Decisões de arquitetura (ADR)](backend/docs/adr/) — o porquê de cada escolha
- [Arquitetura](backend/docs/ARCHITECTURE.md)
- [Documentação geral](backend/docs/DOCUMENTACAO.md)
- [Configuração do Keycloak](backend/docs/SETUP_KEYCLOAK.md)
- [Manual de deploy](backend/docs/DEPLOY.md)
- [Scripts do banco](backend/scripts/tabelas.sql)

---

## Sobre o desenvolvimento

Este projeto foi desenvolvido com apoio de ferramentas de IA, prática hoje comum
no mercado. Vale registrar como isso foi usado, porque muda o resultado:

- **As decisões de arquitetura foram tomadas e justificadas**, não aceitas por
  padrão. Cada uma está registrada em [ADR](backend/docs/adr/) com as
  alternativas descartadas e o gatilho para revisão.
- **Os problemas foram diagnosticados até a causa raiz.** Alguns exemplos, todos
  registrados nas [issues](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues?q=is%3Aissue):
  um logout que falhava por causa de um cabeçalho de segurança que bloqueava o
  redirecionamento; uma atualização automática do Keycloak que quebrou o tema em
  silêncio; um vínculo por nome de usuário que permitiria uma conta recriada
  herdar dados da anterior.
- **A qualidade é verificável:** 47 testes automatizados, nenhuma dependência com
  vulnerabilidade conhecida e publicação bloqueada se algo falhar.

O histórico de commits e as issues mostram o processo completo, incluindo os
erros cometidos e como foram corrigidos.

---

Desenvolvido por **Leonardo Vieira Guimarães**
