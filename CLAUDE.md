# CivicJobs

Technical interview exercise: a job postings app for public sector HR recruiters.
Read `docs/PRD.md` (problem, scope, requirement and rule IDs) and `docs/user-story.md` (domain model, status rules, API contract) before any task. They win over anything you assume. Anything listed as a non-goal in the PRD is out of scope.

## Stack

- .NET 10, C#, ASP.NET Core Web API with controllers (`[ApiController]`), no Minimal APIs
- EF Core with SQLite, migrations in Infrastructure
- JWT bearer auth, `PasswordHasher<T>` for passwords
- Swagger UI (Swashbuckle) with a Bearer "Authorize" button in both APIs
- `TimeProvider` for current time (never `DateTime.Now` or `DateTime.UtcNow` directly)
- Tests: xUnit, Shouldly, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing`
- Frontend: Angular (latest stable), standalone components, signals, reactive forms, Angular Material

No CQRS or MediatR. The Application layer uses plain services.

## Repository layout

```
backend/
  CivicJobs.slnx
  CivicJobs.Core/              Entities, Enums, Exceptions, Repositories (interfaces only). No project or package references.
  CivicJobs.Application/       Services (IJobPostingService, IAuthService, IUserService), Abstractions (IPasswordHasher, ITokenService),
                               InputModels, ViewModels, Validators. References Core only.
  CivicJobs.Infrastructure/    Persistence (CivicJobsDbContext, Configurations, Repositories, Migrations, DbInitializer),
                               Auth (PasswordHasher, JwtTokenService, JwtOptions), DependencyInjection,
                               Web (shared by both APIs: GlobalExceptionHandler, JWT bearer, Swagger, ClaimsPrincipal.GetUserId).
                               References Application and Core.
  CivicJobs.Identity.Api/      AuthController, UsersController. References Application and Infrastructure.
  CivicJobs.Jobs.Api/          JobPostingsController, PublicJobPostingsController (anonymous, FR-11). References Application and Infrastructure.
  CivicJobs.UnitTests/         Core/, Application/, Infrastructure/ (folders mirror the layers)
  CivicJobs.Identity.ApiTests/ endpoint tests with WebApplicationFactory
  CivicJobs.Jobs.ApiTests/     endpoint tests with WebApplicationFactory
frontend/                      Angular app
genai-exercise/                GenAI part of the exercise (prompt, raw output, review). Never referenced by backend or frontend.
docs/
```

The two API test projects are separate on purpose: each web project has its own `Program` and `appsettings.json`, and one test project referencing both would clash.

## Architecture rules (hard rules)

- Dependencies point inward: Api -> Application -> Core, and Infrastructure -> Application/Core. **Application never references Infrastructure.**
- Core and Application never reference EF Core, ASP.NET Core or any Infrastructure type. Validators use the `FluentValidation` package, not `FluentValidation.AspNetCore`.
- Business rules live in the Core entity (state, transitions, invariants) or in an Application service (rules that need other data, like a unique email). Never in controllers or repositories.
- Controllers are thin: receive the InputModel, call the service, map the result to an HTTP response.
- Never expose entities in the API. InputModels in, ViewModels out.
- The owner of a posting comes from the token claim (`sub`), never from the request body or query string.
- A posting owned by another user returns 404.
- Domain exceptions inherit from a base `DomainException` in Core:
  - `DomainValidationException`: invalid data that breaks an entity invariant (for example a bad salary range). Maps to 400.
  - `InvalidStatusTransitionException`: an action not allowed in the current status (publish, close, edit or delete). Maps to 409.
- Application exceptions (in `CivicJobs.Application/Exceptions`), for rules that need other data or ownership:
  - `NotFoundException`: the resource doesn't exist or belongs to another user (BR-08). Maps to 404.
  - `ConflictException`: the email is already in use (BR-01). Maps to 409.
  - `InvalidCredentialsException`: unknown email or wrong password, same message for both. Maps to 401.
  - `UnauthorizedException`: a valid token whose user no longer exists. Maps to 401.
- FluentValidation errors map to 400 with `ValidationProblemDetails`; other errors use `ProblemDetails`. Use a global exception handler (`IExceptionHandler`), no try/catch in controllers.
- No secrets in code. JWT settings come from the `Jwt` configuration section; `appsettings.Development.json` holds a clearly fake dev key. Both APIs use the same issuer, audience and key.
- Async all the way, pass `CancellationToken`.

## Database

- One SQLite file shared by both APIs: connection string `Data Source=../civicjobs.db` (resolves to `backend/civicjobs.db` from each API folder).
- In Development, each API calls `DbInitializer.InitializeAsync()` at startup: apply migrations, then seed only if there are no users. It must be safe to run twice.
- Seed data is defined in `docs/user-story.md`.

## Local ports

- Identity API: http://localhost:5101
- Jobs API: http://localhost:5102
- Frontend: http://localhost:4200

## Git workflow

- One branch per block: `feature/backend`, then `feature/frontend`. No PR per feature.
- Commit messages: only the conventional commit line, plus a short body if needed. No `Co-Authored-By` trailers or any Claude or AI attribution.
- Never delete branches, local or remote.

## TDD workflow (per feature, without stopping)

1. Write all the tests for the feature. Run them and confirm they fail for the right reason (an assertion, or a missing member the tests drive). Commit `test: ...`.
2. Implement the minimum code. Run all tests green. Commit `feat: ...`.
3. Refactor with tests green. Commit `refactor: ...` only when something changed.
4. Move to the next feature.

Only stop and ask me if a decision isn't covered by this file, `docs/PRD.md` or `docs/user-story.md`.
This replaces the "stop and wait for my OK" step in the `tdd-cycle` skill; follow the rest of the skill (rule ids, layers, boundaries).
At the end of each block, run the `architecture-reviewer` agent and fix every "Must fix" item before pushing.

Where tests go:
- Entity rules: `CivicJobs.UnitTests/Core`
- Services and validators (repositories mocked with NSubstitute): `CivicJobs.UnitTests/Application`
- Repositories and DbContext against a real SQLite in-memory connection (not the EF InMemory provider): `CivicJobs.UnitTests/Infrastructure`
- Endpoints, status codes, auth and ownership: the two `ApiTests` projects, each with its own factory using a SQLite in-memory database

Test names: `Method_State_ExpectedResult`, for example `Publish_WhenClosingDateIsInThePast_ThrowsDomainException`.
Tag each test with the PRD id it covers: `[Trait("Requirement", "FR-08")]` or `[Trait("Rule", "BR-05")]`, so `dotnet test --filter Rule=BR-05` runs the tests for one rule.
One behavior per test. Don't write tests that only check a mock was called when a real assertion on the result is possible.
Group related tests (or members) with `#region Name` / `#endregion`, not section comments like `// Register`. Comments are only for explaining why the code does something.

## Commands

```
cd backend && dotnet build
cd backend && dotnet test
cd backend && dotnet run --project CivicJobs.Identity.Api
cd backend && dotnet run --project CivicJobs.Jobs.Api
cd backend && dotnet ef migrations add <Name> --project CivicJobs.Infrastructure --startup-project CivicJobs.Jobs.Api --output-dir Persistence/Migrations
cd frontend && npm start
cd frontend && npm test
docker compose up --build        # whole app: frontend :4200, Identity API :5101, Jobs API :5102
docker compose down -v           # stop and delete the SQLite volume (fresh seed on next up)
```

## Frontend rules

```
frontend/src/app/
  core/           auth service, token storage, functional HTTP interceptor, auth guard, models
  features/auth/            login and register pages
  features/job-postings/    list (filter by status), form (create/edit), detail (publish, close, delete), job-postings.service.ts
  features/open-positions/  public list of open postings for candidates (anonymous, FR-11)
  shared/         small reusable components (status badge, confirm dialog)
```

- `proxy.conf.json` maps `/identity` to http://localhost:5101 and `/jobs` to http://localhost:5102. The app calls relative URLs, so no CORS setup is needed in dev.
- Services own API calls; components own UI state with signals. No API calls inside templates.
- Show Publish, Close and Delete only when the status allows them. The API still enforces the rules.
- Show API validation errors next to the form fields.
- Responsive layout (works at 375 px wide).
- Zero errors and zero warnings in the browser console. Check before saying a task is done.
- A few focused tests: auth service, interceptor and guard.

## Don't

- Don't add packages, projects or files I didn't ask for. The repository must hold only what the exercise needs.
- Don't create symbolic links.
- Don't change the API contract in `docs/user-story.md` without asking me.
- Don't commit generated folders or files (`bin/`, `obj/`, `node_modules/`, `.angular/`, `*.db`).
