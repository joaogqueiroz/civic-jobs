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

Needs the .NET 10 SDK and Node.js 22 or later. Run each command in its own terminal:

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
