# Backend Domain Readability Refactoring Plan

Status: active incremental refactoring. Project Management's public long-parameter debt is cleared, and Identity refactoring is in progress. Current work state and resume instructions are tracked in the [backend refactoring execution tracker](../../zh-TW/04-planning/11-backend-refactoring-execution-tracker.md).

## Goals

- Make Domain APIs read in domain language.
- Prevent positional mistakes in long, same-type primitive parameter lists.
- Consolidate genuinely shared audit lifecycle behavior without introducing a framework-like inheritance hierarchy.
- Preserve HTTP contracts, database columns, stable codes, and observable behavior.

## Current debt baseline

As of 2026-08-12, the architecture fitness baseline records:

- Project Management: zero long public Domain members and one remaining repeated audit owner (`Project`).
- Identity: four long public Domain members and three repeated audit owners.
- Test Management: twenty-five long public Domain members and seventeen repeated audit owners.

`Issue` and `Project` creation and change APIs have already moved to intention-revealing creation/change models. The remaining work must not add smaller types merely to reduce parameter or line counts.

## Target shape

- Express creation through an intention-revealing factory such as `Issue.Create(IssueCreation creation, ChangeContext context)`.
- Express detail changes through `UpdateDetails(IssueDetailsChange change, ChangeContext context)` or equivalent domain names.
- Parameter Objects remain immutable and use-case or domain specific. They are not generic bags and are not reused HTTP request DTOs.
- Use one shallow `AuditableEntity` base for genuinely shared created/updated actor and time fields plus optimistic `Version` behavior.
- Keep lifecycle-specific fields such as `CompletedAt`, `DeletedAt`, and executor evidence on their owning entities.
- Keep current EF Core column names and concurrency behavior. A structural refactor should normally produce no schema migration.

## Sequence and gates

1. Complete Issue characterization tests for creation, updates, status, assignment, audit metadata, and version changes.
2. Add an architecture fitness baseline for existing long parameter lists and repeated audit declarations. New code cannot grow the allowlist.
3. Introduce `ChangeContext` and validate one shallow auditable base within Project Management.
4. Refactor Issue creation and detail updates without changing Application or HTTP contracts.
5. Remove approved debt entries module by module: Project Management, Identity, then Test Management.
6. Run unit tests, API integration tests, formatting, and EF pending-model checks for each batch.

Persistent exceptions to the three-parameter rule require an ADR. A base class must not become a deep `BaseEntity -> BaseAuditableEntity -> BaseAggregateRoot` framework.
