FROM ghcr.io/goauthentik/server:2024.12.3

# Copia os Blueprints (Realms) customizados para o diretório de autocarregamento do Authentik
COPY blueprints/ /blueprints/custom/

# Copia os arquivos de Tema customizados
COPY theme/custom.css /web/dist/custom.css
