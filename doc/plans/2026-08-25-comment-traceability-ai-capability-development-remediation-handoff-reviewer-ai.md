# Development Remediation Handoff：PF-02、PF-03、PF-04 Reviewer Recheck

## Task / Feature

Comment、追溯與 AI 操作紀錄能力的 Implementation Review 一次指定修正：PF-02、PF-03、PF-04。

## Current Phase

`Implementation Review` direct correction 已完成，等待 Reviewer AI recheck。適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`。

## Source-of-Truth Paths

- `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`
- `doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md`
- `doc/plans/2026-08-25-comment-traceability-ai-capability-remediation-intake.md`
- `doc/plans/2026-08-25-comment-traceability-ai-capability-development-remediation-handoff.md`
- `AGENTS.md`、`.ai/backend.md`、`.ai/frontend.md`、`.ai/contract.md`、`.ai/verification.md`

## Scope / Non-Goals

- 已修正 PF-02、PF-03、PF-04，未改變產品語意或 public HTTP contract。
- PF-01 明確不在本次授權；未修改 AI Comment read owner-derived scope，仍由 PM AI／human PO disposition。
- 未執行 Formal QA、deployment、push、release 或 phase advancement。
- 原有未追蹤 planning drafts `doc/zh-TW/04-planning/13-comment-traceability-ai-capability-prep-draft.md`、`14-ai-skill-khaikang-integration-guide.md` 未變更、未提交。

## Produced Commits and Exact Diff

### PF-03 — `9779731451e7de0484711dd914baca594698818d`

`fix: notify successful AI token mutations`

- Added `frontend/apps/web/src/features/aiAccounts/tokenMutationNotice.ts`.
- Added `frontend/apps/web/src/features/aiAccounts/tokenMutationNotice.spec.ts`.
- Updated `frontend/apps/web/src/views/AiAccountManagementView.vue`.
- Token create calls the global created Save Notice; token re-create calls the updated Save Notice. Owner-only plaintext acknowledgement remains in `UiActionDialog`; token view is not misclassified as a mutation.

### PF-04 — `c92f5eee07e9904da74b921f628acb80fa10002f`

`fix: require persistent token protection keys`

- Added `backend/src/KhaiKang.Api/Configuration/DataProtectionKeyOptions.cs`.
- Updated `backend/src/KhaiKang.Api/Program.cs` and `backend/src/KhaiKang.Api/appsettings.json`.
- Added `backend/tests/integration/KhaiKang.Api.IntegrationTests/Identity/DataProtectionKeySourceTests.cs`.
- Updated `deploy/README.md` with non-secret persistent storage guidance.
- Non-Testing startup now validates `DataProtection:KeysDirectory`; Testing retains the explicit ephemeral provider. The continuity test issues through `AiAccountService`, constructs a second Data Protection provider over the same key ring, and recovers through the authorized owner's `ViewTokenAsync` path without writing plaintext to test output.

### PF-02 — `6e550577515b804dcfcefc1a345f74cfeb0a97e2`

`chore: consolidate traceability migrations`

- ProjectManagement replaces the two unpublished working migrations with one final migration:
  - `20260825041524_AddCommentTraceabilityAiCapability.cs`
  - `20260825041524_AddCommentTraceabilityAiCapability.Designer.cs`
- TestManagement replaces the two unpublished working migrations with one final migration:
  - `20260825041607_AddCommentTraceabilityAiCapability.cs`
  - `20260825041607_AddCommentTraceabilityAiCapability.Designer.cs`
- EF regenerated both snapshots; their final content was identical to the already combined model, so Git records no snapshot diff.
- Repository evidence for unpublished status: source commits `3a81302` and `4204603` were contained only by `ecohover/feature/20260820`, not by another local or remote branch.

## Validation Evidence

### PF-03 frontend

- Red: `pnpm --filter @khaikang/web test -- src/features/aiAccounts/tokenMutationNotice.spec.ts` failed because `tokenMutationNotice` did not exist.
- Green focused rerun: same command passed 1/1.
- `pnpm --filter @khaikang/web type-check`: passed.
- `pnpm --filter @khaikang/web test`: passed, 10 files / 40 tests.
- `pnpm --filter @khaikang/web build`: passed; retained the existing Vite warning for a chunk larger than 500 kB.

### PF-04 backend and security boundary

- Red: focused `DataProtectionKeySourceTests` initially failed because Production startup threw no exception when `DataProtection:KeysDirectory` was missing.
- Green: `dotnet test backend/tests/integration/KhaiKang.Api.IntegrationTests/KhaiKang.Api.IntegrationTests.csproj --filter FullyQualifiedName~DataProtectionKeySourceTests --disable-build-servers -m:1` passed 2/2, including key-ring continuity across provider restart.
- Regression: the same project with filter `FullyQualifiedName~DataProtectionKeySourceTests|FullyQualifiedName~AiAccountTokenEndpointsTests` and `--no-build` passed 4/4.
- Targeted `dotnet format ... --verify-no-changes --include` for the new options, Program, and test files passed after import normalization.

### PF-02 migrations and PostgreSQL

- `dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1`: passed after final migration generation, 0 warnings / 0 errors.
- `dotnet ef migrations has-pending-model-changes` for ProjectManagement and TestManagement: both built successfully and reported `No changes have been made to the model since the last migration.`
- Clean PostgreSQL chain: a new no-volume `postgres:17-alpine` container named `khaikang-pf02-postgres-20260825` used a random localhost port and an explicitly disposable `khaikang_traceability_pf02_20260825` database. `TraceabilityPostgreSqlSchemaTests` applied Identity → ProjectManagement → TestManagement migrations from a clean database and passed 1/1 in 4.2042 seconds. The container was stopped and automatically removed afterward.
- The first PostgreSQL attempt is not counted as evidence: the existing dev container had no published host port, so the host test reached a different local instance and failed with PostgreSQL `28P01`. Its explicitly disposable database was removed. The isolated-container rerun above is the valid result.
- Targeted formatter verification for both final migration pairs and both snapshots passed after EF-generated CRLF/BOM normalization.
- `git diff --check 7d77a44..6e55057`: passed with no output.

## Not Verified / Known Risks

- Full backend solution tests were not run; focused startup/token and clean PostgreSQL migration-chain seams plus a full backend build were run.
- No browser/manual UI run was performed for the Save Notice; focused frontend state coverage, type-check, complete frontend unit suite, and production build passed.
- Restart continuity was verified with two independent Data Protection providers over one durable key directory and the real Identity application service, not with a deployed multi-replica environment.
- OpenAPI lint/bundle/breaking checks were not run because this remediation did not change the public HTTP contract or OpenAPI artifact.
- This evidence is Development verification, not Reviewer approval or Formal QA.

## Open Decisions / Blockers

- No blocker remains for PF-02, PF-03, or PF-04 from Development's scoped correction.
- PF-01 remains unresolved and excluded. It requires PM AI／human PO disposition and must not be treated as a second direct Development correction in this recheck.

## Next Role and Required Action

Reviewer AI rechecks only PF-02、PF-03、PF-04 against the original Implementation Review conclusion and this exact commit set. If any same Problem Family recurs, stop the direct loop and escalate evidence to PM AI. Reviewer AI does not run Formal QA, push, deploy, or advance the phase.
