---
name: tdd-cycle
description: Implement one behavior with a strict red, green, refactor cycle. Use when adding or changing any business rule, service method, repository method or endpoint.
---

# TDD cycle

Input: one behavior, for example "publishing a posting with a past closing date is rejected".

1. **Locate the rule.** Find its id in `docs/PRD.md` (FR-xx or BR-xx) and the details in `docs/user-story.md`. If the behavior has no id or is a non-goal, stop and ask me. Decide the layer (Core, Application, Infrastructure or Api) following the architecture rules in `CLAUDE.md`, and the test project and folder that match it. Say both in one line.
2. **Red.** Write one failing test for this behavior only, tagged with its id (`[Trait("Rule", "BR-05")]` or `[Trait("Requirement", "FR-08")]`). Run `dotnet test --filter <TestName>` from `backend/`. Show the failure message and confirm it fails for the right reason (an assertion, or a missing member the test is meant to drive, not an unrelated compile error). Stop and wait for my OK.
3. **Green.** Write the minimum production code to pass. Run the test, then the whole test project.
4. **Refactor.** Remove duplication and improve names with all tests green. No new behavior in this step.
5. **Report.** List the files changed and suggest commit messages: `test: ...`, `feat: ...`, `refactor: ...` (skip refactor if nothing changed).

Rules:
- One behavior per cycle. If you notice another missing rule, list it at the end instead of implementing it.
- Use `FakeTimeProvider` for anything that depends on the current date.
- Cover the boundary when there is one (closing date equal to today, SalaryMax equal to SalaryMin, title with exactly 3 chars).
- Never edit a test to make it pass unless I agree the test was wrong.
