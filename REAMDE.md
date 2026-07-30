
Este repositório contém três módulos principais:

/backend   → API em ASP.NET Core MVC + EF Core + SQL Server
/frontend  → Interface web (HTML, CSS, JS ou framework moderno)
/auth      → Keycloak (OpenID Connect) para autenticação


## 🎯 **Objetivo do Sistema**

Aplicação web em C# que permite:

- Autenticação de usuário via **Keycloak + OpenID Connect**
- CRUD completo de endereços
- Busca automática de endereço via **API ViaCEP**
- Exportação dos endereços para **CSV**
- Persistência em **SQL Server**

---

## 📂 **Estrutura das Pastas**

### **backend/**
- ASP.NET Core MVC
- Entity Framework Core
- Controllers (Auth, Endereços)
- Services (ViaCEP, CSV Export)
- Migrations
- Scripts SQL

### **frontend/**
- HTML, CSS, JS  
ou  
- React / Vue / Angular (opcional)

### **auth/**
- Configurações do Keycloak:
  - Realm
  - Client
  - Roles
  - Redirect URIs

---

# ✔️ **CHECKLIST COMPLETO DO PROJETO**

## 🔹 1. Preparação do Ambiente
- Instalar .NET 8 SDK  
- Instalar SQL Server  
- Instalar Docker (opcional)  
- Instalar Keycloak (Docker recomendado)  
- Criar repositório público no GitHub  

---

## 🔹 2. Autenticação (auth/)
- Criar Realm: `sistema-enderecos`
- Criar Client: `app-csharp`
- Configurar:
  - OpenID Connect
  - Redirect URI: `https://localhost:5001/signin-oidc`
  - Client Secret
- Criar Role: `user`
- Criar usuários de teste

---

## 🔹 3. Backend (backend/)
### **3.1 Criar projeto**
- ASP.NET Core MVC
- EF Core + SQL Server

### **3.2 Criar Models**
- Endereco
- Usuario (opcional, se usar Keycloak)

### **3.3 Criar Controllers**
- AuthController (OIDC)
- EnderecosController (CRUD)

### **3.4 Criar Services**
- ViaCepService
- CsvExportService

### **3.5 Criar Scripts SQL**
- Tabela `Usuarios`
- Tabela `Enderecos`

### **3.6 Criar Migrations**
- `dotnet ef migrations add Initial`
- `dotnet ef database update`

---

## 🔹 4. Frontend (frontend/)
- Criar páginas:
  - Login (se não usar Keycloak direto)
  - Lista de endereços
  - Formulário de cadastro
- Usar Bootstrap
- Consumir API do backend

---

## 🔹 5. Integração ViaCEP
- GET `https://viacep.com.br/ws/{cep}/json/`
- Preencher automaticamente:
  - logradouro
  - bairro
  - cidade
  - uf

---

## 🔹 6. Exportação CSV
- Criar endpoint `/enderecos/exportar`
- Gerar arquivo CSV com `StringBuilder`
- Retornar `File()` para download

---

## 🔹 7. CI/CD (GitHub Actions ou GitLab CI/CD)
### **Pipeline recomendado**
- Build
- Test
- Publish
- Docker build
- Deploy automático (Azure, AWS, Render, Railway)

---

## 🔹 8. Deploy
- Criar Dockerfile
- Criar docker-compose
- Deploy automático via pipeline

---

## 🔹 9. Commits obrigatórios
Cada funcionalidade deve ter um commit separado:

- Criação do projeto
- Configuração do Keycloak
- CRUD de endereços
- Integração ViaCEP
- Exportação CSV
- Scripts SQL
- Pipeline CI/CD
- Deploy

---

## 📄 **Scripts SQL (exigidos pelo teste)**

### **Tabela Usuarios**
```sql
CREATE TABLE Usuarios (
    Id INT IDENTITY PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Usuario VARCHAR(50) NOT NULL,
    Senha VARCHAR(200) NOT NULL
);
```

### **Tabela Enderecos**
```sql
CREATE TABLE Enderecos (
    Id INT IDENTITY PRIMARY KEY,
    Cep VARCHAR(10) NOT NULL,
    Logradouro VARCHAR(200) NOT NULL,
    Complemento VARCHAR(200) NULL,
    Bairro VARCHAR(100) NOT NULL,
    Cidade VARCHAR(100) NOT NULL,
    Uf CHAR(2) NOT NULL,
    Numero VARCHAR(20) NOT NULL,
    UsuarioId INT NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
