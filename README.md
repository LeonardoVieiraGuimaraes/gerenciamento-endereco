# 🏢 Plataforma Integrada de Gerenciamento de Endereços

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Orchestration-2496ED?style=flat&logo=docker&logoColor=white)](https://www.docker.com/)
[![Keycloak](https://img.shields.io/badge/Keycloak-IAM-blue?style=flat&logo=keycloak&logoColor=white)](https://www.keycloak.org/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat&logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/)

> **Acesso em Produção:** [https://enderecos.leoproti.com.br](https://enderecos.leoproti.com.br) *(Exemplo de URL de hospedagem final)*

Um sistema completo de gerenciamento de endereços construído com forte foco em **Engenharia de Software, Segurança e Arquitetura Escalável**. O projeto utiliza uma arquitetura baseada em containers (Docker), gerenciamento de identidade e acessos de ponta com o Keycloak (OIDC/OAuth 2.0) e as melhores práticas do ecossistema .NET 8.

---

## 🏗️ Arquitetura e Engenharia de Software

O sistema foi desenhado visando escalabilidade, manutenibilidade e alta segurança, adotando os seguintes padrões arquiteturais e metodologias de engenharia de software:

- **Arquitetura Baseada em Containers:** Todos os serviços (API, Banco de Dados Relacional, Identity Server e DB de Autenticação) são orquestrados de forma totalmente independente via Docker. Isso garante paridade perfeita entre os ambientes de desenvolvimento, homologação e produção.
- **Preparação para Microsserviços:** A infraestrutura atual já separa a camada de identidade (Keycloak IAM) da camada de negócios (Backend API), o que impede dependências diretas de banco de dados para credenciais, centralizando logins corporativos.
- **Injeção de Dependência (DI):** Amplo uso do contêiner de Inversão de Controle nativo do ASP.NET Core para acoplamento fraco e facilidade de testes automatizados (ex: `IKeycloakAdminService`, `IViaCepService`).
- **Resiliência e Rate Limiting:** Proteção de endpoints sensíveis e APIs externas (como a integração com a API pública do ViaCEP) através de políticas avançadas de *Rate Limiting* (`PartitionedRateLimiter`), prevenindo ativamente ataques de força bruta, scraping excessivo e abusos de banda.
- **Integração Externa Otimizada:** Consumo seguro e cacheado de serviços de terceiros utilizando o padrão `HttpClientFactory`.
- **UI & UX:** Interface projetada utilizando **Glassmorphism**, com CSS puro e moderno, garantindo transições leves e foco primário na experiência interativa do usuário.

---

## 🔐 Segurança e Gerenciamento de Identidade (IAM)

A segurança é o pilar central desta aplicação. A autenticação não foi construída "do zero" no banco da aplicação (prática legada), mas delegada integralmente a um servidor Identity Provider (IdP), garantindo conformidade com protocolos abertos.

- **Servidor Keycloak:** Atua como o Provedor de Identidade (IdP) exclusivo do sistema.
- **Protocolos Modernos Abertos:** Utilização nativa do **OpenID Connect (OIDC)** e **OAuth 2.0** com fluxo de código de autorização (*Authorization Code Flow*).
- **Gestão Híbrida de Perfis e Roles (RBAC Híbrido):**
  - O sistema aplica o conceito de *Role-Based Access Control (RBAC)* operando em duas camadas cruzadas: **Realm Roles** (nível global corporativo) e **Client Roles** (nível específico da aplicação atual).
  - Categorização principal divide usuários em `ADMIN` ou `USUARIO`. Estas *roles* são empacotadas dinamicamente dentro do *Access Token* JWT pelo Keycloak usando *Protocol Mappers*.
  - A API em .NET atua como *Resource Server* e *Client*. Ela intercepta e decodifica o JWT OIDC, convertendo as *roles* transportadas via JSON para a infraestrutura de *Claims* de identidade nativas do C# (`ClaimsIdentity`), integrando nativamente com as políticas granulares de autorização do ASP.NET Core (ex: `options.AddPolicy("EnderecoWrite", ...)`).
- **Tratamento Severo do OIDC:** O backend atua proativamente mantendo o estado dos tokens (`SaveTokens = true`), garantindo sincronia no processo de *Single Logout (SLO)*. Foram aplicados mecanismos contornando barreiras estritas de segurança de rede (`SameSite` cookies & `Secure=true`) que causam loops clássicos de deslogamento durante simulações e deploys (incluindo rewrites de hosts para comunicação *server-to-server* no docker vs *browser-to-server* via localhost).
- **Data Protection Compartilhado:** Chaves de criptografia antiforgery e tokens são persistidas no Entity Framework (`PersistKeysToDbContext`), o que significa que se os contêineres reiniciarem ou sofrerem scale-out em Kubernetes, as sessões ativas dos clientes não serão destruídas, mantendo continuidade absoluta.

---

## 🛠️ Tecnologias, Frameworks e Ferramentas

| Categoria | Tecnologia / Stack |
|-----------|--------------------|
| **Linguagem Base** | C# (C-Sharp) 12 |
| **Framework Web** | ASP.NET Core 8 (MVC + Web API) |
| **Banco de Dados (API)** | Microsoft SQL Server 2022 |
| **Banco de Dados (IdP)**| PostgreSQL 16 (Dedicado para os dados do Keycloak) |
| **Identity & Access** | Keycloak v25/latest (Configurado como IAM) |
| **Orquestração (Local)** | Docker & Docker Compose |
| **Mapeamento (ORM)** | Entity Framework Core 8 |
| **Estilização de UI** | CSS3 Vanilla Moderno (Glassmorphism), HTML5, Razor Views |
| **Integração de Dados** | CsvHelper (Relatórios e Importação/Exportação) |
| **Documentação API** | Swagger (Swashbuckle.AspNetCore v6) |
| **Log Estruturado** | Serilog (Formatado e integrado ao pipeline HTTP) |

---

## 📁 Organização e Estrutura de Pastas

A base de código foi cuidadosamente segmentada baseada nos domínios funcionais e serviços de infraestrutura para facilitar o desenvolvimento independente:

```text
📁 gerenciamento-endereco/
├── 📁 auth-keycloak/              # Infraestrutura isolada do Servidor de Identidade
│   ├── 📄 Dockerfile              # Imagem customizada baseada no Keycloak Oficial (Quarkus)
│   ├── 📁 realm/                  # Backup de configuração (Realm, Clients OIDC, Users, Mappers) injetados automaticamente no boot.
│   └── 📁 theme/                  # Templates FreeMarker que injetam a identidade visual "Glassmorphism" na página de SSO.
│
├── 📁 backend/                    # Core da Aplicação (Backend + Web API + MVC)
│   ├── 📄 docker-compose.yml      # O Maestro Central. Orquestra o SQLServer, Postgres, Keycloak e a aplicação de uma vez.
│   ├── 📄 Program.cs              # Núcleo configuracional de Injeção de Dependência, Filtros OIDC, Rate Limiting e Pipeline de Requests.
│   ├── 📁 Controllers/            # Controladores lidando com UI (MVC) e roteamento de Endpoints puros REST.
│   ├── 📁 Data/                   # Camada de Persistência (AppDbContext, Migrations).
│   ├── 📁 Models/                 # Entidades canônicas de domínio, Enums de acesso e ViewModels de transferência.
│   ├── 📁 Services/               # Core Lógico de negócios, HTTP Client externo (ViaCEP) e integração administrativa REST API do Keycloak.
│   ├── 📁 Views/                  # Camada de Visualização usando Razor e Componentização visual.
│   └── 📁 wwwroot/                # Assets da UI (Stylesheets CSS globais, Javascript customizado e vetores visuais).
│
├── 📁 frontend/                   # Repositório segregado (Preparatório para arquiteturas Headless/BFF).
├── 📁 GerenciamentoEndereco.Tests/# Suíte de testes unitários e de integração (Garantia de Qualidade).
└── 📄 README.md                   # Documentação mestre atual.
```

---

## 🔄 Issues, Linha do Tempo e Kanban (Roadmap)

Organização dos escopos e metas do projeto.

### ✅ Entregáveis (Concluído)
- [x] **Domínio de Banco:** Modelagem e criação da estrutura de banco de dados relacional via *Code-First*.
- [x] **Integrações de Terceiros:** Integração via C# e HttpClient com a API do ViaCEP, incluindo limitação de chamadas contra punições.
- [x] **Desenvolvimento de API:** Construção da RESTful API e autogeração de contratos OAI documentados pelo Swagger.
- [x] **Conteinerização / DevOps Local:** Dockerização 100% autossuficiente: Bancos relacionais (SQL e NoSQL-like auth properties), IAM OIDC, e Código.
- [x] **Identidade Federal (OIDC):** Substituição completa de autenticação antiga via banco próprio para o uso oficial do Keycloak, segregando perfeitamente a gestão de pessoas.
- [x] **UI/UX Global:** Implantação de Design System usando padrão Glassmorphism ponta a ponta (SSO Keycloak -> App Principal).
- [x] **Correções Arquiteturais OIDC:** Fixação severa na transição do contexto de sessão HTTP OIDC em dev local (manipulando `sslRequired` para `none` internamente para não gerar loops infinitos de redirect, e passagem mandatória do `id_token_hint` em pipelines de logouts manuais na API).
- [x] **Gestão de Roles Customizada:** Criação do ambiente dinâmico híbrido mapeando chaves de Claims (OIDC token payload -> .NET User Context) entre perfis `ADMIN` e `USUARIO`.

### 🚀 Future Scope (Próximos Passos & Backlog Architecture)
O caminho para uma aplicação de *Tier Global Enterprise*.

- [ ] **1. Orquestração Avançada (Kubernetes / K8s):**
  - Mover o mecanismo atual do `docker-compose` monolítico local para *Manifests* em YAML e *Helm Charts* robustos no cluster Kubernetes.
  - Adição imediata de *Liveness* e *Readiness probes* nas APIs. Configuração dinâmica via *ConfigMaps* e isolamento de banco via *StatefulSets*.
- [ ] **2. Desacoplamento Headless do Frontend (Next.js & React Native):**
  - Arrancar toda a camada de *Views/Razor* do repositório backend.
  - Implementar uma SPA (*Single Page Application*) isolada utilizando **React e Next.js** para renderização híbrida.
  - Desenvolver cliente Mobile 100% fluido usando **React Native**.
  - O Backend .NET assumirá finalmente um papel estrito de Web API (com padrão *BFF - Backend For Frontend*) respondendo JSON nativo para as novas interfaces e gerenciando JWT Tokens via PKCE diretamente aos apps.
- [ ] **3. Trilha de Auditoria Universal (Audit Trail):**
  - Criar um novo contexto de interceptação nos DbContexts usando a *Engine do Entity Framework* ou via CQRS Events, gravando um log imutável numa tabela otimizada (ou num banco NoSQL/ElasticSearch). Responder rigorosamente: *"Quem alterou esse endereço, quando, e qual era o dado antes e depois da ação?"*.
- [ ] **4. Mensageria Event-Driven (RabbitMQ / Apache Kafka):**
  - A ação de exportação de dados (CsvHelper) ou grandes relatórios podem gerar timeouts de requisições HTTP caso os registros atinjam milhões de linhas. Implementar Mensageria (Publish/Subscribe) para processamento em background (Workers).
- [ ] **5. CI/CD Enterprise:**
  - Plugar processos de integração contínua (GitHub Actions / GitLab CI). Submeter os PRs para checagem com o **SonarQube** para bloquear "Code Smells". Iniciar deploys baseados no estado do repositório usando **ArgoCD** apontando pro cluster.

---

## 🏃 Como Instalar e Executar (Um Clique)

Este projeto foi projetado para exigir o mínimo de atrito do desenvolvedor (DX). Você não precisa ter .NET, Visual Studio ou bancos de dados nativos rodando no Windows. Tudo o que você precisa é do **Docker Desktop** rodando.

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/SeuUsuario/gerenciamento-endereco.git
   ```
2. **Execute o Comando Mágico:** Abra o terminal na subpasta `backend/` e digite:
   ```bash
   docker-compose up -d --build
   ```
3. **Aguarde 15~25s.** A primeira subida do Keycloak demora alguns segundos para provisionar totalmente as chaves OIDC e o banco próprio. 
4. **Pronto! Sistemas Online:**
   - App em .NET: [http://localhost:5000](http://localhost:5000)
   - Painel IAM Keycloak (Admin): [http://localhost:8089](http://localhost:8089)
     - Logue com `admin` e senha `admin123`.

*(Nota: As migrations de tabelas pro SQL Server serão aplicadas automaticamente pelo EF Core no momento de boot do contêiner C#).*

> **Aviso de Recrutamento:** Este escopo abrange o domínio "End-to-End" completo: Levantamento Arquitetural, Containerização Otimizada, Identidade Distribuída (OAuth/OIDC), UX Design em Glassmorphism, e solução de segurança nativa do framework (Rate-limits & Proteção OIDC Data).
