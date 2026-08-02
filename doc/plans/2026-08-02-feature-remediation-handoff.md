# Work Handoff: Feature Remediation

## Objective

Continue product remediation on `ecohover/feature/20260802`. Complete test asset
maintenance, then add regression protection for Test Plan and Test Run flows.

Out of scope: CI result import, AI-generated test cases, Issue-triggered Test
Runs, test environments, repositories, and execution agents.

## Accepted Decisions

- Product UI must be available in English and Traditional Chinese; never translate user-authored values.
- Completed Test Runs are immutable. Cancelled Test Runs may restart and preserve results.
- Dependabot proposes updates; it never modifies `main` directly. Updates are grouped weekly.
- Keep all work on `ecohover/feature/20260802`; commit each completed stage and do not open a PR unless requested.

## Current State

- Branch: `ecohover/feature/20260802`.
- Remote base: `origin/ecohover/feature/20260802` at `7056ac3` before the current local dependency updates.
- Product commits already on this branch:
  - `30877aa` — Test Run lifecycle documentation, OpenAPI alignment, and restart regression test.
  - `7056ac3` — expanded frontend i18n coverage.
  - `fc31ab2` — Test Case title search and active/inactive filter in the suite tree.
- Local, not yet pushed dependency work: five linear Dependabot commits for coverlet, dotnet-ef, Microsoft.Extensions.Hosting, Microsoft.NET.Test.Sdk, and xunit.runner.visualstudio; `40ed233` groups future Dependabot updates weekly.
- Current implementation already supports Suite and Test Case edit, move, inactive status, and optimistic version conflicts.
- The test tree now filters Cases locally by title and status. No API contract change was made for filtering.

## Verification

- `dotnet test backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1`: passed, 20 tests.
- `pnpm type-check`: passed.
- `pnpm test -- --run`: passed, 26 web tests; UI package has no test files and exits successfully.
- `git diff --check`: passed before each committed feature stage.

## Remaining Work

1. Finish stage 3: add stable external Test Case identifiers, including Workspace Prefix rules, migration for existing records, OpenAPI/C#/TypeScript changes, and tests. Do not treat the current tree display index as a stable identifier.
2. Finish stage 3: Test Tag entity, CRUD, Case many-to-many association, and search/filter integration.
3. Audit remaining hard-coded product UI copy before declaring i18n complete.
4. Stage 4: add Plan/Run filters, state and empty displays, API regression tests, and minimum E2E coverage.
5. Before pushing, finish or explicitly abandon the remaining Dependabot branches; do not create merge commits when integrating them. Use linear cherry-picks and resolve central package version conflicts by retaining all intended latest versions.

## Context To Load

- `AGENTS.md`
- `.ai/INDEX.md`, then `.ai/backend.md`, `.ai/frontend.md`, `.ai/contract.md`, and `.ai/verification.md` only when their scope applies.
- `doc/zh-TW/04-planning/06-current-feature-remediation-plan.md`
- `doc/zh-TW/02-specs/03-test-case-management.md`
- `doc/zh-TW/03-data-model/03-test-case-management-data-model.md`
- `doc/zh-TW/04-planning/05-test-management-implementation-plan.md`
