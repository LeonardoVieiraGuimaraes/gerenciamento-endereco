-- Script de Criação das Tabelas exigido pelo teste (SQL Server)

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    Senha VARCHAR(200) NOT NULL
);

CREATE TABLE Enderecos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Cep VARCHAR(10) NOT NULL,
    Logradouro VARCHAR(200) NOT NULL,
    Complemento VARCHAR(200) NULL,
    Bairro VARCHAR(100) NOT NULL,
    Cidade VARCHAR(100) NOT NULL,
    Uf CHAR(2) NOT NULL,
    Numero VARCHAR(20) NOT NULL,
    UsuarioId INT NOT NULL,
    CONSTRAINT FK_Enderecos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
);
