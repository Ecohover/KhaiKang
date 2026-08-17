# Backend Readability Refactoring Plan

Status: active, principle-driven refactoring. Current work state, evidence, and resume instructions are maintained in the [backend refactoring execution tracker](../../zh-TW/04-planning/11-backend-refactoring-execution-tracker.md).

## Completion model

A repository-wide engineering principle is the unit of completion. A feature, module, resource, or folder is only a characterization, review, rollback, and commit checkpoint within that principle.

Completing one Project Management, Identity, or Test Management slice does not complete a principle. A principle is complete only when its repository-wide inventory is cleared, or every remaining exception has explicit Human approval, rationale, and an exact non-growing baseline or ADR.

This does not authorize a single repository-wide rewrite. Each principle is implemented through small vertical slices that preserve observable behavior and can be reviewed and reverted independently.

## Execution model

For every active principle:

1. Define the repository-wide invariant and its exception boundary.
2. Inventory all current debt before changing production code.
3. Add characterization tests for observable behavior, wire contracts, persistence, audit data, and concurrency where applicable.
4. Implement one semantic family or vertical slice without mixing unrelated principles.
5. Reduce an exact debt baseline; never replace one broad abstraction with another broad abstraction.
6. Run targeted and full verification, then obtain independent review.
7. Update the tracker with evidence, remaining debt, approved exceptions, and the next safe checkpoint.

## Principle workstreams

| ID | Principle | Repository-wide completion condition |
| --- | --- | --- |
| P01 | Refactoring safety net | Every changed behavior is protected at the correct boundary; external systems, databases, file storage, and clocks are mocked or isolated where appropriate; tests assert observable behavior rather than private implementation. |
| P02 | Domain and Application API readability | Public business APIs avoid long or ambiguous primitive parameter lists; creation, change, and context models represent real concepts; exceptions are documented and do not grow. |
| P03 | Public boundary contracts | Public HTTP and cross-module contracts use explicit body-form types, one public type per matching file, and preserve canonical required, nullable, JSON, OpenAPI, and TypeScript behavior. |
| P04 | Result and Outcome semantics | Results expose no impossible outcome or payload combination; only genuine semantic families share a type; no-payload operations do not use fake `object` payloads; endpoint mappings are exhaustive; untyped business error strings are removed or explicitly approved. |
| P05 | Closed states and stable codes | Closed state sets use enum or value-object semantics while persistence stores stable English codes; table-managed classifications expose stable client-facing codes and display names; code mappings are named and round-trip tested. |
| P06 | Audit lifecycle and mutation context | Shared created/updated actor, time, and version behavior is centralized only where lifecycle and nullability match; mutation context is passed intact; feature-specific lifecycle fields remain with their owner. |
| P07 | Query and paging readability | EF queries read in the order normalize, filter, order, count, page, project, execute; native EF capabilities are preferred; generic query frameworks do not hide business conditions. |
| P08 | Abstraction restraint | Helpers, factories, interfaces, repositories, base types, and parameter objects exist only when they add domain language, invariants, boundary isolation, or demonstrated reuse. |
| P09 | Physical organization | Public types are organized by resource or use case, not technical buckets; one top-level public type lives in one matching file; cross-resource coordinators remain at the layer root until a real owner exists. |
| P10 | Enforcement and delivery | Objective rules are enforced progressively by compiler, analyzer, architecture tests, format, contract checks, and CI; branch migration policy is checked before merge; baselines only shrink. |

## Human and AI responsibilities

- The Human owner decides business semantics, compatibility, persistence meaning, public-contract changes, exceptions, risk acceptance, merge, and release.
- The AI Builder inventories debt, adds characterization, implements bounded slices, verifies behavior, and synchronizes documentation and the tracker.
- An independent AI Reviewer checks semantic reachability, abstraction value, contract drift, test quality, and whether the global debt actually decreased.
- CI enforces objective rules only. It must not pretend to decide whether two operations genuinely share domain semantics.

## Required batch boundaries

- A batch addresses one global principle. It may span service, endpoint, domain, and tests when they form one vertical slice.
- A batch must not silently combine structural refactoring with a wire-contract, database-schema, business-rule, or error-identity change.
- Characterization precedes production changes when behavior or compatibility is at risk.
- Checkpoint commits are deliberately small enough to review and revert, but a checkpoint does not mark the global principle complete.
- Do not introduce a new generic `OperationResult<T>`, query framework, base hierarchy, or property bag merely to make local code appear uniform.

## Repository-wide definition of done

A principle may be marked complete only when:

- its inventory is zero, or every exception has explicit Human approval and durable rationale;
- the exact architecture or debt baseline cannot grow;
- observable behavior and applicable HTTP, OpenAPI, TypeScript, persistence, audit, and concurrency semantics are covered;
- affected Release builds, unit tests, integration tests, formatting, and contract or EF checks pass;
- independent review reports no unresolved blocker, high, or medium finding;
- the tracker records verification evidence, approved exceptions, and the next safe action.
