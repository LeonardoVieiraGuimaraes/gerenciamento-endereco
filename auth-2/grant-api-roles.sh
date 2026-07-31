#!/bin/bash
set -e

KCADM="/opt/keycloak/bin/kcadm.sh"
SERVER="http://localhost:8080"
REALM="gerenciamento-endereco"

echo "=== Autenticando no Keycloak ==="
$KCADM config credentials --server $SERVER --realm master --user admin --password admin123

echo ""
echo "=== Atribuindo manage-users e view-users ao leonardoadmin ==="
# O cliente que gerencia o realm se chama realm-management
$KCADM add-roles --realm $REALM --uusername leonardoadmin --cclientid realm-management --rolename manage-users
$KCADM add-roles --realm $REALM --uusername leonardoadmin --cclientid realm-management --rolename view-users
$KCADM add-roles --realm $REALM --uusername leonardoadmin --cclientid realm-management --rolename query-users

echo "=== CONCLUIDO! ==="
