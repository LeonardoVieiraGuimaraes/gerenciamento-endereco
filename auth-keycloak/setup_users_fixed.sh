#!/bin/bash
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password admin123

# Apagar usuários existentes (se houver) para evitar conflitos de email/username
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh delete users -r gerenciamento-endereco -q username=leonardo || true
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh delete users -r gerenciamento-endereco -q username=leonardoadmin || true
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh delete users -r gerenciamento-endereco -q email=leonardovieiraxy@gmail.com || true
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh delete users -r gerenciamento-endereco -q email=leonardovieiraxy@hotmail.com || true

# Criar leonardoadmin
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh create users -r gerenciamento-endereco -s username=leonardoadmin -s email=leonardovieiraxy@gmail.com -s enabled=true -s firstName=Leonardo -s lastName=Admin
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh set-password -r gerenciamento-endereco --username leonardoadmin --new-password 12345
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh add-roles -r gerenciamento-endereco --uusername leonardoadmin --rolename admin

# Criar leonardo
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh create users -r gerenciamento-endereco -s username=leonardo -s email=leonardovieiraxy@hotmail.com -s enabled=true -s firstName=Leonardo -s lastName=User
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh set-password -r gerenciamento-endereco --username leonardo --new-password 12345
docker exec keycloak-gerenciamento-endereco /opt/keycloak/bin/kcadm.sh add-roles -r gerenciamento-endereco --uusername leonardo --rolename user
