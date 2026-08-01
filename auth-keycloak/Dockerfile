# Tag de versão fixa (não "latest") — uma tag flutuante já quebrou o tema custom
# silenciosamente uma vez: novas versões do Keycloak reorganizam os temas embutidos
# (ex.: "common/keycloak.v2" virou só "keycloak.v2" numa atualização), e sem pin
# nenhum aviso aparece até alguém notar a tela sem estilo nenhum.
FROM quay.io/keycloak/keycloak:26.6.3

WORKDIR /opt/keycloak

# Copia o tema customizado
COPY theme/gerenciamento-endereco /opt/keycloak/themes/gerenciamento-endereco

# Copia apenas o realm consolidado (settings + tema + clients + roles + users)
# para importação automática. Os demais arquivos em realm/ (users-0, master-*)
# não são usados: criariam usuários duplicados no mesmo realm ou mexeriam no
# realm master, então ficam só como histórico/rascunho.
# --chown é necessário: a imagem roda como o usuário 'keycloak' (uid 1000), e o
# COPY cria o arquivo como root — sem isso o sed abaixo não consegue reescrevê-lo.
COPY --chown=keycloak:root realm/gerenciamento-endereco-realm.json /opt/keycloak/data/import/

# Os client secrets NÃO ficam no arquivo versionado: ele traz apenas
# placeholders, substituídos aqui no build.
#
# Em desenvolvimento os valores padrão abaixo são usados, para o ambiente subir
# com um comando só. Em produção, o docker-compose.prod.yml passa os valores
# reais via build args, alimentados pelos segredos do GitHub.
#
# (A substituição nativa do Keycloak em arquivos de realm tem falhas conhecidas
# em aberto, por isso a troca é feita aqui, de forma explícita e previsível.)
ARG APP_CSHARP_SECRET=dev-app-csharp-secret-nao-usar-em-producao
ARG BACKEND_ADMIN_SECRET=dev-backend-admin-secret-nao-usar-em-producao

RUN sed -i \
      -e "s|__APP_CSHARP_SECRET__|${APP_CSHARP_SECRET}|g" \
      -e "s|__BACKEND_ADMIN_SECRET__|${BACKEND_ADMIN_SECRET}|g" \
      /opt/keycloak/data/import/gerenciamento-endereco-realm.json \
 && ! grep -q "__APP_CSHARP_SECRET__\|__BACKEND_ADMIN_SECRET__" \
      /opt/keycloak/data/import/gerenciamento-endereco-realm.json

# Habilita métricas e health
ENV KC_METRICS_ENABLED=true
ENV KC_HEALTH_ENABLED=true
ENV KC_DB=postgres

# Build otimizado
RUN /opt/keycloak/bin/kc.sh build

EXPOSE 8080

ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "start-dev", "--import-realm"]
