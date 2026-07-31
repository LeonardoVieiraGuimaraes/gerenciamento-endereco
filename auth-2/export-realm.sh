#!/bin/bash
# Script para exportar as configurações feitas no painel do Keycloak (Realm, Clients, Users)
# Isso vai salvar as alterações no arquivo json dentro do container, e se você tiver um volume mapeado, ele salvará fora.

echo "Exportando o Realm do Keycloak..."
docker exec -it keycloak-gerenciamento-endereco /opt/keycloak/bin/kc.sh export --dir /opt/keycloak/data/import

echo "Exportação concluída!"
echo "Para persistir no repositório, você precisa copiar o arquivo de dentro do container para a sua pasta local (se não estiver usando bind mounts)."
echo "Exemplo: docker cp keycloak-gerenciamento-endereco:/opt/keycloak/data/import/sistema-enderecos-realm.json ./realm/"
