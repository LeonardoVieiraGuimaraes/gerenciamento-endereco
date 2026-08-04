#!/bin/bash
#
# Aplica a configuração de e-mail a um realm que JÁ EXISTE.
#
# Por que este script é necessário: o Keycloak ignora o arquivo de importação
# quando o realm já foi criado (issue #34). Então, num ambiente que já rodou, não
# basta preencher os segredos e publicar — a configuração precisa ser aplicada
# pela API de administração, que é o que este script faz.
#
# Em ambiente novo nada disso é preciso: o realm nasce já configurado.
#
# Uso, no servidor, a partir da pasta do projeto (onde está o .env):
#
#     bash auth-keycloak/aplicar-smtp.sh
#
set -euo pipefail

CONTAINER="${KEYCLOAK_CONTAINER:-keycloak-gerenciamento-endereco}"
REALM="${KEYCLOAK_REALM:-gerenciamento-endereco}"
ENV_FILE="${ENV_FILE:-.env}"

if [ ! -f "$ENV_FILE" ]; then
  echo "erro: $ENV_FILE não encontrado — rode a partir da pasta do projeto." >&2
  exit 1
fi

set -a; . "./$ENV_FILE"; set +a

# Endereco e porta do provedor nao sao segredo e ficam versionados no
# docker-compose.prod.yml; aqui so entram como padrao, caso alguem rode o
# script fora do fluxo normal.
SMTP_HOST="${SMTP_HOST:-smtp-relay.brevo.com}"
SMTP_PORT="${SMTP_PORT:-587}"

for v in SMTP_FROM SMTP_USER SMTP_PASSWORD; do
  if [ -z "${!v:-}" ]; then
    echo "erro: $v está vazio em $ENV_FILE." >&2
    echo "      Cadastre SMTP_FROM, SMTP_USER e SMTP_PASSWORD nos segredos do" >&2
    echo "      repositório e publique antes de rodar." >&2
    exit 1
  fi
done

# A senha do administrador do realm master vem do próprio container, para não
# precisar digitá-la nem deixá-la no histórico do shell.
ADMIN_PASS=$(docker inspect "$CONTAINER" \
  --format '{{range .Config.Env}}{{println .}}{{end}}' \
  | grep '^KC_BOOTSTRAP_ADMIN_PASSWORD=' | cut -d= -f2-)

docker exec "$CONTAINER" /opt/keycloak/bin/kcadm.sh config credentials \
  --server http://localhost:8080 --realm master \
  --user admin --password "$ADMIN_PASS" > /dev/null

docker exec "$CONTAINER" /opt/keycloak/bin/kcadm.sh update "realms/$REALM" \
  -s "smtpServer.host=$SMTP_HOST" \
  -s "smtpServer.port=$SMTP_PORT" \
  -s "smtpServer.from=$SMTP_FROM" \
  -s "smtpServer.fromDisplayName=Gerenciamento de Endereços" \
  -s "smtpServer.auth=true" \
  -s "smtpServer.starttls=true" \
  -s "smtpServer.ssl=false" \
  -s "smtpServer.user=$SMTP_USER" \
  -s "smtpServer.password=$SMTP_PASSWORD" \
  -s "resetPasswordAllowed=true"

echo "configuração aplicada. Conferindo:"
docker exec "$CONTAINER" /opt/keycloak/bin/kcadm.sh get "realms/$REALM" \
  | python3 -c "
import sys, json
t = sys.stdin.read()
d = json.loads(t[t.find('{'):])
print('  resetPasswordAllowed:', d.get('resetPasswordAllowed'))
smtp = d.get('smtpServer') or {}
for k in ('host', 'port', 'from', 'auth', 'starttls', 'user'):
    print(f'  {k:9}:', smtp.get(k))
"
echo
echo "Teste enviando um e-mail pela tela de login: 'Esqueci minha senha'."
