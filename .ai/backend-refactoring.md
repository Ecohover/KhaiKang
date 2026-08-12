# Backend Refactoring Context

Read this file only for incremental backend readability refactoring.

## Required references

1. Read `.ai/backend.md`.
2. Read the affected sections of `doc/en/dotnet-development-guidelines.md`.
3. Read `doc/en/planning/backend-domain-refactoring.md` for the accepted direction.
4. Read `doc/zh-TW/04-planning/11-backend-refactoring-execution-tracker.md` for current status, decisions, evidence, and the resume point.
5. Read `.ai/verification.md` before reporting completion.

## Execution rules

- Treat the execution tracker as work state, not as a replacement for specifications, tests, OpenAPI, or current code.
- Work on one cohesive action item or one tightly related batch at a time.
- Add characterization tests before changing observable behavior, audit metadata, version handling, contracts, or persistence semantics.
- Do not create a parameter object, helper, factory, command, or base type only to satisfy a numeric rule or reduce line count.
- Keep public contribution and build workflows independent of private prompt repositories and specific AI tools.
- Update the tracker before leaving an incomplete batch and after completing a verified commit.
- Do not resolve items marked `waiting-human` without an explicit decision.
- Do not commit, push, publish, or deploy unless the user has authorized that action.

## Resume protocol

1. Check the current branch, `git status`, and recent commits.
2. Compare live state with the tracker's `Resume Point`.
3. If they differ, trust Git, accepted specifications, tests, and current code; then correct the tracker.
4. Continue only the next safe action that does not require an unresolved human decision.
