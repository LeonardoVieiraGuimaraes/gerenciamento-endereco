# Servidor de Identidade (Keycloak)

Imagem do **Keycloak 26** com o tema da aplicação e o realm
`gerenciamento-endereco` já configurado — clients, papéis e usuários — importado
automaticamente no primeiro boot.

Esta pasta guarda só a **configuração**. Quem sobe o container é o
`docker-compose.yml` da raiz do repositório, que constrói esta imagem junto com
a aplicação e os dois bancos.

## Requisitos
- **Docker** e **Docker Compose**

## Como Rodar

1. Na **raiz do repositório**, execute:
   ```bash
   docker compose up -d --build
   ```
   Para subir apenas o Keycloak e o banco dele:
   ```bash
   docker compose up -d keycloak
   ```

2. Acesse o Console de Administração:
   ```
   http://localhost:8089/admin
   ```
   Usuário `admin`, senha `admin123` (definidos no `docker-compose.yml`, apenas
   para desenvolvimento).

3. O realm da aplicação fica em:
   ```
   http://localhost:8089/realms/gerenciamento-endereco
   ```

## O que tem aqui

| Caminho | Para que serve |
|---|---|
| `Dockerfile` | Monta a imagem: copia o tema, importa o realm e injeta os client secrets |
| `realm/gerenciamento-endereco-realm.json` | O realm inteiro versionado — clients, papéis, usuários e ajustes de segurança |
| `theme/gerenciamento-endereco/` | Tema próprio das telas de login, conta, e-mail e do Console de Administração |
| `*.sh`, `*.ps1` | Scripts auxiliares usados durante a configuração inicial; mantidos como histórico |

O tema cobre quatro áreas — `login`, `account`, `admin` e `email` —, todas com
modo claro/escuro e textos em pt-BR.

## Detalhes que valem saber

**Os client secrets não estão no repositório.** O arquivo de realm traz apenas
os marcadores `__APP_CSHARP_SECRET__` e `__BACKEND_ADMIN_SECRET__`, substituídos
durante o build. Em desenvolvimento entram valores padrão, para o ambiente subir
com um comando só; em produção, o `docker-compose.prod.yml` passa os valores
reais via *build args*, vindos dos segredos do GitHub. O build falha de
propósito se algum marcador sobrar.

**A versão do Keycloak é fixa, nunca `latest`.** Uma tag flutuante já quebrou o
tema em silêncio: uma atualização reorganizou os temas embutidos e as telas
passaram a renderizar sem estilo nenhum, sem nenhum aviso.

**O realm declara só os papéis da aplicação** (`ADMIN` e `USUARIO`). Toda a
restrição mais fina — quem enxerga qual endereço — fica na aplicação, junto dos
dados. O porquê está na
[ADR de estratégia de autorização](../docs/adr/0003-estrategia-de-autorizacao.md).

**Os fluxos de autenticação são versionados por inteiro.** O realm declara os 21
fluxos completos, não apenas o trecho alterado — declarar uma lista parcial faz o
Keycloak **substituir** a lista interna em vez de complementá-la, e foi assim que
os client scopes sumiram uma vez, deixando o Console de Administração exibindo
"Anônimo". O que muda em relação ao padrão: no subfluxo *Browser - Conditional
2FA*, os métodos **Códigos de recuperação** e **WebAuthn** saem de `DISABLED`
para `ALTERNATIVE`, ao lado do OTP, para que ninguém fique trancado para fora ao
perder o autenticador.

**A importação é ignorada se o realm já existir.** Alterações neste JSON só valem
para ambientes novos; num Keycloak que já rodou, é preciso aplicar a mudança pelo
Console ou via `kcadm`. Limitação registrada na
[issue #34](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues/34).

## Documentações Adicionais
- [Como Configurar o Keycloak](../docs/SETUP_KEYCLOAK.md)
- [Estratégia de autorização (ADR)](../docs/adr/0003-estrategia-de-autorizacao.md)
- [Arquitetura do Projeto](../docs/ARCHITECTURE.md)
- [Aplicação .NET](../backend/README.md)
