# Test Management Specification

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [測試案例管理規格](../../zh-TW/02-specs/03-test-case-management.md).

## Purpose and Scope

The MVP closes a manual testing workflow:

```text
Test Workspace -> Suite -> Case -> Plan -> Run -> Manual Result
```

It includes Workspace membership, fixed roles, Project links, Suite trees, Cases and Steps, global Tags, fixed Plan items, Run snapshots, manual case/step results, and attachments for Cases and Run Items.

CI, Playwright, repositories, environments, execution agents, external result import, AI generation/execution, and Issue-triggered Runs are deferred. Direct Issue links are implemented by the accepted [Issue-Test Traceability specification](./issue-test-traceability.md).

## Workspace and Access

- A Test Workspace is the test-asset access boundary and has a unique name and 2–10 character alphanumeric prefix beginning with a letter.
- The creator becomes Owner. Fixed roles are Owner, Manager, Tester, and Viewer.
- Assets are collaboratively maintained; they do not have per-owner ACLs.
- A Workspace may link to multiple Projects and a Project may link to multiple Workspaces. Links support navigation only and grant no access by themselves.
- The Workspace-Project link is the prerequisite for traceability; it does not itself create an Issue/Case, Plan, or Run link.

## Suite, Case, and Tag

- Suites form a tree with a maximum depth of five.
- Every Case belongs to exactly one Workspace and one Suite, contains at least one ordered Step, and may have zero or more global Tags.
- UUID is the internal primary key. `case_no` is stable and unique within the Workspace; the display code is `{PREFIX}-TC{caseNo}`.
- Moving a Case between Suites, renaming it, or changing its content does not change its number or any existing Plan/Run snapshot.
- Cases and Suites use active/inactive lifecycle rather than destructive deletion.

## Plans

- A Plan belongs to one Workspace, has a Workspace-scoped stable `plan_no` and `{PREFIX}-TP{planNo}` code, and contains at least one ordered Plan Item.
- The case tree can add a Suite and its descendant active Cases in bulk, then individual Cases may be removed or reordered.
- Stored Plan Items are a fixed selection; Suite, Tag, or search criteria are not persisted as dynamic filters.
- Draft and active Plans may be adjusted for future Runs. Archived Plans cannot create Runs.

## Runs and Locking

- A Run is created from one Plan and receives a Plan-scoped stable `run_no` and `{PREFIX}-TP{planNo}-R{runNo}` code.
- Creation copies each Case and Step into immutable Run Item snapshots.
- A new Run is `not_started`; a user explicitly starts it to enter `in_progress`.
- While `in_progress`, the item set and snapshots are locked, but execution is not assigned or locked to one person. Any authorized active Workspace member may update results; each write records the actual account and time.
- Case result changes apply to its Steps. Step changes recalculate the Case summary. Result states are `not_run`, `passed`, `failed`, `blocked`, and `skipped`.
- All required Case/Step results must be non-`not_run` before completion.
- Completed Runs are immutable. Cancelled Runs retain results and may restart as `in_progress`.

## Attachments

Cases and in-progress Run Items support list, upload, download, and soft delete. Run Item attachments are execution evidence. Completed Runs are read-only. PostgreSQL stores metadata and an opaque storage key; `IFileStorage` stores bytes.

## Acceptance Criteria

- Workspace isolation and fixed-role authorization apply to every asset.
- Users can complete the full Workspace-to-Run manual workflow.
- Existing Runs retain their original snapshots after Case or Plan changes.
- Concurrent writes use optimistic versions.
- Case and Run Item attachments cannot cross Workspace boundaries.
- Completed Runs and their evidence are immutable.
