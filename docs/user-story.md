# CivicJobs: user stories, domain and API

The problem, scope and requirement ids (FR-xx, BR-xx) are in [PRD.md](PRD.md). This file has the details needed to build it.

## User stories

**As an** HR recruiter at a city agency,
**I want to** create, submit and close job postings for my department,
**so that** candidates can find open public sector positions and I can keep track of the postings I manage.

**As a** hiring manager, **I want to** approve postings and review applications for my department, **so that** only correct postings go public and candidates get an answer.

**As a** candidate, **I want to** apply to an open position and see the status of my applications, **so that** I know where I stand.

**As an** agency admin, **I want to** manage departments and staff roles, **so that** each person can do only their part.

### Acceptance criteria

| # | Criterion | PRD ids |
|---|---|---|
| 1 | A candidate can register an account, and anyone can log in with email and password | FR-01, FR-02, BR-01, BR-02 |
| 2 | After logging in, a recruiter sees only the job postings they created | FR-05, BR-08 |
| 3 | A recruiter can create a posting as a **Draft** for their department, edit it, and **submit** it for approval (status **PendingApproval**) | FR-04, FR-07, FR-08 |
| 4 | A posting can only be submitted or approved if its closing date is after today | BR-05 |
| 5 | The salary range must be valid (minimum greater than zero, maximum greater than or equal to minimum) | BR-04 |
| 6 | The hiring manager of the department **approves** a posting (status **Open**) or **rejects** it with a reason (back to **Draft**) | FR-16, FR-17, BR-17 |
| 7 | A recruiter can **close** an Open posting, and Open postings close by themselves after the closing date. A Closed posting can't be edited or reopened | FR-09, FR-22, BR-06, BR-11 |
| 8 | A recruiter can delete only **Draft** postings. Published postings are kept for public records | FR-10, BR-07 |
| 9 | Nobody sees, edits or deletes postings outside their reach: another recruiter's (recruiter) or another department's (hiring manager) | BR-08, BR-16 |
| 10 | Anyone, without logging in, can list Open postings that close today or later | FR-11 |
| 11 | A candidate can apply once to an Open posting and follow the status of each application | FR-18, FR-19, BR-18 |
| 12 | The hiring manager moves applications to Under review, then Hired or Rejected, and the candidate gets an email each time | FR-20, FR-21, FR-23, BR-19 |
| 13 | An admin creates departments and staff accounts for their agency and changes staff roles | FR-12, FR-13, FR-14, BR-13, BR-14, BR-15 |

## Roles

| Role | Belongs to | Can |
|---|---|---|
| Admin | Agency | Manage the agency's departments and staff accounts |
| Recruiter | Department | Manage their own postings |
| HiringManager | Department | Approve or reject the department's postings, decide on their applications |
| Candidate | Nobody | Apply to open postings, follow their applications |

Public registration creates a Candidate. Staff accounts are created by an admin of the agency. A user calling an endpoint of another role gets **403**.

## Domain model

### Agency

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| Name | string | Required, up to 150 chars. Seeded, not created from the app |
| CreatedAt | DateTimeOffset | UTC |

### Department

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| AgencyId | Guid | Required |
| Name | string | Required, up to 100 chars, unique within the agency (case-insensitive) |
| CreatedAt | DateTimeOffset | UTC |

### User

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| Email | string | Required, valid format, unique, stored lowercase |
| FullName | string | Required, up to 100 chars |
| PasswordHash | string | Hashed with `PasswordHasher<T>`, never returned by the API |
| Role | enum | Admin, Recruiter, HiringManager, Candidate |
| AgencyId | Guid? | Required for Admin, Recruiter and HiringManager; empty for Candidate |
| DepartmentId | Guid? | Required for Recruiter and HiringManager; empty for Admin and Candidate |
| CreatedAt | DateTimeOffset | UTC |

Password: 8 to 128 chars, with a letter and a number. The upper limit caps the cost of hashing.

### JobPosting

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| Title | string | Required, 3 to 150 chars |
| DepartmentId | Guid | The recruiter's department, set from the recruiter's account, never from the request body |
| Department | string | The department name when the posting was created, kept as part of the record |
| Description | string | Required, up to 4,000 chars |
| Location | string | Required, up to 100 chars |
| EmploymentType | enum | FullTime, PartTime, Temporary; any other value is rejected |
| SalaryMin | decimal | > 0 |
| SalaryMax | decimal | >= SalaryMin |
| ClosingDate | DateOnly | Not in the past on create (today is allowed); must be after today to submit or approve |
| Status | enum | Draft, PendingApproval, Open, Closed |
| RejectionReason | string? | Set when the hiring manager rejects, up to 500 chars; cleared when the posting is submitted again |
| CreatedByUserId | Guid | Owner, set from the token, never from the request body |
| CreatedAt / UpdatedAt | DateTimeOffset | UTC, from `TimeProvider` |

### JobApplication

| Field | Type | Rules |
|---|---|---|
| Id | Guid | Primary key |
| JobPostingId | Guid | An Open posting that closes today or later when the candidate applies |
| CandidateId | Guid | Set from the token |
| CoverLetter | string? | Optional, up to 2,000 chars |
| Status | enum | Submitted, UnderReview, Rejected, Hired |
| CreatedAt / UpdatedAt | DateTimeOffset | UTC |

One application per candidate and posting (unique index).

### Posting status transitions

```
Draft --submit--> PendingApproval --approve--> Open --close (recruiter, or automatic after the closing date)--> Closed
  ^                      |
  +------reject----------+
```

- Edit: allowed in Draft and Open. PendingApproval and Closed are read only.
- Editing an Open posting keeps the publish rules (closing date must stay after today).
- Delete: Draft only.
- Submit and approve both check that the closing date is after today (BR-05).
- Every hour the Jobs API closes the Open postings whose closing date is before today (BR-11).
- Any other transition returns **409 Conflict**.
- Order of checks on edit (PUT): **404** if the posting doesn't exist or belongs to another user, then **409** if it's PendingApproval or Closed, then **400** for invalid input (including a closing date in the past), then **409** if an Open posting's closing date isn't after today (BR-05).

### Application status transitions

```
Submitted --review--> UnderReview --hire--> Hired
    |                      |
    +-----reject-----------+---reject--> Rejected
```

Hired and Rejected are final. Any other transition returns **409 Conflict**.

### Notifications

Sent to the candidate through `IEmailSender` (written to the log in this version, FR-23):

| When | Subject |
|---|---|
| The candidate applies | We received your application for {title} |
| The application changes status | Your application for {title}: {Under review, Rejected or Hired} |

## APIs

Both APIs use the same JWT settings (issuer, audience, signing key), so a token issued by the Identity API is accepted by the Jobs API. Login sets the token in the `civicjobs_session` cookie (httpOnly, Secure, SameSite=Strict, path `/`); both APIs read it from that cookie or from an `Authorization: Bearer` header (BR-21).

### Identity API (second API), http://localhost:5101

| Verb | Route | Role | Success | Errors |
|---|---|---|---|---|
| POST | /api/auth/register | Anonymous | 201 + candidate user (no hash) | 400 validation, 409 email in use |
| POST | /api/auth/login | Anonymous | 200 + `{ accessToken, expiresAt }` and the session cookie | 400, 401 invalid credentials |
| POST | /api/auth/logout | Anonymous | 204 and an expired session cookie | |
| GET | /api/users/me | Any | 200 + current user with role, agency and department names | 401 |
| GET | /api/departments | Admin | 200 + departments of my agency, by name | 401, 403 |
| POST | /api/departments | Admin | 201 + department | 400, 401, 403, 409 name in use |
| GET | /api/users | Admin | 200 + staff of my agency, by name | 401, 403 |
| POST | /api/users | Admin | 201 + staff user `{ fullName, email, password, role, departmentId }` | 400, 401, 403, 404 department, 409 email in use |
| PUT | /api/users/{id}/role | Admin | 200 + staff user `{ role, departmentId }` | 400, 401, 403, 404, 409 own role |

### Jobs API, http://localhost:5102

Recruiter:

| Verb | Route | Success | Errors |
|---|---|---|---|
| GET | /api/job-postings?status= | 200 + my postings | 401, 403 |
| GET | /api/job-postings/{id} | 200 | 401, 403, 404 |
| POST | /api/job-postings | 201 + Location header | 400, 401, 403 |
| PUT | /api/job-postings/{id} | 200 | 400, 401, 403, 404, 409 |
| DELETE | /api/job-postings/{id} | 204 | 401, 403, 404, 409 |
| POST | /api/job-postings/{id}/submit | 200 | 401, 403, 404, 409 |
| POST | /api/job-postings/{id}/close | 200 | 401, 403, 404, 409 |

Hiring manager (postings and applications of my department):

| Verb | Route | Success | Errors |
|---|---|---|---|
| GET | /api/department/job-postings?status= | 200 + non-draft postings, newest first | 401, 403 |
| GET | /api/department/job-postings/{id} | 200 | 401, 403, 404 |
| POST | /api/department/job-postings/{id}/approve | 200 | 401, 403, 404, 409 |
| POST | /api/department/job-postings/{id}/reject | 200, body `{ reason }` | 400, 401, 403, 404, 409 |
| GET | /api/department/job-postings/{id}/applications | 200 + applications, oldest first | 401, 403, 404 |
| PUT | /api/department/applications/{id}/status | 200, body `{ status }` | 400, 401, 403, 404, 409 |

Candidate:

| Verb | Route | Success | Errors |
|---|---|---|---|
| POST | /api/applications | 201, body `{ jobPostingId, coverLetter }` | 400, 401, 403, 404, 409 |
| GET | /api/applications | 200 + my applications with posting title, department and status, newest first | 401, 403 |

Anonymous:

| Verb | Route | Success |
|---|---|---|
| GET | /api/public/job-postings | 200 + Open postings closing today or later, soonest first (no owner, status or timestamps) |

A posting or application out of the caller's reach returns **404**, not 403, so the API doesn't reveal that the id exists. 403 is only for the wrong role.

Errors use `ProblemDetails` / `ValidationProblemDetails`.

## Frontend

| Page | Route | Role |
|---|---|---|
| Login | /login | Anonymous |
| Register (candidate) | /register | Anonymous |
| Open positions, with Apply for candidates | /open-positions | Anonymous |
| My applications | /my-applications | Candidate |
| My job postings (filter by status) | /job-postings | Recruiter |
| New posting | /job-postings/new | Recruiter |
| Posting detail (submit, close, delete) | /job-postings/:id | Recruiter |
| Edit posting | /job-postings/:id/edit | Recruiter |
| Department postings (filter by status) | /department | HiringManager |
| Department posting detail (approve, reject, applications) | /department/:id | HiringManager |
| Departments and staff | /admin | Admin |

After login each role lands on its first page: Admin on /admin, Recruiter on /job-postings, HiringManager on /department, Candidate on /open-positions.

## Seed data

All passwords are `Demo@12345`. Agency: **City of Example**, with the departments Parks & Recreation and Public Works.

| Email | Role | Department |
|---|---|---|
| admin@cityofexample.gov | Admin | |
| recruiter@cityofexample.gov | Recruiter | Parks & Recreation, 6 postings: 2 Draft, 1 PendingApproval, 2 Open, 1 Closed |
| recruiter2@cityofexample.gov | Recruiter | Public Works, 2 postings (1 Draft, 1 Open), used to show ownership rules |
| manager@cityofexample.gov | HiringManager | Parks & Recreation |
| manager2@cityofexample.gov | HiringManager | Public Works |
| candidate@example.com | Candidate | 2 applications: Recreation Program Coordinator (Submitted) and Groundskeeper (UnderReview) |

Assumptions (second API, shared SQLite database, roles in the token, cookie) are in [PRD.md, section 10](PRD.md#10-assumptions-and-constraints).
