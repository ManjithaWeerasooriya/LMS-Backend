# AGENTS.md

## 1. Project Overview
- This repository is a layered ASP.NET Core Web API for an LMS backend targeting `.NET 10` (`net10.0`).
- Major feature areas currently implemented: authentication, user profile management, courses, quizzes, materials, live sessions, dashboards, admin user/course management, and reporting.
- Persistence uses `Entity Framework Core` with `SQL Server` via `ApplicationDBContext`.
- Identity/auth uses `ASP.NET Core Identity` with a custom `User` entity plus JWT access tokens and database-backed refresh tokens.
- External integrations already present in code: `Azure Blob Storage` for course materials and profile images, `Azure Communication Services` for live-session identity/chat/join tokens/recording, and `SMTP` email via `MailKit`.
- Startup behavior in `Program.cs`: loads `.env` and `.env.{Environment}`, configures CORS/Identity/JWT auth/Swagger/health checks, and applies pending EF migrations on startup.

## 2. Architecture & Data Flow
- The project is a layered monolith, not Clean Architecture and not repository-driven.
- Dominant flow: `Controller` receives HTTP request, `Service` enforces business rules, `ApplicationDBContext` and/or external service performs work, and the result is projected into DTOs and returned.
- Controllers are under `Controllers/`.
- Business logic mostly lives under `Services/`.
- Database access is direct from services through `ApplicationDBContext`; there is no repository layer.
- Entity configuration is split between `Data/ApplicationDBContext.cs` and `Data/Configurations/*.cs`.
- DTOs and entities are clearly separated: request/response models live under `Models/DTOs/*`, and persistence models live under `Models/Entities/*`.
- Newer modules use domain exceptions plus `ApiControllerBase`; older modules often return plain MVC results directly.

## 3. Project Structure
- `Program.cs`: composition root, middleware, DI, auth, health checks, migration startup logic
- `Controllers/`: API endpoints grouped by role/feature
- `Services/`: business services, token/email/storage/ACS integrations, dashboards, reporting
- `Services/Reporting/`: reporting-specific abstractions and implementation
- `Data/`: `ApplicationDBContext`, EF Core design-time factory, model configuration classes
- `Infrastructure/Auth/`: role/policy constants and auth helpers
- `Infrastructure/HealthChecks/`: DB, Blob Storage, ACS health checks and response writer
- `LMS.Infrastructure/Seed/`: identity bootstrap seeding logic
- `Models/Entities/`: EF entities and enums
- `Models/DTOs/`: DTOs grouped by feature (`Auth`, `Courses`, `LiveSessions`, `Quiz`, `Admin`, `Reports`, etc.)
- `Models/Exceptions/`: `NotFoundException`, `ForbiddenException`, `ConflictException`, `ServiceUnavailableException`
- `Migrations/`: EF Core migrations and snapshot
- `LMS-Backend.Tests/`: xUnit tests, Moq-based controller/service tests, and Selenium UI tests
- `Properties/`: `launchSettings.json`; note that `AzureStorageService.cs` currently also lives here even though its namespace is `LMS_Backend.Services`
- `Tools/PasswordHashTool/`: separate utility project

## 4. Coding Standards
- Use `PascalCase` for types, methods, DTOs, and enums.
- Use `_camelCase` for private readonly fields.
- Async methods are consistently suffixed with `Async`.
- DTO naming is descriptive and feature-based: `CreateCourseRequestDto`, `LiveSessionDto`, `TeacherDashboardResponseDto`, `UserListResponseDto`.
- Constructor injection is the standard for controllers and services.
- Manual mapping is the norm. Do not assume AutoMapper; it is not used anywhere.
- Request validation is a mix of data annotations on DTOs, `ModelState` checks in controllers, and guard clauses/normalization inside services.
- String normalization is common before persistence: `Trim()`, null cleanup, enum parsing, required/optional normalization.
- `DateTime.UtcNow` is used throughout for timestamps.
- Read queries often use `AsNoTracking()` when entities are not being updated.
- Cancellation tokens are standard in newer controller/service code, especially quizzes, materials, live sessions, dashboards, and reporting.

## 5. API Design Conventions
- Most routes are versioned under `/api/v1/...`.
- Public endpoints are an older exception under `/api/public`.
- Role-scoped route groups are common: `/api/v1/teacher/...`, `/api/v1/student/...`, `/api/v1/admin/...`.
- Two response styles coexist: newer controllers inherit `ApiControllerBase` and translate custom exceptions into `ApiResponse<T>`, while older controllers inherit `ControllerBase` and return plain DTOs or anonymous objects.
- Match the style of the controller you are editing instead of forcing a repo-wide response rewrite.
- Example of the newer controller pattern from quizzes/live sessions:

```csharp
var teacherId = GetCurrentUserId();
if (string.IsNullOrWhiteSpace(teacherId))
{
    return UnauthorizedResponse();
}

try
{
    var quiz = await _quizService.GetTeacherQuizByIdAsync(teacherId, quizId, cancellationToken);
    return Success(quiz, "Quiz retrieved successfully.");
}
catch (Exception ex)
{
    return HandleException(ex);
}
```

- `CreatedAtAction`, `NoContent`, and idempotent success patterns are already used and should be preserved where appropriate.

## 6. Database & Persistence Rules
- `ApplicationDBContext` inherits from `IdentityDbContext<User>`.
- The actual EF provider is `SQL Server`; both `Program.cs` and `ApplicationDbContextFactory` use `UseSqlServer(...)`.
- `Program.cs` enables SQL retry-on-failure for the main DB connection.
- There is no repository abstraction; services query `DbSet`s directly.
- Favor LINQ projection to DTOs over loading entities and mapping afterward.
- Keep entity configuration consistent with the existing split: simple relationship/index rules may stay in `ApplicationDBContext`, while feature-specific mapping usually lives in `Data/Configurations/*`.
- Existing schema conventions already in place: unique refresh token per `(UserId, DeviceId)`, unique course enrollment per `(CourseId, StudentId)`, and SQL Server-specific defaults such as `GETUTCDATE()`.
- Soft delete is only implemented where it already exists: quizzes, questions, and question options use `IsDeleted` + `HasQueryFilter(...)`; many other modules still use hard delete, archive, or status transitions instead.
- If you add schema changes, update entities/configuration and create a migration in `Migrations/`.
- EF CLI tooling depends on `Data/ApplicationDbContextFactory.cs`, which also loads `.env`.

## 7. Authentication & Authorization
- Authentication stack: `ASP.NET Core Identity` for users/password rules/email confirmation/reset tokens, `JWT Bearer` authentication for API access, and refresh tokens persisted in `RefreshTokens`.
- Current app roles are defined in `Infrastructure/Auth/AuthConfiguration.cs`: `Teacher`, `Student`, and legacy `Admin` still recognized and normalized into teacher behavior.
- Authorization is mostly policy-based through `AppPolicies.TeacherOnly` and `AppPolicies.StudentOnly`.
- Some controllers also use role strings directly, for example combined teacher/student access.
- Auth flow currently requires confirmed email before login.
- Password changes and password resets revoke refresh tokens.
- CORS is restricted to configured frontend origins in `Program.cs`; do not widen it casually.

## 8. Common Patterns in This Codebase
- Direct service + EF query projection:

```csharp
return await _context.Materials
    .AsNoTracking()
    .Where(m => m.CourseId == courseId)
    .Select(m => new MaterialDto { ... })
    .ToListAsync(cancellationToken);
```

- Guard methods enforce access rules inside services:
  `EnsureTeacherOwnsCourseAsync(...)`, `EnsureStudentEnrolledInCourseAsync(...)`, and `ThrowTeacherQuizAccessExceptionAsync(...)`.
- Domain exceptions are used in newer services:
  `NotFoundException`, `ForbiddenException`, `ConflictException`, and `ServiceUnavailableException`.
- Manual DTO creation is preferred over shared mapper layers.
- Service registration is mixed: reusable/domain services often use interfaces (`IQuizService`, `ILiveSessionService`, `IMaterialService`, `IReportingService`), while some app services are injected as concrete types (`CourseService`, `AdminService`, `TeacherDashboardService`, `TokenService`).
- External-service wrappers exist already; reuse them before adding new SDK calls directly from controllers.

## 9. Development Workflow
1. Read `Program.cs`, the target controller, the matching service, related DTOs, and existing tests in the same feature folder.
2. If the feature needs persistence changes, update `Models/Entities/*`.
3. If the feature needs persistence changes, update `Data/ApplicationDBContext.cs` and/or `Data/Configurations/*`.
4. If the feature needs persistence changes, add a migration.
5. Add or extend DTOs under the correct feature folder in `Models/DTOs/`.
6. Put business rules in a service, not in EF entities.
7. Use direct `ApplicationDBContext` access from the service; do not add a repository layer.
8. Register new services in `Program.cs`.
9. Add or extend controllers under the existing route/role grouping and match the response style already used in that controller/module.
10. If the change touches auth, storage, or live sessions, reuse the existing `TokenService`, `AzureStorageService`, `AzureCommunicationIdentityService`, and `AzureCommunicationLiveSessionService`.
11. Add tests under `LMS-Backend.Tests/<Feature>/`.
12. For schema work, keep generated migration files with the code change.

## 10. Do & Don’t Rules
- Do use `DateTime.UtcNow` for persisted timestamps.
- Do trim and normalize inbound strings before saving.
- Do pass `CancellationToken` through new async APIs when you are working in a newer feature area.
- Do use `AsNoTracking()` for read-only queries.
- Do enforce teacher ownership / student enrollment checks in services before returning data.
- Do throw existing domain exceptions in newer service-based modules and let `ApiControllerBase` translate them.
- Do preserve soft-delete behavior in the quiz module.
- Do use `UserManager` / `SignInManager` for Identity mutations instead of writing around them.
- Do keep configuration in `appsettings*.json` + environment variables; secrets are not hardcoded.
- Don’t introduce AutoMapper, MediatR, or a repository layer for isolated changes; those patterns are not present here.
- Don’t assume one global response contract. Match the module: use `ApiResponse<T>` in newer `ApiControllerBase` controllers and plain DTO/anonymous MVC responses in older controllers.
- Don’t bypass the existing Azure/email wrapper services from controllers.
- Don’t assume the database is MySQL because `AdminDiagnosticsService` exposes a `MySql` DTO field; the actual EF provider is SQL Server.
- Don’t convert hard-delete/archive/status modules into soft-delete unless the feature already uses that pattern.

## 11. Output Expectations for AI Agents
- Base changes on the existing module’s conventions, not generic ASP.NET defaults.
- Keep edits localized and consistent with the current layered structure.
- Preserve public API shapes unless the task explicitly requires a contract change.
- When adding endpoints, include the full pattern used in that area: route, auth policy/role attribute, DTOs, service method, and tests.
- When adding persistence, include entity/configuration/DbContext/migration updates together.
- Prefer manual LINQ projection and explicit ownership checks over abstract frameworks.
- Reuse existing exception types, auth helpers, Azure wrappers, and option classes.
- Add or update tests in `LMS-Backend.Tests/` for behavior changes; controller tests commonly use `Moq`, service tests commonly use EF Core in-memory setups, and UI tests live under `LMS-Backend.Tests/UI/`.
