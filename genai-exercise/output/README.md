# Task Manager API

A RESTful task management API built with .NET 10, ASP.NET Core controllers, EF Core with SQLite, and JWT bearer auth, organized in clean architecture layers.

```
src/
  TaskManager.Core            Entities, enum, domain rules, repository interface (no dependencies)
  TaskManager.Application     DTOs, TaskService, NotFoundException          (→ Core)
  TaskManager.Infrastructure  EF Core DbContext, mappings, TaskRepository     (→ Core)
  TaskManager.Api             Controllers, JWT, ProblemDetails, Swagger, /dev/token
tests/
  TaskManager.UnitTests       Entity rules + TaskService (fake repository, FakeTimeProvider)
  TaskManager.Api.Tests       Endpoint tests with WebApplicationFactory
```

## Prerequisites

- .NET 10 SDK

## Run

The JWT signing key is not stored in the repo, so set it once with user-secrets. Use any random string of 32 or more bytes:

```bash
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-characters>" --project src/TaskManager.Api
dotnet run --project src/TaskManager.Api --launch-profile http
```

Open http://localhost:5124/swagger. The SQLite database (`tasks.db`) is created on first start.

Outside Development, supply the settings through environment variables, for example `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience` and `ConnectionStrings__TaskDb`. If the key is missing or too short, the app refuses to start.

## Try it in Swagger

1. Call `POST /dev/token` with `{ "userId": "<any guid>" }` and copy `accessToken`.
2. Click **Authorize** and paste the token without the `Bearer ` prefix.
3. Call the `/api/tasks` endpoints. Use a different user id to see that one user's tasks return 404 for another user.

`/dev/token` and Swagger are only registered in the Development environment. In any other environment the route does not exist (404).

## Endpoints

All `/api/tasks` endpoints require a valid JWT. The owner is always taken from the `sub` claim.

| Method | Route                       | Success                 |
|--------|-----------------------------|-------------------------|
| GET    | `/api/tasks?status=Doing`   | 200, the caller's tasks |
| GET    | `/api/tasks/{id}`           | 200                     |
| POST   | `/api/tasks`                | 201 + `Location`        |
| PUT    | `/api/tasks/{id}`           | 200                     |
| DELETE | `/api/tasks/{id}`           | 204                     |

Errors: 400 returns `ValidationProblemDetails`. 401, 403, 404 and 500 return `ProblemDetails`. Another user's task returns 404.

## Rules (enforced in `TaskItem`)

- Title is required, cannot be whitespace, and must be 3–150 characters after trimming.
- DueDate must be after today (UTC, taken from `TimeProvider`) on create, and on update only when the date changes.
- New tasks start as `Todo`. A `Done` task cannot be moved to another status.

## Test

```bash
dotnet test
```

## Notes

- The schema is created with `EnsureCreated` to keep this exercise simple. A production system should use EF Core migrations.
- `User` exists in Core and has a table, but tasks have no foreign key to it, because users come from the token and don't need to be registered first.
