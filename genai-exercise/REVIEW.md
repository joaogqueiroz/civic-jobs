# GenAI exercise: review of the generated API

Tool: **Claude Code**, one fresh session in an empty folder, with no CLAUDE.md or project context.
Prompt: [PROMPT.md](PROMPT.md). Generated code: [output/](output/) (as it came, without `bin/` and `obj/`).

## Result at a glance

- Four projects as asked (Core, Application, Infrastructure, Api) plus two test projects.
- `dotnet build`: 0 warnings, 0 errors.
- `dotnet test`: 47 tests pass (33 unit, 14 endpoint).
- The API boots in Development, Swagger loads, and the main flow works end to end.

The output was good on the first run, and I think the main reason is the prompt: it named the architecture rules, the security rules and the expected error formats instead of only asking for "a CRUD".

## How I validated it

1. **Build and tests.** I ran `dotnet build` and `dotnet test` myself, outside the AI session.
2. **Read it like a pull request.** Project references first (Core references nothing, Application only Core), then the entity, the service, the repository and the controller.
3. **Checked the SQL, not only the C#.** With EF Core logging on, every read and delete query filters by both id and owner:
   `WHERE "t"."Id" = @id AND "t"."UserId" = @userId`. That's the IDOR check I care most about, and it's correct.
4. **Manual tests in Swagger** with a token from `/dev/token`: no token (401), another user's task (404), invalid title and past due date (400 with both errors at once).
5. **Tests that test something.** I checked that the tests assert results and status codes, not only that a mock was called.

## What the AI got right

- Owner taken from the token's `sub` claim and never from the request body; another user's task returns 404. It sets `MapInboundClaims = false`, so `sub` is read by its real name, pins the algorithm to HS256 and keeps the clock skew at 30 seconds.
- `UpdatedAt` is set on every update, and a unit test advances the fake clock to prove it. (In the endpoint logs the UPDATE doesn't touch `UpdatedAt` only because those tests keep the clock frozen, so the value doesn't change.)
- The service tests use a small in-memory repository instead of mocks, so they assert real results.
- No secrets in code: the signing key lives in user-secrets, and the app refuses to start without a key of at least 32 bytes.
- `/dev/token` and Swagger only exist in Development, and a test proves `/dev/token` returns 404 outside it.
- Business rules in the entity, all validation errors returned at once, and a failed update changes nothing.
- A PUT that leaves out `status` returns 400 instead of silently resetting the task to Todo.
- The due date rule only runs when the date changes, so an overdue task can still be renamed or marked Done.

## What I corrected or would improve

| # | Finding | Why it matters | Fix |
|---|---|---|---|
| 1 | The AI's own test caught a bug: the dev token was issued with the fake test clock (2030), so it wasn't valid yet. The AI switched the issuer to the system clock. | Good sign that endpoint tests catch real problems. I agree with the fix: the JWT validator checks times against the system clock. | Accepted as is. |
| 2 | `Tasks.UserId` has no foreign key to `Users`, and any GUID in a token is accepted as a user. | A task can belong to a user that doesn't exist. Fine for a dev token, not for production. | Add the foreign key and check the user exists when the token is issued (or on first use). |
| 3 | `Description` has no maximum length, in the entity or in the database. | Anyone can send a huge text and it's stored as is. My prompt only said "optional", and the AI didn't add a limit on its own. | Add a maximum (for example 2,000 chars) in the entity and the EF configuration, with a boundary test. Lesson for the prompt: state limits for every text field. |
| 4 | `EnsureCreated` instead of migrations. | No way to evolve the schema without losing data. The AI flagged it in the README. | Use EF Core migrations, as in CivicJobs. |
| 5 | `UseHttpsRedirection` with no HTTPS port logs a warning on every test run. | Noise in the logs, and it hides real warnings. | Remove it for the HTTP-only dev profile, or configure the HTTPS port. |
| 6 | The list has no pagination and orders by due date and title, with no tie-breaker. | Large lists and unstable order for tasks with the same date and title. | Add `page` and `pageSize` with a maximum, and `ThenBy(Id)`. |

## Edge cases, authentication and validation

- **Edge cases:** due date equal to today is rejected (must be after today); Done can't move back to another status; title of only spaces is rejected; a PUT without status is a 400.
- **Authentication:** every task endpoint requires a valid JWT, the `sub` claim must be a GUID, and ownership is enforced in every repository query.
- **Validation:** rules live in the Core entity and return 400 `ValidationProblemDetails` with all errors at once; not found is 404; anything else is a 500 `ProblemDetails`.

## Takeaways

- A detailed prompt does most of the work. Naming the architecture, the security rules and the error formats gave a result that was close to production quality on the first run.
- The AI still needs a reviewer: the missing foreign key and the unbounded description pass all tests and only show up when you read the SQL and the configuration. And when the prompt leaves something out, the AI usually leaves it out too.
- In the main project (CivicJobs) I went further than one prompt: CLAUDE.md with the rules, a TDD skill (failing tests first, then the code), and a reviewer agent at the end of each block. One-shot is good for a scaffold; for real work I prefer small steps I can check.