# LMS Backend

Backend services for the LMS project built with ASP.NET Core and Entity Framework Core (targeting .NET 10). This document covers day-to-day workflows for developers working on the repository.

## Prerequisites
- .NET SDK 10.0 (Preview) or newer installed and available on your `PATH`.
- Docker Desktop (or the Docker Engine CLI) for running SQL Server locally.
- SQL Server client tool of your choice (Azure Data Studio, sqlcmd, etc.) for manual inspection.

## Build, Run, and Test
- **Restore dependencies**: `dotnet restore` from the repo root the first time you clone or when packages change.
- **Build**: `dotnet build` (adds `--configuration Release` when producing artifacts). This validates the code compiles against the configured target framework.
- **Run the API**: `dotnet run --project LMS-Backend.csproj` (optionally add `--launch-profile "https"`). Regardless of environment, the app expects `ConnectionStrings:DefaultConnection`; provide different values per environment via `.env`, `.env.Production`, or environment variables named `ConnectionStrings__DefaultConnection`.
- **Smoke-test DB connectivity**: `dotnet run --project LMS-Backend.csproj -- --testconnection` loads the configured connection string for the current environment, attempts to connect once, then exits (non-zero exit code on failure). Use this before deployments to ensure the app can reach Azure SQL.
- **Hot reload (optional)**: `dotnet watch --project LMS-Backend.csproj` for rapid iteration during development.
- **Test**: `dotnet test LMS-Backend.Tests/LMS-Backend.Tests.csproj` runs all xUnit suites. From the repository root you can also run `dotnet test` to execute every test project in the solution. To target a single test or namespace, append `--filter "<expression>"` (e.g., `--filter "FullyQualifiedName~Users"`). For coverage reports, use `dotnet test LMS-Backend.Tests/LMS-Backend.Tests.csproj --collect:"XPlat Code Coverage"` and inspect the `TestResults/<timestamp>/coverage.cobertura.xml` file.

### UI Tests
- Selenium-based UI tests live under `LMS-Backend.Tests/UI/`.
- They are skipped by default so normal `dotnet test` runs do not depend on a frontend server being available.
- To run them intentionally, start the frontend first and note its base URL, for example `http://localhost:3000`.
- Then set `RUN_UI_TESTS=true` and `LMS_UI_BASE_URL` before running the test project:
  ```bash
  RUN_UI_TESTS=true LMS_UI_BASE_URL=http://localhost:3000 dotnet test LMS-Backend.Tests/LMS-Backend.Tests.csproj
  ```
- You can also run only the UI namespace:
  ```bash
  RUN_UI_TESTS=true LMS_UI_BASE_URL=http://localhost:3000 dotnet test LMS-Backend.Tests/LMS-Backend.Tests.csproj --filter "FullyQualifiedName~LMS_Backend.Tests.UI"
  ```
- The current UI coverage includes login page validation and admin login flow checks. Ensure the frontend test environment has the expected admin account if you run the admin login test.

### Change Environment Profile
- `dotnet run` without flags loads the profile named `http` from `Properties/launchSettings.json`, which sets `ASPNETCORE_ENVIRONMENT=Development`. Use this for local Docker + dev DB testing.
- To run against another environment temporarily, export both `DOTNET_ENVIRONMENT` and `ASPNETCORE_ENVIRONMENT` before launching and bypass launch settings so their values win, e.g. `DOTNET_ENVIRONMENT=Production ASPNETCORE_ENVIRONMENT=Production dotnet run --no-launch-profile --urls http://localhost:5251`.
- To keep using launch profiles, duplicate one of the existing entries in `Properties/launchSettings.json`, rename it (for example `Production`), set `ASPNETCORE_ENVIRONMENT` accordingly, and then run `dotnet run --launch-profile Production` (or choose it via Visual Studio/Rider UI). Any profile-specific URLs you configure there will be honored.
- Remember that whichever profile/environment you pick must still provide `ConnectionStrings__DefaultConnection` (through `.env`, secrets manager, or platform settings) so the app can reach the correct database.

### Password Reset Flow
- `POST /api/v1/auth/forgot-password` accepts `{ "email": "user@example.com" }` and always returns `{ "message": "If an account with this email exists, a password reset link has been sent." }`. Behind the scenes, the API only generates a token and sends email when the address exists and is confirmed, but the consistent response prevents account enumeration.
- The email contains a URL-safe token plus the `userId`; clients should direct users to a UI that calls `POST /api/v1/auth/reset-password` with `{ "userId": "...", "token": "...", "newPassword": "NewPassword123!", "confirmPassword": "NewPassword123!" }`.
- `POST /api/v1/auth/reset-password` validates the token (single-use, time-limited by Identity) and password confirmation, revokes existing refresh tokens, and returns `{ "message": "Password has been reset successfully. You can now sign in with the new password." }`. Invalid or expired tokens yield `400` with a descriptive error payload.

## SQL Server via Docker
1. Pull the official SQL Server image once:
   ```bash
   docker pull mcr.microsoft.com/mssql/server:2022-latest
   ```
2. Start a container (matching the existing `ConnectionStrings:DefaultConnection` development credentials) and expose port `1433`:
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
4. **Apply the migration to the database** targeted by `ConnectionStrings:DefaultConnection` (ensure your `.env`/environment variables point to the intended server first):
   ```bash
   dotnet ef database update
   ```
5. **Review generated SQL** with `dotnet ef migrations script` if you need a script for manual deployments.

Commit both the entity changes and the generated migration files so the schema stays synchronized across environments.
