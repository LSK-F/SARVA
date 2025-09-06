# SARVA - Serviço de Apoio a Revendedores Autônomos

<p align="center">
  <em>Uma aplicação web desenvolvida com ASP.NET Core 9.0 MVC para otimizar a gestão de revendedores autônomos.</em>
</p>

---

## Tecnologias Utilizadas

<div align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Entity Framework">
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
</div>

---

## Pré-requisitos

Antes de começar, garanta que você tenha os seguintes pré-requisitos instalados:

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Visual Studio 2022](https://visualstudio.microsoft.com/) (ou outra IDE de sua preferência)
-   [SQL Server](https://www.microsoft.com/pt-br/sql-server/sql-server-downloads)
-   [Ferramentas de Linha de Comando do EF Core](https://docs.microsoft.com/pt-br/ef/core/cli/dotnet)

---

## ⚙️ Guia de Instalação e Configuração

Siga os passos abaixo para configurar o ambiente de desenvolvimento.

### 1. Configuração do Banco de Dados (`appsettings.json`)

Ajuste as *connection strings* no arquivo `appsettings.json` para que apontem para a sua instância do SQL Server.

**Template do `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_SERVIDOR;Database=SARVA_Identity_DB;Trusted_Connection=True;TrustServerCertificate=True",
    "connSARVA": "Server=SEU_SERVIDOR;Database=SARVA_Business_DB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Instruções:**
-   Substitua `SEU_SERVIDOR` pelo nome da sua instância do SQL Server (ex: `localhost`, `.\\SQLEXPRESS`).
-   Ajuste o nome dos bancos de dados (`Database`) se desejar.

---

### 2. Aplicando Migrações do Identity

O projeto utiliza o ASP.NET Core Identity para gerenciar usuários. Para criar as tabelas no banco de dados, execute o seguinte comando no terminal, na pasta raiz do projeto:

```bash
dotnet ef database update -c ApplicationDbContext
```

---

### 3. Script do Banco de Dados do Modelo de Negócios

O banco de dados principal da aplicação, que contém as regras de negócio do SARVA, precisa ser criado manualmente usando o script SQL fornecido.

**Localização do Arquivo:**
-   O script se encontra na pasta `/DatabaseScripts` na raiz do repositório.

**Passos para Execução:**
1.  Inicie o SQL Server Management Studio (SSMS) ou a sua ferramenta de banco de dados preferida.
2.  Conecte-se à instância do SQL Server que você especificou na connection string `connSARVA`.
3.  Abra o arquivo de script da pasta `/DatabaseScripts`.
4.  Execute o script. Isso irá criar o banco de dados `SARVA` com todas as tabelas e relacionamentos necessários.
