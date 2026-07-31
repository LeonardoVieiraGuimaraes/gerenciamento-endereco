# Arquitetura do Sistema

Este projeto foi construído pensando na escalabilidade. Inicialmente foi planejado como um monolito completo em C#, mas já conta com decisões arquiteturais visando uma quebra para Microsserviços no futuro.

## Componentes

### 1. Backend C# (ASP.NET Core)
Responsável pelas regras de negócio. Atualmente, ele renderiza as Views em HTML (MVC), mas já possui um controlador de API RESTful (`EnderecosApiController`) documentado com Swagger para quando o Frontend for separado (ex: React, Angular, Vue).

### 2. SQL Server
Banco de dados relacional. Toda a estrutura de migração (Migrations) está configurada via Entity Framework Core. O banco sobe isoladamente via Docker.
- A tabela `Usuarios` existe no banco apenas para amarrar os registros. As senhas e regras de segurança não ficam nela.
- A tabela `Enderecos` guarda os endereços amarrados a um `UsuarioId`.

### 3. Keycloak (Autenticação e 2FA)
A decisão de usar o Keycloak como IdP (Identity Provider) central foi para suportar nativamente e com segurança recursos como:
- Autenticação de 2 Fatores (2FA) via Aplicativo Autenticador.
- Login Social.
- Single Sign-On (SSO) entre múltiplos sistemas.
O Backend C# delega o processo de login para o Keycloak via protocolo **OpenID Connect**.

## Integrações Externas
- **ViaCEP**: O preenchimento automático do endereço com base no CEP é feito diretamente no client-side via Javascript, consumindo a API pública do ViaCEP sem onerar o servidor C#. (Também existe um `ViaCepService` no C# para uso interno caso necessário).
