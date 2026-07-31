# Deploy em produção (leoproti.com.br)

Deploy automático via GitHub Actions (`.github/workflows/deploy.yml`), disparado a
cada push na branch `main` (ou manualmente via "Run workflow").
O runner do GitHub se conecta ao servidor por SSH através do Cloudflare Tunnel
(`ssh.leoproti.com.br`), sincroniza o repositório e sobe os containers com
`docker-compose.prod.yml`.

- App: `https://enderecos.leoproti.com.br`
- Keycloak: `https://auth-enderecos.leoproti.com.br`
- Servidor: `leoproti-root` (200.198.43.52, acessado via túnel), diretório
  `/home/leonardovieiraxy/projetos/gerenciamento-endereco`

## Passos únicos (fazer antes do primeiro deploy)

### 1. Criar um Service Token no Cloudflare Access

O SSH pro servidor passa pelo Cloudflare Access (`ProxyCommand cloudflared access
ssh`) — sem uma identidade, o túnel nem aceita a conexão, independente da chave
SSH. Pra automação (CI) isso é feito com um Service Token, não login interativo:

1. **Cloudflare Zero Trust Dashboard** → **Access** → **Service Auth** → **Service
   Tokens** → **Create Service Token**.
2. Dê um nome (ex.: `github-actions-deploy`) e copie o **Client ID** e o
   **Client Secret** gerados (o secret só aparece uma vez).
3. Confirme que a política de Access que protege `ssh.leoproti.com.br` permite
   esse Service Token (Access → Applications → localize a app do SSH → Policies
   → inclua o token como um "Include" ou "Service Auth" rule).

### 2. Adicionar os Secrets no repositório GitHub

**Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valor |
|---|---|
| `DEPLOY_SSH_HOST` | `ssh.leoproti.com.br` |
| `DEPLOY_SSH_USER` | `root` |
| `DEPLOY_SSH_KEY` | Conteúdo do arquivo de chave privada (`id_ed25519`) usado no `leoproti-root` — cole o arquivo inteiro, incluindo as linhas `-----BEGIN...` e `-----END...` |
| `CF_ACCESS_CLIENT_ID` | Client ID do Service Token criado no passo 1 |
| `CF_ACCESS_CLIENT_SECRET` | Client Secret do Service Token criado no passo 1 |
| `SQL_SA_PASSWORD` | `GUgq8bSz2ntWGkKnsFa4Fi7E!Aa1` |
| `KEYCLOAK_DB_PASSWORD` | `WXrum4APsNi9ialqDLMf1IUMMImS` |
| `KEYCLOAK_ADMIN_PASSWORD` | `264tGr1fRd6BKBDThUwQAa1!` |
| `KEYCLOAK_CLIENT_SECRET` | `bcdbmbolTUjkbRc21tgKv7yuU8XZDk1E` (tem que bater com o `secret` do client `app-csharp` no realm JSON) |
| `KEYCLOAK_ADMIN_CLIENT_SECRET` | `K3eQM5uNBNJCRBKTxGRiBwqAPsErOrZh-GIwYpZ3PC4` (tem que bater com o `secret` do client `backend-admin-api`) |

As três senhas geradas acima (SQL/Keycloak DB/Keycloak admin) são novas e
aleatórias — específicas de produção, diferentes das usadas em desenvolvimento
local. Depois de cadastradas como secrets, não precisam ficar salvas em mais
lugar nenhum.

### 3. Cloudflare Tunnel — novos subdomínios

Duas entradas novas em `~/.cloudflared/config.yml` no servidor (feito uma vez,
manualmente, por afetar o túnel compartilhado com os outros ~20 serviços já
hospedados nele):

```yaml
  - hostname: enderecos.leoproti.com.br
    service: http://localhost:8090

  - hostname: auth-enderecos.leoproti.com.br
    service: http://localhost:8089
```

(antes da linha `- service: http_status:404`, que precisa continuar sendo a
última). Depois, criar os registros DNS e reiniciar o serviço:

```bash
cloudflared tunnel route dns ff3e2bca-b603-48e4-a3ae-9a8ff58a305f enderecos.leoproti.com.br
cloudflared tunnel route dns ff3e2bca-b603-48e4-a3ae-9a8ff58a305f auth-enderecos.leoproti.com.br
systemctl restart cloudflared
```

## Deploys seguintes

Só dar push na branch `main` (ou rodar o workflow manualmente).
O job `test` roda a suíte de testes e o `dotnet list package --vulnerable`
antes de qualquer coisa tocar o servidor — se falhar, o deploy não acontece.

## Observações

- `docker-compose.prod.yml` não expõe a porta do SQL Server (`1433`) pra fora
  do host — só os containers da rede interna `gerenc-net` falam com ele.
- O realm do Keycloak já é importado com o redirect URI de produção
  (`https://enderecos.leoproti.com.br/signin-oidc`) junto dos de
  desenvolvimento local — o mesmo `gerenciamento-endereco-realm.json` serve os
  dois ambientes.
- Rotação de secrets: os dois `client secret` do Keycloak (`app-csharp` e
  `backend-admin-api`) estão hardcoded no realm JSON committado no repositório
  — pra rotacioná-los de verdade também precisa gerar um novo valor no realm
  JSON e atualizar o secret do GitHub junto, nessa ordem, senão a autenticação
  quebra até os dois lados baterem de novo.
