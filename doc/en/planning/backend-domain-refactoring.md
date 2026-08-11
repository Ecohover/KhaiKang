# Backend Domain Readability Refactoring Plan

Status: accepted for incremental refactoring. This document records current debt and sequencing; it does not claim that production code has been refactored.

## Goals

- Make Domain APIs read in domain language.
- Prevent positional mistakes in long, same-type primitive parameter lists.
- Consolidate genuinely shared audit lifecycle behavior without introducing a framework-like inheritance hierarchy.
- Preserve HTTP contracts, database columns, stable codes, and observable behavior.

## Confirmed debt

- `Issue` creation currently takes thirteen positional parameters, including several GUIDs and nullable strings.
- `Issue.UpdateDetails` currently mixes editable content, classification, actor, and occurrence time in nine parameters.
- Thirty-three Domain types currently declare a complete or near-complete set of audit fields and duplicate initialization/change behavior.

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
