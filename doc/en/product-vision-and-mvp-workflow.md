# Product Vision and MVP Workflow

## Product position

KhaiKang is an open-source, self-hosted development and test management platform for individual developers and small-to-medium software teams. It manages requirements, work items, tests, quality gates, environment promotion, and delivery evidence while external tools continue to execute builds, tests, and deployments.

KhaiKang is Kanban-first and Scrum-compatible. AI may assist at every stage, and every transition may optionally require automatic checks or human approval.

## Delivery principles

- AI proposes; humans approve when required.
- Feature code and test automation are developed in parallel.
- Tests remain traceable to requirements, artifacts, and environments.
- Build once and promote the same immutable artifact.
- Environment stages define entry conditions, actions, tests, exit conditions, approvals, and failure policies.
- Start with templates and preserve future configurability.
- Support a linear MVP while keeping the model graph-ready.

## Target workflow

1. Record a requirement.
2. Analyze the requirement and relevant repository context with AI.
3. Propose the technical approach and framework changes.
4. Generate acceptance criteria, development tasks, and test cases.
5. Optionally require human approval.
6. Develop feature code and tests in parallel.
7. Build an immutable artifact and run early checks.
8. Deploy and verify that artifact in each configured environment.
9. Evaluate quality conditions and optional approvals before promotion.
10. Generate requirement-level quality evidence.
11. Promote the approved artifact to production and verify service health.

## Environment templates

The default full template is:

```text
DEV -> SIT -> UAT -> PRE-PROD -> PROD
```

Each stage has its own purpose and conditions. Test types are not permanently bound to an environment.

MVP templates:

- Individual or small project: DEV -> PROD
- Standard team: DEV -> SIT -> UAT -> PROD
- Full delivery: DEV -> SIT -> UAT -> PRE-PROD -> PROD

## MVP scope

The MVP includes projects, requirements, issues, Kanban, test cases and runs, AI-assisted analysis, linear environment stages, environment templates, basic quality conditions, optional human approval, generic webhooks, basic Jenkins integration, and requirement-level quality reporting.

A visual workflow designer, arbitrary branches, a custom expression language, complex approval matrices, full rollback orchestration, and autonomous multi-agent execution are deferred.
