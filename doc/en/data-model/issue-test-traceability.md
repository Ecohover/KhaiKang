# Issue and Test-Asset Traceability Data Model

Status: product direction accepted; implementation started (2026-08-10).

## Ownership

- Project Management owns `issue_relation_types` and `issue_relations`.
- Test Management owns `test_case_requirement_links`, the Plan Test-Issue reference, the Run Test-Issue snapshot, and `test_run_bug_links`.
- Project Management exposes Issue identity, type, Project, and authorization through an explicit application contract. Test Management must not query its DbContext directly.
- Migration order remains Identity, Project Management, then Test Management.

## issue_relation_types

The table stores a stable UUID, unique `code`, forward and reverse labels, `direction_kind` (`symmetric`, `directed`, or `hierarchical`), sort order, system/active flags, and audit metadata.

The first version seeds `related`, `parent_of`, `blocks`, `duplicates`, and `tests`. Relation-type CRUD is not exposed.

## issue_relations

The table stores `id`, `project_id`, `relation_type_id`, `source_issue_id`, `target_issue_id`, soft-delete metadata, and common audit/version fields.

Constraints:

- source and target differ and both belong to `project_id`;
- symmetric `related` records are canonicalized by UUID order;
- active type/source/target tuples are unique;
- an active `parent_of` target has at most one parent;
- application code rejects direct and transitive hierarchy cycles;
- source, target, and Project lookup indexes are present.

## test_case_requirement_links

Each row stores `id`, `test_workspace_id`, `test_case_id`, `project_id`, `requirement_issue_id`, soft-delete metadata, and common audit/version fields. Active Case/Issue pairs are unique and require an active Workspace-Project mapping.

## Test Plan Test Issue

`test_plans` adds nullable `test_issue_project_id` and `test_issue_id`. The pair is either both null or both populated. The Issue belongs to that Project, the Workspace-Project mapping is active, and updates use the Plan's optimistic version.

## Test Run Snapshot

`test_runs` adds nullable `test_issue_project_id` and `test_issue_id`, copied from the Plan at Run creation. The API does not update these fields after creation. The pair is either both null or both populated.

## test_run_bug_links

Each row stores `id`, `test_workspace_id`, `test_run_id`, `project_id`, `bug_issue_id`, and creation audit data. The Issue must have type `bug` when created. An active Bug origin is unique while a Run may have many Bugs.

## Cross-Module Contract

The Issue directory contract must support authorized minimal Issue reads; Project ownership and type validation; `issue.read`, `issue.update`, and `issue.create` checks; and authorized batch reads. Minimal data includes Issue and Project IDs, Project code, Issue number, title, type, and status.

## Lifecycle and Concurrency

- Issue relations and Case requirement links use soft deletion.
- A Plan stores current configuration while a Run stores creation-time history.
- Run-Bug origin links remain historical evidence across lifecycle changes.
- All writes record the actor, timestamp, and resource version.
- Workspace-Project unlink rejects active Case, Plan, Run, or Bug trace dependencies.
- Link creation and unlink lock the same Workspace-Project mapping row.

## Future Extensions

- Run Item/Step defect evidence and cross-Run reproduction.
- Configurable Issue relation types and cross-Project relations.
- Requirement coverage and trace-graph read models.
