# Configuração do Keycloak

Este guia documenta como o Keycloak foi configurado para autenticar a aplicação C#.

## 1. Subindo o Container

A imagem do Keycloak (tema e realm) é construída a partir de `auth-keycloak/`,
mas quem a sobe é o Compose da **raiz do repositório**, junto com o restante do
ambiente:

```bash
docker compose up -d --build
```

Para subir só o Keycloak e o banco dele:

```bash
docker compose up -d keycloak
```

Acesse o painel do administrador em: `http://localhost:8089/admin` (admin / admin123).

## 2. Configuração do Realm e Client

Foi criado um Realm chamado **sistema-enderecos**.
Dentro dele, criamos um Client chamado **app-csharp** com as seguintes propriedades:
- **Client authentication**: `ON` (Gera o Client Secret)
- **Valid redirect URIs**: 
  - `http://localhost:5000/signin-oidc`
  - `https://localhost:5001/signin-oidc`

## 3. Autenticação de Dois Fatores (2FA)

Para ativar o 2FA para os usuários (Ex: Google Authenticator):
1. No painel de Admin do Keycloak, vá em **Authentication**.
2. Na aba **Flows**, selecione o fluxo **Browser**.
3. Na linha do **Browser - Conditional OTP**, mude o status para `REQUIRED`.

Dessa forma, todo usuário que tentar fazer login será obrigado a escanear o QRCode na primeira vez e digitar o token nas próximas.

## 4. Exportando Configurações

Sempre que fizer alterações no painel e quiser salvar no projeto para versionamento:
```bash
cd auth
./start-realm-export.sh
```
O JSON será exportado para a pasta `auth/realm/`.
