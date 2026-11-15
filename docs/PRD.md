# CivicJobs: Product Requirements Document

| | |
|---|---|
| Product | CivicJobs, job posting management for public sector HR |
| Version | 1.1 (agencies and roles, approval, applications, technical exercise) |
| Author | Joao de Queiroz |
| Related | [user-story.md](user-story.md): domain model, status rules and API contract |

## 1. Problem

Public sector hiring is slow and is losing candidates.

- In 2019 the average public sector time to hire was **119 days**, against **36 days** in the private sector. Local governments took **130 days** ([NEOGOV Time to Hire Report 2020](https://blog.neogov.com/press/neogov-releases-time-to-hire-report-2020)).
- Applications per public sector job fell **35%** from 2019 to 2021 and another **56%** from 2021 to 2022, and **79%** of HR officials surveyed in 2022 said they weren't finding enough applicants ([Route Fifty, June 2022](https://www.route-fifty.com/workforce/2022/06/what-job-applicants-say-about-government-hiring-experience/368721/)).

Part of the problem starts with the job posting itself. When postings are managed without clear rules, candidates find positions that already closed, salary ranges that don't make sense, and incomplete descriptions. Agencies also need to keep a record of what they published, and several recruiters from different departments work on the same system.

## 2. Solution

CivicJobs is the first module of a hiring system: it manages the **life cycle of a job posting**, from draft to closed, with business rules that keep postings valid and accountable. Version 1.1 adds the people around the posting: agencies with departments and roles, an approval step before a posting goes public, and candidates who apply and follow their applications.

## 3. Goals and non-goals

**Goals (v1.1)**
- A recruiter can create, edit and submit job postings in a few minutes.
- A hiring manager approves every posting of their department before candidates see it.
- Candidates only see postings that are valid and open, apply to them and follow each application.
- Every published posting is kept as a record, and postings close by themselves when the closing date passes.
- Each recruiter manages only their own postings; each agency manages only its own departments and staff.

**Non-goals (v1.1)**
- Screening, interviews and offers beyond the application status
- Creating agencies from the app (agencies are seeded)
- Sending real email (notifications are written to the log)
- File attachments (resumes)

## 4. Users

| Persona | Need | Access |
|---|---|---|
| **Admin** of a city agency | Set up the departments and the staff accounts of the agency | Logged in, role `Admin` |
| **HR recruiter** at a department | Write and manage the department's open positions quickly and correctly | Logged in, role `Recruiter` |
| **Hiring manager** of a department | Approve the department's postings and decide on applications | Logged in, role `HiringManager` |
| **Candidate** | See which positions are open now, apply and follow the applications | Anonymous to browse, role `Candidate` to apply |

## 5. User stories

**As an** HR recruiter at a city agency, **I want to** create, submit and close job postings for my department, **so that** candidates can find open public sector positions and I can keep track of the postings I manage.

**As a** hiring manager, **I want to** approve postings and review applications for my department, **so that** only correct postings go public and candidates get an answer.

**As a** candidate, **I want to** apply to an open position and see the status of my applications, **so that** I know where I stand.

**As an** agency admin, **I want to** manage departments and staff roles, **so that** each person can do only their part.

## 6. Functional requirements

Each requirement maps to tests, so the test suite shows which rules are covered.

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | A candidate can register with full name, email and password | Must |
| FR-02 | A user can log in and receive an access token | Must |
| FR-03 | A logged-in user can see their own profile, with role, agency and department | Must |
| FR-04 | A recruiter can create a job posting as Draft, for their own department | Must |
| FR-05 | A recruiter can list their postings and filter by status | Must |
| FR-06 | A recruiter can view one of their postings | Must |
| FR-07 | A recruiter can edit a Draft or Open posting | Must |
| FR-08 | A recruiter can submit a Draft posting for approval | Must |
| FR-09 | A recruiter can close an Open posting | Must |
| FR-10 | A recruiter can delete a Draft posting | Must |
| FR-11 | Anyone can list Open postings without logging in (only postings closing today or later) | Could |
| FR-12 | An admin can list and create the departments of their agency | Must |
| FR-13 | An admin can list the staff of their agency and create staff accounts with a role and a department | Must |
| FR-14 | An admin can change the role and department of a staff account of their agency | Should |
| FR-15 | A hiring manager can list the postings of their department (except drafts) and filter by status | Must |
| FR-16 | A hiring manager can approve a posting awaiting approval, which opens it | Must |
| FR-17 | A hiring manager can reject a posting awaiting approval with a reason, which returns it to Draft | Must |
| FR-18 | A candidate can apply to an Open posting with an optional cover letter | Must |
| FR-19 | A candidate can list their applications with the posting and the status | Must |
| FR-20 | A hiring manager can list the applications to a posting of their department | Must |
| FR-21 | A hiring manager can move an application to Under review, Rejected or Hired | Must |
| FR-22 | Open postings are closed automatically once their closing date has passed | Should |
| FR-23 | The candidate gets an email when they apply and every time their application changes status | Should |
| FR-24 | A user can log out, which removes the session cookie | Must |

## 7. Business rules

| ID | Rule | Error |
|---|---|---|
| BR-01 | Email is unique and stored in lowercase | 409 |
| BR-02 | Password has 8 to 128 chars, with a letter and a number | 400 |
| BR-03 | Title, description and location are required, within their max length | 400 |
| BR-04 | SalaryMin is greater than zero and SalaryMax is greater than or equal to SalaryMin | 400 |
| BR-05 | A posting can be submitted or approved only if its closing date is after today | 409 |
| BR-06 | Status moves only Draft -> PendingApproval -> Open -> Closed; a rejection returns PendingApproval to Draft; PendingApproval and Closed postings can't be edited, and a Closed posting can't be reopened | 409 |
| BR-07 | Only Draft postings can be deleted; published postings are kept as records | 409 |
| BR-08 | A recruiter can only access their own postings; others return 404 | 404 |
| BR-09 | A posting can't be created with a closing date in the past (today is allowed) | 400 |
| BR-10 | Employment type must be FullTime, PartTime or Temporary | 400 |
| BR-11 | An Open posting whose closing date is before today is closed automatically | |
| BR-12 | Each endpoint is limited to its role; a user with another role gets 403 | 403 |
| BR-13 | Recruiters and hiring managers belong to one department of their agency, admins belong to the agency only, candidates belong to neither | 400 |
| BR-14 | A department name is required, up to 100 chars, and unique within its agency | 400, 409 |
| BR-15 | An admin only sees and changes the departments and staff of their own agency; others return 404 | 404 |
| BR-16 | A hiring manager only sees the postings and applications of their own department; others return 404 | 404 |
| BR-17 | A rejection needs a reason of up to 500 chars | 400 |
| BR-18 | A candidate can apply only to an Open posting that closes today or later, and only once per posting | 404, 409 |
| BR-19 | Application status moves only Submitted -> UnderReview -> Hired or Rejected, or Submitted -> Rejected; Hired and Rejected are final | 409 |
| BR-20 | A cover letter is optional, up to 2,000 chars | 400 |
| BR-21 | The access token travels in an httpOnly, Secure, SameSite=Strict cookie; the APIs also accept a Bearer header for API clients | 401 |
| BR-22 | An admin can't change their own role, so an agency always keeps the admin who is making changes | 409 |

## 8. Non-functional requirements

| Area | Requirement |
|---|---|
| Architecture | Clean Architecture: business layer independent of the data layer and the APIs |
| Quality | TDD; unit tests for Core, Application and Infrastructure, endpoint tests for both APIs; CI runs on every push |
| Security | Passwords hashed, JWT in an httpOnly cookie, role-based authorization, owner taken from the token, no secrets in code |
| API | REST verbs and status codes, `ProblemDetails` errors, Swagger documentation |
| Frontend | Responsive down to 375 px, clear validation messages, no browser console warnings |
| Setup | Runs locally with no database install (SQLite) and seeded demo data, or with one command using Docker (`docker compose up --build`) |

## 9. Success metrics

Targets for a pilot, not measured in this exercise.

| Metric | Target |
|---|---|
| Time for a recruiter to submit a new posting | Under 5 minutes |
| Time from submission to approval | Under 2 working days |
| Open postings with a closing date in the past | 0 |
| Published postings lost by deletion | 0 |
| Applications without a decision 30 days after the posting closes | 0 |

## 10. Assumptions and constraints

- "A second API" in the exercise is read as a separate Identity API (register, login, profile, departments and staff). The Jobs API accepts the token issued by the Identity API.
- Both APIs share one SQLite database.
- Public registration creates candidates only. Staff accounts (admin, recruiter, hiring manager) are created by an agency admin, so nobody can give themselves a staff role. Agencies and their first admin are seeded.
- The role is a claim in the token, so a role change applies at the next login (tokens last 60 minutes).
- The token is stored in an httpOnly cookie, so scripts in the page can't read it. The frontend and both APIs share one origin through the dev proxy or nginx, and SameSite=Strict stops other sites from sending the cookie (CSRF).
- "Today" in date rules (BR-05, BR-09, BR-11, BR-18) is the current date in UTC. A multi-time-zone deployment would use the agency's local time zone.
- The automatic close (BR-11) runs in the Jobs API every hour, which is enough for a date-based rule. Several API instances would move it to a single worker.
- Notifications are written to the log through an `IEmailSender` abstraction; a real SMTP or email service only needs a new implementation.

## 11. Future work

1. Agency time zone for the date rules
2. Resume upload and interview scheduling
3. Notify candidates when a posting they applied to closes
4. Audit trail of approvals and status changes
