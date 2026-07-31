#!/bin/bash
set -e

KCADM="/opt/keycloak/bin/kcadm.sh"
SERVER="http://localhost:8080"
REALM="gerenciamento-endereco"

echo "=== Autenticando no Keycloak ==="
$KCADM config credentials --server $SERVER --realm master --user admin --password admin123

echo ""
echo "=== Roles atuais no realm ==="
$KCADM get roles --realm $REALM

echo ""
echo "=== Criando role 'admin' no realm (se não existir) ==="
if $KCADM get roles/$REALM/admin --realm $REALM 2>/dev/null | grep -q '"admin"'; then
  echo "Role 'admin' já existe."
else
  $KCADM create roles --realm $REALM -s name=admin -s description="Administrador do sistema" && echo "Role 'admin' criada!"
fi

echo ""
echo "=== Verificando usuários ==="
$KCADM get users --realm $REALM

echo ""
echo "=== Atribuindo role 'admin' ao leonardoadmin ==="
ADMIN_ID=$($KCADM get users --realm $REALM --query username=leonardoadmin 2>/dev/null | grep '"id"' | head -1 | awk -F'"' '{print $4}')
echo "ID do leonardoadmin: $ADMIN_ID"
if [ -n "$ADMIN_ID" ]; then
  $KCADM add-roles --realm $REALM --uusername leonardoadmin --rolename admin && echo "Role 'admin' atribuída ao leonardoadmin!"
else
  echo "AVISO: Usuário leonardoadmin não encontrado!"
fi

echo ""
echo "=== Atribuindo role 'user' ao leonardo ==="
USER_ID=$($KCADM get users --realm $REALM --query username=leonardo 2>/dev/null | grep '"id"' | head -1 | awk -F'"' '{print $4}')
echo "ID do leonardo: $USER_ID"
if [ -n "$USER_ID" ]; then
  $KCADM add-roles --realm $REALM --uusername leonardo --rolename user && echo "Role 'user' atribuída ao leonardo!"
else
  echo "AVISO: Usuário leonardo não encontrado!"
fi

echo ""
echo "=== Roles do leonardoadmin ==="
$KCADM get-roles --realm $REALM --uusername leonardoadmin

echo ""
echo "=== Roles do leonardo ==="
$KCADM get-roles --realm $REALM --uusername leonardo

echo ""
echo "=== CONCLUIDO! ==="
