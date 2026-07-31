FROM quay.io/keycloak/keycloak:latest

WORKDIR /opt/keycloak

# Copia o tema customizado
COPY theme/gerenciamento-endereco /opt/keycloak/themes/gerenciamento-endereco

# Copia apenas o realm consolidado (settings + tema + clients + roles + users)
# para importação automática. Os demais arquivos em realm/ (users-0, master-*)
# não são usados: criariam usuários duplicados no mesmo realm ou mexeriam no
# realm master, então ficam só como histórico/rascunho.
COPY realm/gerenciamento-endereco-realm.json /opt/keycloak/data/import/

# Habilita métricas e health
ENV KC_METRICS_ENABLED=true
ENV KC_HEALTH_ENABLED=true
ENV KC_DB=postgres

# Build otimizado
RUN /opt/keycloak/bin/kc.sh build

EXPOSE 8080

ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "start-dev", "--import-realm"]
