FROM quay.io/keycloak/keycloak:latest

# Modo de produção
ENV KC_METRICS_ENABLED=true
ENV KC_HEALTH_ENABLED=true
ENV KC_FEATURES=token-exchange

# Diretório de trabalho
WORKDIR /opt/keycloak

# Build do Keycloak
RUN /opt/keycloak/bin/kc.sh build

# Porta padrão
EXPOSE 8080

# Comando de inicialização
ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "start", "--optimized"]
