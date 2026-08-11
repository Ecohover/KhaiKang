# Data-Model Overview

Status: synchronized with the baseline migrations on 2026-08-09. Traditional Chinese counterpart: [資料模型總覽](../../zh-TW/03-data-model/01-data-model-overview.md).

KhaiKang is a modular monolith with one PostgreSQL database and three EF Core DbContexts:

- Identity owns accounts, sessions, global roles, permissions, and identity audit events.
- Project Management owns Projects, fixed Project roles/memberships, Issues, Issue metadata, attachments, and project audit events.
- Test Management owns Workspaces, Project links, members, Suites, Cases, Tags, Plans, Runs, results, and test attachments.

Each context has one unpublished-MVP baseline migration. The contexts share PostgreSQL and `__EFMigrationsHistory`, while modules retain model and migration ownership. Identity is applied before Project and Test because cross-module foreign keys reference `accounts`; Project is applied before Test because Workspace links reference `projects`.

Entity keys are UUIDs. Human-readable scoped numbers are separate integer columns. Database identifiers use `snake_case`. Timestamps use UTC `timestamp with time zone`. Mutable resources use optimistic integer `version` fields.

Deferred tables such as Issue comments, relations, tags, watchers, transitions, CI repositories/environments/agents, and AI execution records are not present in the MVP baseline.

The active [Issue-Test Traceability model](./issue-test-traceability.md) assigns basic Issue relations to Project Management and test-specific trace records to Test Management. Its implementation is being added after the current migration baseline.
