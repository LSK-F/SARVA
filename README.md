# SARVA - Serviço de Apoio a Revendedores Autônomos
<p align="center" fontsize="20px">Support Service for Autonomous Resellers</p>

<p align="left">
  Uma aplicação web desenvolvida com ASP.NET Core MVC para otimizar a gestão de revendedores autônomos.
</p>

<br>

<p align="left">
  A web application developed with ASP.NET Core MVC to optimize the management of autonomous resellers.
</p>

---

## Technologies Used

<div align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Entity Framework">
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
</div>

---

## Prerequisites

Before you begin, ensure you have the following prerequisites installed:

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Visual Studio 2022](https://visualstudio.microsoft.com/) (or another IDE of your choice)
-   [SQL Server](https://www.microsoft.com/pt-br/sql-server/sql-server-downloads)
-   [EF Core Command-Line Tools](https://docs.microsoft.com/pt-br/ef/core/cli/dotnet)

---

## Installation and Configuration Guide

Follow the steps below to set up the development environment.

### 1. Database Configuration (`appsettings.json`)

Adjust the connection strings in the `appsetting.json` file to point to your SQL Server instance.

**Template for `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=aspnet-SARVA;Trusted_Connection=True;TrustServerCertificate=True",
    "connSARVA": "Server=YOUR_SERVER;Database=SARVA;Trusted_Connection=True;TrustServerCertificate=True"
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

**Instructions:**
-   Replace `YOUR_SERVER` with the name of your SQL Server instance (e.g., `localhost`, `.\\SQLEXPRESS`).
-   Adjust the database names (`Database`) if you want.

---

### 2. Applying Identiy Migrations

The project uses ASP.NET Core Identity to manage users. To create the tables in the database, run the following command in the terminal, in the project's root folder:

```bash
dotnet ef database update -c ApplicationDbContext
```

---

### 3. Business Model Database Script

The application's main database, which contains SARVA's business rules, needs to be created manually using the provided SQL script.

**File Location:**
-   The script is located in the `/DatabaseScripts` folder in the repository's root.

**Execution Steps:**
1.  Start SQL Server Management Studio (SSMS).
2.  Connect to the SQL Server instance you specified in the `connSARVA` connection string.
3.  Open the script file from the `/DatabaseScripts` folder.
4.  Execute the script. This will create the `SARVA` database with all the necessary tables and relationships.
