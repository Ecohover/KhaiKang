# Functional Spec Index

Status: synchronized with the implemented MVP on 2026-08-11.

| Area | Implemented MVP capability | Spec |
| --- | --- | --- |
| Identity | First-run setup, local accounts, cookie sessions, CSRF, password change, account lifecycle, global roles and permissions | [User and authentication](./user-authentication.md) |
| Project Management | Projects, fixed roles, members, issue metadata, list/board workflow, assignment, status, attachments | [Project management](./project-management.md) |
| Test Management | Workspaces, members, Project links, Suite/Case/Tag, Plan/Run, manual results, snapshots, evidence attachments | [Test management](./test-management.md) |
| Issue-Test Traceability | Fixed Issue relations, Case requirements, Plan Test Issue, Run snapshot, and Run-originated Bugs | [Issue-Test Traceability](./issue-test-traceability.md) |

Deferred until after the MVP: CI/Jenkins integration, Playwright execution, external result import, AI-generated or AI-executed tests, custom workflow/roles/fields, Issue tags/comments/watchers/notifications, configurable relation types, and advanced reporting.

The OpenAPI source of truth is [`contract/openapi/khaikang.v1.yaml`](../../../contract/openapi/khaikang.v1.yaml).
