# Project Management Data Model

Status: synchronized with `InitialProjectManagement` on 2026-08-09. Traditional Chinese counterpart: [專案管理資料模型](../../zh-TW/03-data-model/02-project-management-data-model.md).

## Implemented Tables

| Table | Key responsibility and constraints |
| --- | --- |
| `projects` | Unique project code, name, description, active/inactive status, creator, and audit metadata. |
| `project_roles` | Seeded fixed roles: Owner, Manager, Contributor, Reviewer. |
| `project_members` | Project/account relationship and active lifecycle; unique active membership per pair. |
| `project_member_roles` | Unique member/role mappings. |
| `project_role_permissions` | Unique role/permission mappings referencing Identity permissions. |
| `issue_types` | Seeded extensible Issue types such as story, task, bug, and spike. |
| `issue_statuses` | Seeded statuses with category (`todo`, `doing`, `done`) and sort order. |
| `issue_priorities` | Seeded low, medium, high, and critical priorities. |
| `issues` | Project-scoped `issue_no`, content, metadata foreign keys, reporter, optional assignee, completion fields, and audit metadata. `(project_id, issue_no)` is unique. |
| `issue_attachments` | Attachment metadata, opaque storage key, hash, size, uploader, soft-delete state, and audit metadata. Bytes are not stored in PostgreSQL. |
| `project_audit_events` | Project-domain actor, event, target, outcome, and occurrence time. |
| `project_number_counters` | PostgreSQL-managed scoped counter used by `next_project_number`; `(counter_type, scope_id)` is the primary key. The MVP uses type `issue` with Project ID as scope. |

Project creation and its first Owner membership are transactional. Foreign keys to `accounts` and `permissions` cross the module boundary at the database level but are mapped through explicit module contracts in application code.

Issue creation calls `next_project_number('issue', project_id)` in the same transaction as the Issue insert. PostgreSQL serializes updates to the matching counter row, so concurrent creates receive distinct Project-scoped numbers without locking unrelated Projects.

## Traceability Extension

The active traceability phase adds seeded `issue_relation_types` and `issue_relations` owned by Project Management. Test-specific Case, Plan, Run, and Bug trace data remains owned by Test Management. See [Issue-Test Traceability](./issue-test-traceability.md).

Issue comments, tags, watchers, configurable transitions, and custom relation types remain deferred.
