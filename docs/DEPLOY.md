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

### 1. Adicionar os Secrets no repositório GitHub

`ssh.leoproti.com.br` não está atrás de uma política de Cloudflare Access que
exija autenticação extra — é só um túnel transparente (confirmado: o mesmo
padrão já funciona no workflow do leo-portifolio, sem Service Token). Só a
chave SSH basta.

**Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valor |
|---|---|
| `DEPLOY_SSH_HOST` | `ssh.leoproti.com.br` |
| `DEPLOY_SSH_USER` | `root` |
| `DEPLOY_SSH_KEY` | Conteúdo do arquivo de chave privada (`id_ed25519`) usado no `leoproti-root` — cole o arquivo inteiro, incluindo as linhas `-----BEGIN...` e `-----END...` |
| `SQL_SA_PASSWORD` | `GUgq8bSz2ntWGkKnsFa4Fi7E!Aa1` |
| `KEYCLOAK_DB_PASSWORD` | `WXrum4APsNi9ialqDLMf1IUMMImS` |
| `KEYCLOAK_ADMIN_PASSWORD` | `264tGr1fRd6BKBDThUwQAa1!` |
| `KEYCLOAK_CLIENT_SECRET` | Secret do client `app-csharp` — **não fica no repositório**, ver abaixo |
| `KEYCLOAK_ADMIN_CLIENT_SECRET` | Secret do client `backend-admin-api` — idem |

As três senhas geradas acima (SQL/Keycloak DB/Keycloak admin) são novas e
aleatórias — específicas de produção, diferentes das usadas em desenvolvimento
local. Depois de cadastradas como secrets, não precisam ficar salvas em mais
lugar nenhum.

### 2. Cloudflare Tunnel — novos subdomínios (já feito)

Duas entradas já foram adicionadas em `~/.cloudflared/config.yml` no servidor
e o serviço reiniciado (confirmado: os outros ~20 serviços no mesmo túnel não
foram afetados):

```yaml
  - hostname: enderecos.leoproti.com.br
    service: http://localhost:8090

  - hostname: auth-enderecos.leoproti.com.br
    service: http://localhost:8089
```

Os registros DNS também já foram criados via `cloudflared tunnel route dns`.
Só documentado aqui para referência — não precisa repetir isso.

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
### Client secrets do Keycloak

O realm versionado guarda apenas **placeholders** (`__APP_CSHARP_SECRET__` e
`__BACKEND_ADMIN_SECRET__`). Os valores reais entram no build da imagem:

| Ambiente | De onde vem o valor |
|---|---|
| Desenvolvimento | Valores padrão declarados no `Dockerfile`, claramente marcados como de desenvolvimento — permitem subir o ambiente com um comando |
| Produção | Build args do `docker-compose.prod.yml`, alimentados pelos segredos do GitHub |

O build falha se algum placeholder não for substituído, evitando publicar uma
imagem com o valor literal.

**Para rotacionar em produção**, os dois lados precisam mudar juntos — entre um
passo e outro o login fica indisponível:

1. Gerar o novo valor
2. Atualizar o secret correspondente no GitHub
3. Aplicar o mesmo valor no client do Keycloak em produção:
   ```bash
   kcadm.sh update clients/<id> -r gerenciamento-endereco -s secret=<novo-valor>
   ```

O passo 3 é necessário porque a importação do realm é ignorada quando ele já
existe — mudar o arquivo sozinho não altera um ambiente já provisionado.
