# LMS Backend

Backend services for the LMS project built with ASP.NET Core and Entity Framework Core (targeting .NET 10). This document covers day-to-day workflows for developers working on the repository.

## Prerequisites
- .NET SDK 10.0 (Preview) or newer installed and available on your `PATH`.
- Docker Desktop (or the Docker Engine CLI) for running SQL Server locally.
- SQL Server client tool of your choice (Azure Data Studio, sqlcmd, etc.) for manual inspection.

## Build, Run, and Test
- **Restore dependencies**: `dotnet restore` from the repo root the first time you clone or when packages change.
- **Build**: `dotnet build` (adds `--configuration Release` when producing artifacts). This validates the code compiles against the configured target framework.
- **Run the API**: `dotnet run --project LMS-Backend.csproj` (optionally add `--launch-profile "https"`). The command will use the connection string from `appsettings.Development.json` or any `ConnectionStrings__DefaultConnection` environment variable override.
- **Hot reload (optional)**: `dotnet watch --project LMS-Backend.csproj` for rapid iteration during development.
- **Test**: once a test project exists, execute `dotnet test`. If you add multiple test projects (for example `LMS-Backend.Tests`), running the command from the solution root will discover them automatically. At present the repo does not include automated tests yet, so this command will simply report “No test projects found.”

## SQL Server via Docker
1. Pull the official SQL Server image once:
   ```bash
   docker pull mcr.microsoft.com/mssql/server:2022-latest
   ```
2. Start a container (matching the existing `DefaultConnection` credentials) and expose port `1433`:
   ```bash
   docker run -e "ACCEPT_EULA=Y" \
              -e "SA_PASSWORD=StrongPass!123" \
              -p 1433:1433 \
              --name lms-sql \
              -d mcr.microsoft.com/mssql/server:2022-latest
   ```
3. (Optional) Persist data by adding `-v lms_sql_data:/var/opt/mssql` to the command above so container restarts keep the database files.
4. Verify the server is accepting connections with `docker logs lms-sql` and by connecting via your preferred SQL client using `Server=localhost,1433;User Id=sa;Password=StrongPass!123;TrustServerCertificate=True;`.

If you ever need to stop or remove the instance: `docker stop lms-sql` and `docker rm lms-sql`. Restart with `docker start lms-sql`.

## Entity Framework Core Migrations
1. **Install the EF CLI (once per machine)**: `dotnet tool install --global dotnet-ef` (or update via `dotnet tool update --global dotnet-ef`).
2. **Add or update entity classes** inside `Models/Entities/` and any related configuration files.
3. **Create a migration** from the repo root (single project solution, so no extra flags needed):
   ```bash
   dotnet ef migrations add <MigrationName>
   ```
   Replace `<MigrationName>` with any descriptive name, e.g. `UpdateEnrollmentSchema`. EF will create files under `Migrations/` (or the configured folder).
4. **Apply the migration to the database** targeted by `DefaultConnection`:
   ```bash
   dotnet ef database update
   ```
5. **Review generated SQL** with `dotnet ef migrations script` if you need a script for manual deployments.

Commit both the entity changes and the generated migration files so the schema stays synchronized across environments.
