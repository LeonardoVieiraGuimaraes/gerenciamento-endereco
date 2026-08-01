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

- **Login único (SSO)** com verificação em duas etapas (2FA)
- **CRUD de endereços**, com busca automática por CEP e filtros
- **Exportação para CSV**
- **Área administrativa** para gerenciar usuários
- **API REST completa**, com documentação interativa
- **Tema claro e escuro**, inclusive nas telas de login

---

## Decisões de arquitetura

O que define um projeto não é a lista de tecnologias, e sim o motivo de cada
escolha. As decisões abaixo estão registradas em
[ADRs](backend/docs/adr/), com o gatilho que indica quando devem ser revistas.

### Identidade delegada, não construída

**Decisão:** a autenticação é feita por um servidor de identidade (Keycloak) via
OpenID Connect. A aplicação não guarda nem valida senha.

**Por quê:** autenticação é um problema resolvido, e resolvê-lo de novo significa
assumir a responsabilidade por hash de senha, recuperação, bloqueio por tentativa
e 2FA. Delegando, esses recursos vêm prontos e auditados.

**Alternativa descartada:** tabela de usuários com senha na própria aplicação —
o caminho mais rápido, e o que concentra mais risco.

### Autorização em duas camadas

**Decisão:** o **papel** vem do Keycloak (ADMIN/USUARIO); a checagem de **dono do
registro** acontece no banco, junto da consulta.

**Por quê:** são perguntas diferentes. "Esta pessoa pode excluir endereços?" o
servidor de identidade responde. "Este endereço é dela?" só o banco responde — o
Keycloak não sabe que endereços existem.

**Alternativa descartada:** o Keycloak Authorization Services faria as duas, mas
exigiria registrar cada endereço como recurso dentro dele. Para uma regra que
cabe numa cláusula `WHERE`, o custo em acoplamento e latência não se paga.
Externalizar só passa a valer com regras que a consulta não expressa —
compartilhamento entre usuários, hierarquia de equipe — e nesse caso o mercado
usa ferramentas dedicadas (OpenFGA, Cerbos), não o próprio IdP.

### Permissões por ação, não por "está logado"

**Decisão:** existem políticas separadas para ler, escrever, excluir, exportar,
ver a documentação e gerenciar usuários.

**Por quê:** permite liberar leitura a um perfil sem liberar exclusão, sem
reescrever o controle de acesso. Toda verificação é feita no servidor — esconder
o botão na interface não protege nada.

### Tabela local de usuários espelhando o Keycloak

**Decisão:** manter uma tabela `Usuarios` local, sem credenciais, ligada à
identidade pelo identificador imutável da conta.
([ADR 0002](backend/docs/adr/0002-tabela-local-de-usuarios.md))

**Por quê:** chave estrangeira não atravessa bancos diferentes, e filtrar
endereços por nome precisa do nome no SQL.

**Alternativa descartada:** guardar só o id do Keycloak no endereço. Cada
listagem viraria uma chamada de API por registro, e o filtro por nome deixaria de
funcionar em consulta.

### Exclusão permanente, não lógica

**Decisão:** excluir um endereço remove a linha do banco.
([ADR 0001](backend/docs/adr/0001-exclusao-de-enderecos.md))

**Por quê:** nada referencia um endereço além do dono, então apagar não corrompe
histórico. E endereço é dado pessoal — exclusão lógica manteria esse dado no
banco, o que conflita com o direito de eliminação da LGPD.

**Quando muda:** se surgir pedido ou entrega apontando para endereço. Mesmo aí, a
solução seria copiar os campos no momento do uso (*snapshot*), não exclusão lógica.

### Docker Compose agora, Kubernetes depois

**Decisão:** orquestração com Docker Compose, com Kubernetes mapeado no roadmap.

**Por quê:** Kubernetes resolve escala, alta disponibilidade e atualização sem
indisponibilidade — problemas que este sistema ainda não tem. Adotar antes
adicionaria complexidade sem benefício.

**O que já está preparado:** as chaves de sessão são persistidas no banco, então
múltiplas réplicas não derrubariam a sessão de ninguém.

### MVC hoje, API pronta para desacoplar

**Decisão:** as telas são renderizadas pelo .NET, mas a API REST já existe
completa e independente.

**Por quê:** entrega valor agora sem fechar a porta. A interface em Next.js
prevista no roadmap consome a API que já está pronta e testada, sem reescrever
regra de negócio nem controle de acesso.

### Versões fixadas em vez de `latest`

**Decisão:** todas as imagens Docker usam versão fixa.

**Por quê:** aprendizado do próprio projeto — uma atualização automática do
Keycloak reorganizou os temas internos e quebrou o tema customizado
silenciosamente, sem erro visível até alguém abrir a tela de login.

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
├── backend/            Aplicação .NET — telas, API, regras e acesso a dados
│   ├── Controllers/    Entrada das requisições
│   ├── Services/       Regras e integrações isoladas (ViaCEP, CSV, Keycloak)
│   ├── Models/         Domínio e contratos de entrada
│   ├── Views/          Telas
│   ├── Data/           Contexto e configuração do banco
│   ├── Migrations/     Histórico versionado do schema
│   ├── docs/           Documentação técnica, ADRs e manual de deploy
│   └── docker-compose*.yml   Ambientes de desenvolvimento e produção
│
├── auth-keycloak/      Servidor de identidade — realm e tema versionados
├── GerenciamentoEndereco.Tests/   Testes automatizados
├── frontend/           Reservado para a interface em Next.js
└── .github/workflows/  Testes e publicação automática
```

A configuração de autenticação fica separada do código da aplicação, mas o
ambiente sobe com um comando só — o `docker-compose.yml` do `backend/` constrói
inclusive a imagem do Keycloak, a partir de `auth-keycloak/`.

---

## Como rodar

Só é preciso ter o [Docker](https://www.docker.com/products/docker-desktop/) instalado.

```bash
cd backend
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

Desenvolvido por **Leonardo Vieira Guimarães**
