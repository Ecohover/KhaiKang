# PM Checkpoint 與 Parallel Implementation Handoff：PF-01 AI Comment owner-derived scope

## PM Checkpoint

- Workflow Role：PM AI
- Checkpoint：PF-01 remediation track 的 `Parallel Design` 與 `Artifact Review` complete。
- Evidence：QA cases已接受；Reviewer `PF01-DS-ROLE-ADMISSION` recheck 於 `090d4b5` CLOSED；沒有未決產品語意、schema、migration或public contract blocker。
- Authorization：只授權依既有產品規格與已審查 Development Specification 實作 PF-01，不授權改變產品語意、外部 AI Skill、OpenAPI、schema、migration、push、deploy、Formal QA或驗收宣告。

## Task / Feature

修正 AI Bearer Comment list/create 的 owner-derived scope與固定角色 Comment target admission，使 list/create使用同一fresh actor context及resource-owner verdict。

## Current Phase

`Parallel Implementation`。適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`http-contract-quality`。

## Source-of-Truth Paths

1. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`。
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`。
4. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-recheck-conclusion-reviewer-ai.md`。
5. `AGENTS.md`、`.ai/INDEX.md`、`.ai/backend.md`、`.ai/contract.md`、`.ai/verification.md`。

## Authorized Implementation Scope

- Identity-owned fresh `ResourceActorSnapshot` is the sole actor/scope seam. `AccountId` remains AI authorship/idempotency identity; `ScopeAccountId` is used only for owner-derived membership scope.
- `CommentService.ListAsync` and `CreateAsync` use the same `CommentResourceAccessQuery` and resource-owner `CommentResourceAccess { CanRead, CanCreate }` verdict.
- ProjectManagement implements role-aware Comment target admission using authoritative Issue type, assignment and `tests` relation context. TestManagement implements QA testing-context admission from Case source／Plan Test Issue／Run snapshot plus Workspace–Project intersection, using only the narrow public Project boolean seam.
- Preserve `CanRead=true, CanCreate=false` for fixed-role/context denial; POST remains non-disclosing `404` before idempotency reservation. Human behavior, cookie/CSRF boundary, existing OpenAPI, schema and migrations remain unchanged.

## Parallel Role Assignments

| Role | Required action and output |
| --- | --- |
| Development AI | Implement the reviewed slices and Development-owned HTTP integration tests. Produce focused build/test/format/contract compatibility evidence and a persistent Implementation Review Handoff. |
| QA AI | Implement the approved PF01-QA automated HTTP regression cases and selected legacy regressions. Report test code/evidence to PM for Implementation Review; do not execute Formal QA. |
| Reviewer AI | Not involved until PM issues the Implementation Review Handoff. |
| PM AI | Receive both implementation handoffs and issue the next Implementation Review Handoff; do not advance to Formal QA. |

## Required Verification

- Five-role allowed target matrix; Reviewer readable-but-create-denied; missing/unknown context fail-closed; Test Case/Plan/Run QA testing contexts.
- Owner Project／Workspace intersection allow/deny, System Administrator non-bypass, membership/link change re-evaluation, owner transfer/new-owner scope, disabled/orphan/revoked actor, and human cookie/CSRF regression.
- Unauthorized/missing/soft-deleted outcomes preserve `401`／non-disclosing `404` boundaries and do not create idempotency records on denied create.
- Backend restore/build, focused HTTP integration tests, relevant legacy regression, and targeted format verification. OpenAPI artifact must remain zero-diff and existing operation mapping must be checked.

## Guardrails

- Explicit staged paths only; do not use `git add -A` or mix the two untracked planning drafts.
- If implementation needs a changed product semantic, public contract, schema/migration, a new Reviewer assignment relation, or an unsupported Test resource context, stop and return evidence to PM; do not infer a workaround.
- Commit coherent, verified slices only. Development/QA evidence is not Reviewer approval or Formal QA.

## Next Role and Required Action

Development AI and QA AI begin the specified parallel implementation work. On completion, both return persistent Handoff Packages to PM AI; PM then decides whether an Implementation Review Handoff can be issued.
