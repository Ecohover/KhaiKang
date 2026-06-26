# Development Guidelines

## Purpose

This document defines the development principles, code boundaries, and implementation style for KhaiKang.
It exists to keep future feature work, refactoring, and review consistent.

## Scope

- `src/backend/`
- `src/frontend/`
- `src/worker/`
- `src/contracts/`
- `deploy/`

## Core Principles

1. Preserve maintainability before abstraction.
2. Preserve consistency before personal style.
3. Support human workflows first, then reserve AI workflows.
4. Build a clear modular monolith before considering service splitting.
5. Write the spec first, then implement.

## Architecture Principles

- Use a modular monolith.
- Keep module boundaries clear.
- Put shared rules in contracts or shared foundation layers, not scattered across modules.
- Do not split into microservices too early.
- Do not hardcode cross-module rules inside controllers or pages.

### Example

Good:

```text
Authentication -> Project -> Work Item -> Test Management
```

Bad:

```text
Controller A directly calls Controller B
Page A copies Page B's flow
```

## Documentation and Spec Principles

- Every feature must have a spec before implementation.
- Write the spec in Chinese first, then add English after review.
- Keep documentation structure consistent.
- Each spec should include index, scope, non-goals, flow, rules, and acceptance criteria.

### Example

Recommended spec order:

1. Purpose
2. Related Docs
3. Scope
4. Non-Goals
5. Core Model
6. Flow
7. State and Rules
8. Security and Audit
9. Acceptance Criteria
10. Open Questions

## Naming Principles

- Use English for classes, interfaces, DTOs, and enums.
- Use English for source code comments.
- Use Chinese first for docs, then add English after validation.
- Use `.` as the permission string separator.

### Example

Good permission strings:

```text
user.manage
project.read
project.write
testcase.create
```

Not recommended:

```text
USER::MANAGE
ProjectRead
test-case-create
```

## Authorization Principles

- Start with two global roles: `admin` and `user`.
- Project roles are separate from global roles.
- Permission strings describe capabilities, not scope.
- Scope is represented by membership or assignment.
- Add workspace scope later if needed.

### Example

User A:

- Global role: `user`
- Project X role: `project_admin`
- Project Y role: `project_member`

This means:

- A can manage Project X
- A can participate in Project Y
- A does not become a system admin because of a project role

## AI and Human Workflow Principles

- Phase 1 is human workflow first.
- AI capabilities must reserve queue / job / hook entry points.
- Humans use the UI.
- AI uses queue / background jobs / agent workers.
- Notifications go through webhooks, in-app notifications, or other notification channels.
- AI actions must be auditable, reversible, and traceable.

### Example

Human flow:

```text
User -> UI -> Change Status -> Audit Log -> Notification
```

AI flow:

```text
User -> Queue Job -> AI Agent -> Proposal -> Approval -> Apply -> Audit Log
```

## Implementation Principles

- Implement in small steps.
- Build the minimum usable version first.
- Do not try to implement every future need at once.
- Make the workflow run first, then add abstractions.
- Extract shared components or services only when there is a clear reuse case.

### Example

Good:

```text
Finish login first
Then user management
Then project base
Then work item base
```

Bad:

```text
Build the entire user / project / test / AI / CI stack at once
```

## Review Principles

- Read the spec before implementation.
- Compare implementation against the spec after changes.
- Do not break documentation consistency for convenience.
- Keep tests, specs, and code semantics aligned.

