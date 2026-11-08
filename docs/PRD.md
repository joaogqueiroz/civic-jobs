# CivicJobs: Product Requirements Document

| | |
|---|---|
| Product | CivicJobs, job posting management for public sector HR |
| Version | 1.0 (MVP, technical exercise) |
| Author | Joao de Queiroz |
| Related | [user-story.md](user-story.md): domain model, status rules and API contract |

## 1. Problem

Public sector hiring is slow and is losing candidates.

- In 2019 the average public sector time to hire was **119 days**, against **36 days** in the private sector. Local governments took **130 days** ([NEOGOV Time to Hire Report 2020](https://blog.neogov.com/press/neogov-releases-time-to-hire-report-2020)).
- Applications per public sector job fell **35%** from 2019 to 2021 and another **56%** from 2021 to 2022, and **79%** of HR officials surveyed in 2022 said they weren't finding enough applicants ([Route Fifty, June 2022](https://www.route-fifty.com/workforce/2022/06/what-job-applicants-say-about-government-hiring-experience/368721/)).

Part of the problem starts with the job posting itself. When postings are managed without clear rules, candidates find positions that already closed, salary ranges that don't make sense, and incomplete descriptions. Agencies also need to keep a record of what they published, and several recruiters from different departments work on the same system.

## 2. Solution

CivicJobs is the first module of a hiring system: it manages the **life cycle of a job posting**, from draft to closed, with business rules that keep postings valid and accountable.

## 3. Goals and non-goals

**Goals (v1.0)**
- A recruiter can create, edit, publish and close job postings in a few minutes.
- Candidates only see postings that are valid and open.
- Every published posting is kept as a record.
- Each recruiter manages only their own postings.

**Non-goals (v1.0)**
- Candidate applications, screening and interviews
- Roles and permissions beyond "recruiter" (admin, hiring manager)
- Approval workflows, email notifications, file attachments
- Multiple agencies (multi-tenancy)

## 4. Users

| Persona | Need | Access |
|---|---|---|
| **HR recruiter** at a city agency department | Publish and manage the department's open positions quickly and correctly | Logged in |
| **Candidate** (optional in v1.0) | See which positions are open now | Anonymous |

## 5. User story

**As an** HR recruiter at a city agency, **I want to** create, publish and close job postings for my department, **so that** candidates can find open public sector positions and I can keep track of the postings I manage.

## 6. Functional requirements

Each requirement maps to tests, so the test suite shows which rules are covered.

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | A user can register with full name, email and password | Must |
| FR-02 | A user can log in and receive an access token | Must |
| FR-03 | A logged-in user can see their own profile | Must |
| FR-04 | A recruiter can create a job posting as Draft | Must |
| FR-05 | A recruiter can list their postings and filter by status | Must |
| FR-06 | A recruiter can view one of their postings | Must |
| FR-07 | A recruiter can edit a Draft or Open posting | Must |
| FR-08 | A recruiter can publish a Draft posting | Must |
| FR-09 | A recruiter can close an Open posting | Must |
| FR-10 | A recruiter can delete a Draft posting | Must |
| FR-11 | Anyone can list Open postings without logging in | Could |

## 7. Business rules

| ID | Rule | Error |
|---|---|---|
| BR-01 | Email is unique and stored in lowercase | 409 |
| BR-02 | Password has at least 8 chars, with a letter and a number | 400 |
| BR-03 | Title, department, description and location are required, within their max length | 400 |
| BR-04 | SalaryMin is greater than zero and SalaryMax is greater than or equal to SalaryMin | 400 |
| BR-05 | A posting can be published only if its closing date is after today | 409 |
| BR-06 | Status moves only Draft -> Open -> Closed; a Closed posting can't be edited or reopened | 409 |
| BR-07 | Only Draft postings can be deleted; published postings are kept as records | 409 |
| BR-08 | A recruiter can only access their own postings; others return 404 | 404 |

## 8. Non-functional requirements

| Area | Requirement |
|---|---|
| Architecture | Clean Architecture: business layer independent of the data layer and the APIs |
| Quality | TDD; unit tests for Core, Application and Infrastructure, endpoint tests for both APIs; CI runs on every push |
| Security | Passwords hashed, JWT bearer auth, owner taken from the token, no secrets in code |
| API | REST verbs and status codes, `ProblemDetails` errors, Swagger documentation |
| Frontend | Responsive down to 375 px, clear validation messages, no browser console warnings |
| Setup | Runs locally with no database install (SQLite) and seeded demo data |

## 9. Success metrics

Targets for a pilot, not measured in this exercise.

| Metric | Target |
|---|---|
| Time for a recruiter to publish a new posting | Under 5 minutes |
| Open postings with a closing date in the past | 0 |
| Published postings lost by deletion | 0 |

## 10. Assumptions and constraints

- "A second API" in the exercise is read as a separate Identity API (register, login, authorized and anonymous endpoints). The Jobs API is fully protected and accepts the token issued by the Identity API.
- Both APIs share one SQLite database with the `Users` and `JobPostings` tables.
- The token is stored in `localStorage` for the demo; production would use an httpOnly cookie.

## 11. Future work

1. Candidate applications and application status
2. Agencies and departments as entities, with roles (admin, recruiter, hiring manager)
3. Automatic closing of postings when the closing date passes (background job)
4. Approval step before publishing
5. Email notifications to candidates
