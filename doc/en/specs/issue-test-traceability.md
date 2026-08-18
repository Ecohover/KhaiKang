# Issue and Test-Asset Traceability Specification

Status: implemented and verified by integration, frontend, fresh PostgreSQL, and Docker smoke checks (2026-08-11).

## Purpose

This feature creates an authorized trace chain across requirements, test tasks, test cases, test plans, test runs, and defects while preserving the existing Project and Test Workspace access boundaries.

```text
Requirement Issue <- tests - Test Issue <- Test Plan <- Test Run -> Bug Issue
        ^
        |
    Test Case
```

## Scope

The first version includes:

- basic Issue-to-Issue relations with explicit direction and hierarchy semantics;
- direct many-to-many Case-to-requirement Issue links;
- at most one Test Issue on a Test Plan;
- a snapshot of that Test Issue when a Run is created;
- origin links from a Run to Bug Issues created from that execution;
- authorized navigation from either side of the trace;
- Workspace-Project scope and both Project and Workspace authorization checks.

Custom relation types, automatic test generation, Run Item or Step defect evidence, weighted coverage, external automation imports, and AI-created links are outside the first version.

## Basic Issue Relations

Project Management owns Issue relations. The first version seeds these fixed types:

| Code | Forward label | Reverse label | Direction | Rule |
| --- | --- | --- | --- | --- |
| `related` | relates to | relates to | symmetric | Stored once in canonical order. |
| `parent_of` | parent of | child of | hierarchical | Source is parent and target is child; a child has at most one active parent and cycles are forbidden. |
| `blocks` | blocks | blocked by | directed | Source blocks target. |
| `duplicates` | duplicates | duplicated by | directed | Source is the duplicate and target is the retained Issue. |
| `tests` | tests/verifies | tested/verified by | directed | Source is the Test Issue and target is a covered requirement Issue. |

The first version permits relations only inside one Project. Self-relations and duplicate active relations are forbidden. `related` is symmetric; all other types preserve source and target.

The Test Issue uses the existing `task` type in the first version. The semantic role comes from the `tests` relation code, not a localized display label.

## Test Trace Semantics

### Case and Requirement Issues

A Case may validate multiple requirement Issues, and an Issue may be validated by multiple Cases. A link describes intended coverage; it does not mean that the requirement passed.

### Plan and Test Issue

A Plan has at most one direct Test Issue. A Test Issue can have multiple Plans across versions or cycles. The Test Issue covers requirements through Issue `tests` relations; the Plan does not duplicate that requirement list. The value remains optional for compatibility with existing Plans.

### Run Test-Issue Snapshot

Run creation copies the Plan's current Test Issue. Later Plan changes never rewrite an existing Run. The snapshot may be empty when the source Plan has no Test Issue.

### Run and Bug Issues

A Run may create multiple Bug Issues. Each newly created Bug stores that Run as its origin. The first version stores one origin Run per new Bug; cross-Run reproduction links are deferred.

## Workflow

1. Create requirement Issues and one Test Issue.
2. Link the Test Issue to one or more requirements with `tests`.
3. Link Cases to the requirements they validate.
4. Select one Test Issue on the Plan.
5. Snapshot that Test Issue when creating a Run.
6. Create Bugs from a failed Run while retaining the origin Run.
7. Show only trace data for which the caller can read both the Project and Workspace side.

## Consistency Rules

- Test traces require an active Workspace-Project mapping.
- Active Case, Plan, Run, or Bug traces block removal of that mapping.
- Deactivating a Workspace or Project preserves history but prevents trace writes.
- Case, Plan, Run, and Issue lifecycle changes do not delete trace history.
- Run snapshots do not follow later Plan or Issue-relation changes.
- Unique constraints prevent duplicate active links, and replacement or removal uses optimistic concurrency.

## Authorization

Issue relation reads require `issue.read`; creation requires `issue.relation.create`; removal requires `issue.update`. Both Issues must be in the same Project in the first version.

Test trace reads require both active Workspace membership and `issue.read` in the Issue Project. Creating, replacing, or removing Case and Plan links requires Workspace `owner` or `manager`, `issue.update`, active resources, and an active Workspace-Project mapping.

Run snapshots are system-generated from an authorized Plan. Creating a Bug from a Run additionally requires `issue.create`; the origin link is saved with the Bug creation workflow.

## Acceptance Criteria

- Users can create the five fixed Issue relations and see correct forward and reverse semantics.
- `parent_of` rejects a second parent, self-relations, and direct or indirect cycles.
- `related` A-B and B-A are treated as one relation.
- A Test Issue can cover several requirements through `tests`.
- A Case can link to several requirement Issues.
- A Plan has at most one Test Issue and a Run snapshots it at creation.
- Later Plan changes do not affect an existing Run.
- A Run retains origin links to the Bug Issues created from it.
- Unauthorized, cross-Project, or out-of-scope requests reveal no opposite-side data.
- Concurrent writes return a conflict rather than silently overwriting data.

## Future Extensions

- Bug reproduction across Runs and Run Item or Step evidence.
- Trace graphs and requirement coverage reporting.
- Configurable relation types and cross-Project relations.
