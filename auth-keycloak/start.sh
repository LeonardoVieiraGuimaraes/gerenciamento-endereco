#!/bin/bash
# Script para iniciar o Keycloak via Docker Compose
echo "Iniciando o Keycloak e o banco de dados..."
docker compose up -d --build
echo "Keycloak iniciado com sucesso! Acesse http://localhost:8089"
