![CI](https://github.com/joaogqueiroz/civic-jobs/actions/workflows/ci.yml/badge.svg)

# CivicJobs

CivicJobs is a job posting manager for public sector HR recruiters: create, publish and close job postings for a department. It has an Identity API (register, login), a Jobs API and an Angular frontend, sharing one SQLite database seeded with demo data.

Requirements and the API contract: [docs/PRD.md](docs/PRD.md) and [docs/user-story.md](docs/user-story.md).

## Run with Docker

Needs Docker with Compose. From the repository root:

```
docker compose up --build
```

| What | URL |
|---|---|
| App | http://localhost:4200 |
| Identity API (Swagger) | http://localhost:5101/swagger |
| Jobs API (Swagger) | http://localhost:5102/swagger |

Log in with `recruiter@cityofexample.gov` / `Demo@12345` (5 postings) or `recruiter2@cityofexample.gov` / `Demo@12345` (2 postings).

Candidates can browse open positions without logging in at http://localhost:4200/open-positions.

The database lives in the `civicjobs-data` volume and is seeded on the first start. To start over with fresh demo data:

```
docker compose down -v
docker compose up --build
```

## Run locally

Needs the .NET 10 SDK and Node.js 22.22 or later, or 24.15 or later (required by Angular 22). Run each command in its own terminal:

```
cd backend && dotnet run --project CivicJobs.Identity.Api   # http://localhost:5101/swagger
cd backend && dotnet run --project CivicJobs.Jobs.Api       # http://localhost:5102/swagger
cd frontend && npm install && npm start                     # http://localhost:4200
```

Both APIs migrate and seed `backend/civicjobs.db` on startup in Development. Delete that file to start over.

## Tests

```
cd backend && dotnet test
cd frontend && npm test
```

## Architecture

Clean Architecture with plain services (no CQRS or MediatR). Arrows point to what each part depends on.

```
Angular frontend
      │  HTTP: /identity → Identity API, /jobs → Jobs API
      ▼
┌────────────────┬────────────────┐
│  Identity API  │    Jobs API    │   controllers, JWT auth, Swagger, error handling
└────────────────┴────────────────┘
         │                 │
         ▼                 ▼
  Infrastructure ──────► Application ──────► Core
  EF Core, SQLite,       services,           entities, business rules,
  JWT, hashing           validators          repository interfaces
```

- **Dependency rule:** dependencies point inward.
  - Both APIs reference Application and Infrastructure.
  - Infrastructure references Application and Core.
  - Application references only Core, never Infrastructure.
  - Core references nothing.

  Business rules live in Core entities or Application services, never in controllers or repositories.
- **Shared between the APIs:** both read the same `Jwt` settings (issuer, audience, key), so a token from the Identity API works in the Jobs API, and both use the same SQLite database file.

## Project structure

```
backend/
  CivicJobs.Core/               entities, enums, domain exceptions, repository interfaces
  CivicJobs.Application/        services, validators, input and view models, abstractions
  CivicJobs.Infrastructure/     EF Core + SQLite, migrations, seed, JWT, password hashing, shared web setup
  CivicJobs.Identity.Api/       register, login, current user
  CivicJobs.Jobs.Api/           job postings (recruiters) and the public list (candidates)
  CivicJobs.UnitTests/          Core, Application and Infrastructure tests
  CivicJobs.*.ApiTests/         endpoint tests for each API
frontend/                       Angular app (core, features, shared)
docs/                           PRD and user story
genai-exercise/                 GenAI part of the exercise
docker-compose.yml
```

## Key decisions and assumptions

- **A second API, read as a separate Identity API** for register, login and the current user. The Jobs API accepts its tokens. See [PRD section 10](docs/PRD.md#10-assumptions-and-constraints).
- **Another recruiter's posting returns 404**, not 403, so the API doesn't reveal that the id exists (BR-08). See [PRD section 7](docs/PRD.md#7-business-rules).
- **Validation in two places:**
  - the validator returns 400 with the error next to each form field
  - the entity guards its own rules, so invalid data can't be saved from anywhere else

  See [PRD section 7](docs/PRD.md#7-business-rules).
- **Typed exceptions, mapped in one place:**
  - 400: invalid data
  - 409: an action the status doesn't allow, or an email already in use
  - 404: not found, or not yours
  - 401: wrong credentials, or an expired session

  See [PRD section 7](docs/PRD.md#7-business-rules).
- **`TimeProvider` for date rules** (closing date, publishing), so the tests can fix "today". "Today" means the UTC date. See [PRD section 10](docs/PRD.md#10-assumptions-and-constraints).
- **SQLite for zero setup:** no database to install, and demo data is seeded on start. See [PRD section 8](docs/PRD.md#8-non-functional-requirements).
- **Token in `localStorage` for the demo.** Production would use an httpOnly cookie. See [PRD section 10](docs/PRD.md#10-assumptions-and-constraints).

## How AI was used

I used Claude Code as my coding assistant, with guardrails I set up for this project:
- **[CLAUDE.md](CLAUDE.md)** holds the rules I defined: architecture, tests and conventions.
- **The `tdd-cycle` skill** follows the workflow I chose: failing tests first, then the code.
- **The `architecture-reviewer` agent** checked each block for layer violations, ownership and weak tests before I merged it.
- **The `frontend-design` plugin** helped with the UI pass, following the visual direction I picked.

I wrote the requirements (PRD and user story), made the design decisions and reviewed the code at the end of each block. The commit history shows each step. The GenAI part of the exercise (prompt, raw output and my review) is in [genai-exercise/](genai-exercise/).
