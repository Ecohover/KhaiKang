# Project Management Specification

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [專案管理規格](../../zh-TW/02-specs/02-project-management.md).

## Purpose and Scope

The MVP provides a Kanban-oriented project and issue core without Sprint coupling. It includes projects, members, fixed project roles, issue types/statuses/priorities, issue list and board views, assignment, free status transitions, completion information, filtering, sorting, and attachments.

Custom workflow editors, custom roles, custom fields, Issue tags, comments, watchers, notifications, and AI execution are not part of the MVP. Basic fixed Issue relations are delivered by the traceability phase.

## Projects, Membership, and Roles

- A project has a unique code, name, optional description, and `active` or `inactive` status.
- The creator becomes an active member with the Owner role in the same transaction.
- MVP project roles are Owner, Manager, Contributor, and Reviewer. Their permission mappings are seeded and cannot be edited through the MVP UI.
- System administration does not automatically grant access to every project's internal resources; project scope is determined by active membership and permissions.
- Removing a member removes project access, not the account.

## Issues

An Issue belongs to one Project and has a Project-scoped sequential issue number. The implemented fields are title, optional description, user story, definition of done, type, status, priority, reporter, optional assignee, optional completion summary, completion time, and audit metadata.

- Title, type, status, and priority are required.
- New issues are unassigned unless an assignee is explicitly selected.
- Status and assignee may change independently; the MVP has no transition gate.
- Moving to a status in the `done` category records completion; leaving it clears completion state according to the application contract.
- Writes use optimistic `version` checks and return conflict when the resource changed concurrently.

The list supports keyword, type, status, priority, assignee/unassigned, sorting, paging, and list/board presentation. Metadata endpoints provide the active type, status, priority, and member choices.

## Issue Attachments

Existing Issues support list, multipart upload, content download, and soft delete. PostgreSQL stores metadata only; `IFileStorage` stores bytes under an opaque UUID-based key. The original file name is retained for display and download. The configured maximum size defaults to 20 MiB.

Authorization uses `issue.read`, `issue.attachment.upload`, and `issue.attachment.delete`. Inactive projects reject writes. Path traversal, empty files, oversized files, missing parents, and cross-project access must be rejected.

## Issue Relations and Test Traceability

The [Issue-Test Traceability specification](./issue-test-traceability.md) defines the fixed `related`, `parent_of`, `blocks`, `duplicates`, and `tests` Issue relations. Project Management owns their types and relation records. Direction is retained except for symmetric `related`; hierarchy prevents multiple parents and cycles.

Test Management links Cases to requirement Issues, stores one current Test Issue on a Plan, snapshots that Issue on Run creation, and records Bugs created from the Run. Test Management uses a minimal Project Management application contract rather than reading its DbContext.

## Security and Audit

Project and Issue operations are restricted to active members with the required fixed-role permissions. Creation, update, status, assignment, membership, role, and attachment operations retain actor and timestamp information. Client-visible contracts use RFC 9457 Problem Details and the canonical OpenAPI schema.

## Acceptance Criteria

- Authorized users can create and update projects, manage members, and use the fixed roles.
- Authorized project members can create, filter, sort, assign, transition, and complete Issues.
- Issue attachments can be uploaded, listed, downloaded, and soft-deleted without crossing Project boundaries.
- Inactive Projects retain history and reject disallowed writes.
- Concurrent writes are detected through resource versions.
