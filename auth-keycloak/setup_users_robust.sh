#!/bin/bash
export KC="/opt/keycloak/bin/kcadm.sh"

docker exec keycloak-gerenciamento-endereco $KC config credentials --server http://localhost:8080 --realm master --user admin --password admin123

# Deletar leonardo
ID1=$(docker exec keycloak-gerenciamento-endereco $KC get users -r gerenciamento-endereco -q username=leonardo | grep '"id"' | head -n 1 | awk -F '"' '{print $4}')
if [ ! -z "$ID1" ]; then
    docker exec keycloak-gerenciamento-endereco $KC delete users/$ID1 -r gerenciamento-endereco
fi

# Deletar leonardoadmin
ID2=$(docker exec keycloak-gerenciamento-endereco $KC get users -r gerenciamento-endereco -q username=leonardoadmin | grep '"id"' | head -n 1 | awk -F '"' '{print $4}')
if [ ! -z "$ID2" ]; then
    docker exec keycloak-gerenciamento-endereco $KC delete users/$ID2 -r gerenciamento-endereco
fi

# Criar leonardoadmin
docker exec keycloak-gerenciamento-endereco $KC create users -r gerenciamento-endereco -s username=leonardoadmin -s email=leonardovieiraxy@gmail.com -s enabled=true -s firstName=Leonardo -s lastName=Admin
docker exec keycloak-gerenciamento-endereco $KC set-password -r gerenciamento-endereco --username leonardoadmin --new-password 12345
docker exec keycloak-gerenciamento-endereco $KC add-roles -r gerenciamento-endereco --uusername leonardoadmin --rolename admin

# Criar leonardo
docker exec keycloak-gerenciamento-endereco $KC create users -r gerenciamento-endereco -s username=leonardo -s email=leonardovieiraxy@hotmail.com -s enabled=true -s firstName=Leonardo -s lastName=User
docker exec keycloak-gerenciamento-endereco $KC set-password -r gerenciamento-endereco --username leonardo --new-password 12345
docker exec keycloak-gerenciamento-endereco $KC add-roles -r gerenciamento-endereco --uusername leonardo --rolename user
