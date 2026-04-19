# LMS Backend

Backend services for the LMS project built with ASP.NET Core and Entity Framework Core (targeting .NET 10). This document covers day-to-day workflows for developers working on the repository.

## Prerequisites
- .NET SDK 10.0 (Preview) or newer installed and available on your `PATH`.
- Docker Desktop (or the Docker Engine CLI) for running SQL Server locally.
- SQL Server client tool of your choice (Azure Data Studio, sqlcmd, etc.) for manual inspection.
- Azurite for local Azure Blob Storage emulation during Development.

## Build, Run, and Test
- **Restore dependencies**: `dotnet restore` from the repo root the first time you clone or when packages change.
- **Build**: `dotnet build` (adds `--configuration Release` when producing artifacts). This validates the code compiles against the configured target framework.
- **Run the API**: `dotnet run --project LMS-Backend.csproj` (optionally add `--launch-profile "https"`). Regardless of environment, the app expects `ConnectionStrings:DefaultConnection`; provide different values per environment via `.env`, `.env.Production`, or environment variables named `ConnectionStrings__DefaultConnection`.
- **Blob storage configuration**: in `Development`, the backend uses `AzureStorage:ConnectionString=UseDevelopmentStorage=true`, which points to Azurite. In `Production`, set `AzureStorage__ConnectionString` to the real Azure Storage account connection string.
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
- The current UI coverage includes login page validation and privileged-teacher login flow checks. Ensure the frontend test environment has the expected teacher account if you run that UI test.

### Change Environment Profile
- `dotnet run` without flags loads the profile named `http` from `Properties/launchSettings.json`, which sets `ASPNETCORE_ENVIRONMENT=Development`. Use this for local Docker + dev DB testing.
- To run against another environment temporarily, export both `DOTNET_ENVIRONMENT` and `ASPNETCORE_ENVIRONMENT` before launching and bypass launch settings so their values win, e.g. `DOTNET_ENVIRONMENT=Production ASPNETCORE_ENVIRONMENT=Production dotnet run --no-launch-profile --urls http://localhost:5251`.
- To keep using launch profiles, duplicate one of the existing entries in `Properties/launchSettings.json`, rename it (for example `Production`), set `ASPNETCORE_ENVIRONMENT` accordingly, and then run `dotnet run --launch-profile Production` (or choose it via Visual Studio/Rider UI). Any profile-specific URLs you configure there will be honored.
- Remember that whichever profile/environment you pick must still provide `ConnectionStrings__DefaultConnection` so the app can reach the correct database, plus the matching blob setting:
  `AzureStorage__ConnectionString=UseDevelopmentStorage=true` for local Development or a real Azure Storage connection string for Production.

## Azurite for Local Blob Storage
The backend is configured to use Azurite automatically in the `Development` environment through `appsettings.Development.json` and `.env`.

### Install Azurite on your machine
You can install Azurite in either of these common ways:

- With Node.js and npm installed:
  ```bash
  npm install -g azurite
  ```
- With Visual Studio Code:
  install the `Azurite` extension from the VS Code Extensions marketplace.

After installation, verify the CLI is available:

```bash
azurite --version
```

### Run Azurite locally
Start Azurite before running the API in `Development`:

```bash
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

This starts the local Blob service using the default development endpoints expected by `UseDevelopmentStorage=true`.

The backend will then connect through:

- Blob endpoint: `http://127.0.0.1:10000/devstoreaccount1`
- Queue endpoint: `http://127.0.0.1:10001/devstoreaccount1`
- Table endpoint: `http://127.0.0.1:10002/devstoreaccount1`

### Use the Azurite VS Code extension
If you prefer working from VS Code instead of the CLI:

1. Install the `Azurite` extension.
2. Open the command palette.
3. Run `Azurite: Start`.
4. Keep Azurite running while the backend is running in `Development`.

The extension starts the same local emulator services, so the backend will still work with `AzureStorage__ConnectionString=UseDevelopmentStorage=true`.

### Development configuration used by this project
- `appsettings.Development.json` sets `AzureStorage:ConnectionString` to `UseDevelopmentStorage=true`.
- `.env` also sets `AzureStorage__ConnectionString=UseDevelopmentStorage=true`.
- The default blob container used by the API is `course-materials`.
- The backend also pins an older Azure Blob service API version when running against Azurite in `Development`. This avoids failures where newer `Azure.Storage.Blobs` SDK versions send a storage API version that the installed Azurite build does not support.

### Production configuration
- `appsettings.json` keeps the Azure storage value empty by default.
- `.env.Production` contains `AzureStorage__ConnectionString`, which must be set to your real Azure Storage account connection string before deployment.
- Production does not use Azurite.

## Azure Communication Services (Live Classes)
The backend is prepared to use Azure Communication Services (ACS) for live classes via configuration only. No feature code is wired up yet; this sprint focuses on DevOps and configuration so future work can plug into ACS.

### 1. Create an ACS resource
Use either the Azure Portal or Azure CLI:

- In the Azure Portal:
  1. Go to `Create a resource` and search for `Communication Services`.
  2. Create a new resource (e.g., name `lms-acs-liveclass`) in the same subscription and region as the backend Web App.
  3. After deployment, open the resource and go to **Keys & Connection String** to copy:
     - The primary connection string.
     - The endpoint URL.

- With Azure CLI (example):
  ```bash
  az communication create \
    --name lms-acs-liveclass \
    --resource-group <your-resource-group> \
    --data-location <region> \
    --location <region>
  ```
  Then retrieve keys:
  ```bash
  az communication list-key \
    --name lms-acs-liveclass \
    --resource-group <your-resource-group>
  ```

### 2. Configuration keys used by the backend
The backend expects ACS configuration under the `AzureCommunication` section:

- In `appsettings.json` / `appsettings.Development.json`:
  - `AzureCommunication:ConnectionString`
  - `AzureCommunication:Endpoint`

These are defined but left empty by default so that real secrets can be supplied via environment variables or App Service configuration.

### 3. Local development configuration
For local development, configure ACS in `.env` (do not commit real secrets for shared environments):

```bash
AzureCommunication__ConnectionString="<your-dev-acs-connection-string>"
AzureCommunication__Endpoint="<your-dev-acs-endpoint>"
```

When running via `dotnet run`, ASP.NET Core will bind these to `AzureCommunication:ConnectionString` and `AzureCommunication:Endpoint`.

### 4. Production configuration
For production, prefer setting ACS values as App Settings on the Azure Web App hosting the backend:

- In the Azure Portal, open your Web App (e.g., `lms-backend-deepana`).
- Under **Configuration → Application settings**, add:
  - `AzureCommunication__ConnectionString` = `<your-prod-acs-connection-string>`
  - `AzureCommunication__Endpoint` = `<your-prod-acs-endpoint>`
- Save and restart the Web App.

These values will be available to the backend at runtime via the standard configuration system without hardcoding them into the repository. `.env.Production` contains commented placeholders for the same keys if you choose to manage them via environment files instead.

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
