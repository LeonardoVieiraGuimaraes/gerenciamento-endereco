# Etapa base para o runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Etapa de build e restauração de pacotes
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GerenciamentoEndereco.API.csproj", "."]
RUN dotnet restore "./GerenciamentoEndereco.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./GerenciamentoEndereco.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etapa de publicação
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GerenciamentoEndereco.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa final (Cópia dos binários e execução)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GerenciamentoEndereco.API.dll"]
