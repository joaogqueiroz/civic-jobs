# CivicJobs: user story, domain and API

The problem, scope and requirement ids (FR-xx, BR-xx) are in [PRD.md](PRD.md). This file has the details needed to build it.

## User story

**As an** HR recruiter at a city agency,
**I want to** create, publish and close job postings for my department,
**so that** candidates can find open public sector positions and I can keep track of the postings I manage.

### Acceptance criteria

| # | Criterion | PRD ids |
|---|---|---|
| 1 | I can register an account and log in with my email and password | FR-01, FR-02, BR-01, BR-02 |
| 2 | After logging in, I see only the job postings I created | FR-05, BR-08 |
| 3 | I can create a posting as a **Draft**, edit it, and **publish** it (status **Open**) | FR-04, FR-07, FR-08 |
| 4 | A posting can only be published if its closing date is after today | BR-05 |
| 5 | The salary range must be valid (minimum greater than zero, maximum greater than or equal to minimum) | BR-04 |
| 6 | I can **close** an Open posting. A Closed posting can't be edited or reopened | FR-09, BR-06 |
| 7 | I can delete only **Draft** postings. Published postings are kept for public records | FR-10, BR-07 |
| 8 | I can't see, edit or delete postings created by another recruiter | BR-08 |
| 9 | Anyone, without logging in, can list Open postings that close today or later | FR-11 |

## Domain model

### JobPosting

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| Title | string | Required, 3 to 150 chars |
| Department | string | Required, up to 100 chars |
| Description | string | Required, up to 4,000 chars |
| Location | string | Required, up to 100 chars |
| EmploymentType | enum | FullTime, PartTime, Temporary; any other value is rejected |
| SalaryMin | decimal | > 0 |
| SalaryMax | decimal | >= SalaryMin |
| ClosingDate | DateOnly | Not in the past on create (today is allowed); must be after today to publish |
| Status | enum | Draft, Open, Closed |
| CreatedByUserId | Guid | Owner, set from the token, never from the request body |
| CreatedAt / UpdatedAt | DateTimeOffset | UTC, from `TimeProvider` |

### Status transitions

```
Draft --publish--> Open --close--> Closed
```

- Edit: allowed in Draft and Open. Closed is read only.
- Editing an Open posting keeps the publish rules (closing date must stay after today).
- Delete: Draft only.
- Any other transition returns **409 Conflict**.
- Order of checks on edit (PUT): **404** if the posting doesn't exist or belongs to another user, then **409** if it's Closed, then **400** for invalid input (including a closing date in the past), then **409** if an Open posting's closing date isn't after today (BR-05).

### User

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| Email | string | Required, valid format, unique, stored lowercase |
| FullName | string | Required, up to 100 chars |
| PasswordHash | string | Hashed with `PasswordHasher<T>`, never returned by the API |
| CreatedAt | DateTimeOffset | UTC |

Password: 8 to 128 chars, with a letter and a number. The upper limit caps the cost of hashing.

## APIs

Both APIs use the same JWT settings (issuer, audience, signing key), so a token issued by the Identity API is accepted by the Jobs API.

### Identity API (second API), http://localhost:5101

| Verb | Route | Auth | Success | Errors |
|---|---|---|---|---|
| POST | /api/auth/register | Anonymous | 201 + user (no hash) | 400 validation, 409 email in use |
| POST | /api/auth/login | Anonymous | 200 + `{ accessToken, expiresAt }` | 400, 401 invalid credentials |
| GET | /api/users/me | Bearer | 200 + current user | 401 |

### Jobs API, http://localhost:5102

| Verb | Route | Auth | Success | Errors |
|---|---|---|---|---|
| GET | /api/job-postings?status= | Bearer | 200 + my postings | 401 |
| GET | /api/job-postings/{id} | Bearer | 200 | 401, 404 |
| POST | /api/job-postings | Bearer | 201 + Location header | 400, 401 |
| PUT | /api/job-postings/{id} | Bearer | 200 | 400, 401, 404, 409 |
| DELETE | /api/job-postings/{id} | Bearer | 204 | 401, 404, 409 |
| POST | /api/job-postings/{id}/publish | Bearer | 200 | 401, 404, 409 |
| POST | /api/job-postings/{id}/close | Bearer | 200 | 401, 404, 409 |
| GET | /api/public/job-postings | Anonymous | 200 + Open postings closing today or later, soonest first (no owner, status or timestamps) | |

A posting owned by another user returns **404**, not 403, so the API doesn't reveal that the id exists.

Errors use `ProblemDetails` / `ValidationProblemDetails`.

## Frontend

| Page | Route | Auth |
|---|---|---|
| Login | /login | Anonymous |
| Register | /register | Anonymous |
| My job postings (filter by status) | /job-postings | Logged in |
| New posting | /job-postings/new | Logged in |
| Posting detail (publish, close, delete) | /job-postings/:id | Logged in |
| Edit posting | /job-postings/:id/edit | Logged in |
| Open positions (public list for candidates, FR-11) | /open-positions | Anonymous |

## Seed data

- `recruiter@cityofexample.gov` / `Demo@12345` (Parks & Recreation), with 5 postings: 2 Draft, 2 Open, 1 Closed
- `recruiter2@cityofexample.gov` / `Demo@12345` (Public Works), with 2 postings, used to show ownership rules

Assumptions (second API, shared SQLite database, token storage) are in [PRD.md, section 10](PRD.md#10-assumptions-and-constraints).
