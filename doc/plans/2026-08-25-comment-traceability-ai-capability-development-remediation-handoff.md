# Development Remediation Handoff：Implementation Review PF-02、PF-03、PF-04

## Task / Feature

Comment、追溯與 AI 操作紀錄能力的 Implementation Review 修正：僅 PF-02、PF-03、PF-04。

## Current Phase

`Implementation Review` 的一次指定 direct correction。適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`。

## Source-of-Truth Paths

1. `AGENTS.md`、`.ai/INDEX.md`，以及適用的 `.ai/backend.md`、`.ai/frontend.md`、`.ai/contract.md`、`.ai/verification.md`。
2. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`。
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md`。
4. `doc/plans/2026-08-25-comment-traceability-ai-capability-remediation-intake.md`。
5. `doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-handoff.md`。

## Authorized Scope

### PF-02 — Final migrations

For ProjectManagement and TestManagement, retain exactly one final unpublished migration per affected DbContext, regenerate snapshots, and prove the complete chain applies on clean disposable PostgreSQL. Do not rewrite any published/shared migration; if that status cannot be established, stop and return the evidence to PM AI.

### PF-03 — Token success notice

After a successful token create or re-create, call `useSaveNotice` while retaining `UiActionDialog` for the one-time plaintext token acknowledgement. Add focused frontend coverage.

### PF-04 — Persistent token key source

Make a persistent protected-key source required and startup-validated outside Testing. Record only non-secret configuration guidance. Add focused evidence that key-ring continuity across restart still permits the authorized owner token recovery path; do not record plaintext key or token in source, migration, logs, audit, test output, or documentation.

## Non-Goals and Boundaries

- PF-01 is explicitly excluded: it is escalated to PM/human PO after exhausting its direct correction limit.
- No changes to product semantics, public contract unless an unavoidable incompatibility is first reported to PM, external AI Skill, workflow automation, role matrix, Formal QA, deployment, push, or release.
- Preserve unrelated untracked planning drafts. Use explicit staged file lists; do not use `git add -A`.

## Validation Evidence Required

Report exact commands and results, including focused tests for every changed behavior, clean PostgreSQL migration-chain evidence for PF-02, restart/key-ring continuity evidence for PF-04, and applicable backend build / frontend type-check / test / build / contract checks. Clearly state anything not run.

## Next Role and Required Action

Development AI performs only the three scoped corrections, creates coherent local commits when each slice is complete and verified, and returns a persistent remediation handoff with evidence to Reviewer AI for one recheck. If a finding recurs for the same Problem Family, stop and escalate to PM AI; do not make a second direct correction.
