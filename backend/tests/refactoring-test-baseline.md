# Backend Refactoring Test Baseline

This baseline protects observable behavior while backend implementation code is
refactored. Passing tests do not prove that every implementation line is
covered. They prove the business rules and public contracts listed below.

Verified locally on 2026-08-11:

- 63 Domain unit tests passed;
- 19 fast API integration tests passed;
- 82 backend tests passed in total.

## Test Change Policy During Refactoring

An implementation-only refactor may change test setup, fixture construction, or
test doubles when a dependency boundary moves. It must not silently change:

- accepted and rejected business transitions;
- authorization and cross-scope visibility;
- optimistic concurrency behavior;
- HTTP status codes and business problem codes;
- Test Plan and Test Run snapshot behavior;
- attachment metadata, download, and read-only rules.

If a test expectation must change, treat that as a product behavior change. The
change requires an explicit decision and a separate explanation from the code
cleanup. Do not delete or weaken a failing test merely to complete a refactor.

## Behavior Matrix

| Area | Domain unit protection | Fast API integration protection | External protection |
| --- | --- | --- | --- |
| Identity account and session lifecycle | Account versions, password state, login time, session expiry and revocation | Initialization, login, logout, account create/list/status | Deployment smoke confirms assembled authentication flow |
| Project and member lifecycle | Project updates; member roles, removal and restore | Owner creation, role changes, last-owner rule, inactive project, stale versions | PostgreSQL behavior remains deployment-smoke scope |
| Issue lifecycle | Status/completion, detail updates, assignment | Create, query/filter/page, update, assign, status, permission and stale version | PostgreSQL behavior remains deployment-smoke scope |
| Issue relations | Relation metadata, direction catalog, soft delete | Direction, symmetric duplicates, parent conflict, hierarchy cycle, validation, permission, inactive project and stale delete | PostgreSQL unique/concurrency semantics remain a future focused suite |
| Issue and Test attachments | Metadata and soft-delete lifecycle | Upload/list/open/delete, terminal Run read-only, and unavailable storage mapping through `IFileStorage` fake | Deployment smoke covers real local volume persistence and restart |
| Test Workspace, member, Suite, Case and Tag | Aggregate state, hierarchy metadata, ordered steps, role lifecycle | Workspace prefix rules, membership visibility, project link, Case Markdown fields, ordered steps and attachments | Deployment smoke covers persisted workflow |
| Case requirement trace | Link lifecycle and version | Link/list/delete, duplicate, linked active Project requirement and stale version | PostgreSQL unique constraint remains deployment-smoke scope |
| Test Plan | Draft/update state and linked test Issue | Case ordering, active scope validation, linked task Issue, stale version and Run source scope | Deployment smoke covers persistence |
| Test Run | Plan and Case snapshots, result state and terminal timestamps | Snapshot immutability, progress, step/item versions, completion rules, cancellation, rerun and terminal read-only behavior | Deployment smoke covers restart persistence |
| Run-created Bug trace | Run/Bug link metadata | Bug creation/list, linked active Project and Issue option validation | Deployment smoke covers persisted traceability |
| OpenAPI and error envelope | Not applicable | Canonical OpenAPI, unknown-resource problem details | Deployment readiness and smoke scripts |

## Isolation Rules

- Focused API behavior tests create a fresh `ApiIntegrationTestFactory`; they do
  not depend on test execution order or data left by another test.
- SQLite is a fast integration substitute, not a PostgreSQL mock. Do not claim
  that it validates PostgreSQL migrations, locks, indexes, or provider-specific
  concurrency.
- External services should be replaced at an existing contract. For example,
  attachment storage failures use an `IFileStorage` fake instead of changing
  directory permissions.
- Do not mock EF Core `DbSet`. Application services that currently depend on a
  concrete `DbContext` remain protected through the API integration suite until
  a real application boundary is introduced during refactoring.

## Refactoring Gate

Before and after each production refactoring step, run:

```powershell
dotnet test backend\KhaiKang.Backend.slnx `
  --configuration Release `
  --no-restore `
  --disable-build-servers `
  -m:1
```

Run the external PostgreSQL/deployment smoke test before an RC release. It is
not required after every local implementation-only edit.
