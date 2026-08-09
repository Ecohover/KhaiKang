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

Project creation and its first Owner membership are transactional. Foreign keys to `accounts` and `permissions` cross the module boundary at the database level but are mapped through explicit module contracts in application code.

## Deferred Models

`issue_comments`, `issue_relations`, `issue_relation_types`, Issue tags, watchers, and transitions are design directions only and do not exist in the MVP baseline.
