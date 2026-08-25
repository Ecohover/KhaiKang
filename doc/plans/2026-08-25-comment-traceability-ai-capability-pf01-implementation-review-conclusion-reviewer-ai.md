# Implementation Review Conclusion：PF-01 AI Comment owner-derived scope

## Review Result

**Returned with findings.** PF-01 actor/scope admission implementation and its public HTTP seams are closed in this review. However, two new P2 Problem Families in the QA contribution remain open. They are not a recurrence of PF-01 and each receives its one permitted direct correction from Reviewer to QA.

## PF-01 Scope Review — CLOSED

- **Fresh actor/scope seam**: `CommentService` resolves `IResourceActorDirectory` before both list and create, passes one `CommentResourceAccessQuery` to the resource owner, retains `AccountId` for author/idempotency, and uses owner-derived `ScopeAccountId` only for admission. Resource denial occurs before `ReserveAsync`.
- **Fixed-role target admission**: `ProjectCommentTargetAdmission` enforces PM／Development／QA／Reviewer／DevOps Project targets; `TestCommentTargetAdmission` limits Test Case／Plan／Run create to QA AI with authoritative Case source／Plan Test Issue／Run snapshot and Project＋Workspace intersection. CommentManagement does not inspect owner-module persistence.
- **Readable-but-not-creatable and non-disclosure**: a readable target with a missing role context returns `200` and `canCreate=false`; create is `404` before idempotency reservation. Missing resource/scope remains non-disclosing `404`; inactive AI/token/owner remains authentication rejection.
- **Module and contract boundary**: Identity no longer references CommentManagement; CommentManagement consumes Identity public contracts; TestManagement reaches ProjectManagement through a narrow boolean context contract. No OpenAPI, TypeScript, schema, or migration diff was found.
- **Test seams**: Development and QA tests use HTTP integration seams for five role allow targets, Reviewer denial, Test Case／Plan／Run context, owner scope changes, disable/transfer, System Admin non-bypass, human CSRF and Bearer behavior. Supplied executable results remain Development/QA evidence; Reviewer did not rerun them. `git diff --check b19a20d..c797ca5` completed with no output.

## Findings

### PF-05 — QA persistent handoff evidence chain is broken

- **Severity**: P2
- **Evidence**: QA commit `c797ca5` deletes `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-parallel-implementation-handoff.md`, while the current PM Implementation Review Handoff still names that exact path as a Source-of-Truth artifact. This breaks the persisted QA scope, evidence, and known-limits chain needed for later PM/QA review.
- **One direct correction to QA AI**: Restore the deleted persistent QA Parallel Implementation Handoff with its original evidence/limits, or create a persistent successor and atomically update every authoritative reference to it. The resulting artifact must state the actual QA commit/test scope and retain known coverage limits. Do not alter product code, behavior, OpenAPI, schema, or migration.
- **Recurrence handling**: If the QA recheck still leaves the cited evidence chain broken, escalate PF-05 to PM AI; do not issue another direct QA correction.

### PF-06 — Public QA test class/file layout is inconsistent

- **Severity**: P2
- **Evidence**: `backend/tests/integration/KhaiKang.Api.IntegrationTests/Identity/Pf01HumanCsrfRegressionTests.cs` declares `public sealed partial class Pf01AiCommentScopeTests`, whose main declaration is in `Pf01AiCommentScopeTests.cs`. The public top-level type does not match this file and is split only to reuse private helpers.
- **Standard**: `doc/en/dotnet-development-guidelines.md` requires each top-level public class in its own matching file. This is also an avoidable test-discovery/maintenance ambiguity.
- **One direct correction to QA AI**: Either move the CSRF test into `Pf01AiCommentScopeTests.cs`, or rename this file’s class to `Pf01HumanCsrfRegressionTests` and place shared fixture helpers in an appropriately named internal test helper. Preserve the existing public HTTP assertion; do not weaken CSRF or Bearer coverage.
- **Recurrence handling**: If the QA recheck still has this file/type mismatch, escalate PF-06 to PM AI; do not issue another direct QA correction.

## Known Coverage Limits (not findings)

The supplied handoffs explicitly retain no separate automation for a physically removed owner, every individual Workspace membership/link transition, or wrong-CSRF distinguished from missing-CSRF. They are documented limits rather than a current observed regression; Formal QA remains outside this review.

## Handoff Package

- **Task / Feature**: PF-01 AI Comment owner-derived Project／Workspace scope and fixed-role Comment admission.
- **Current Phase**: `Implementation Review`.
- **Source-of-Truth Paths**: `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-implementation-review-handoff.md`; `AGENTS.md`; `.ai/backend.md`; `.ai/contract.md`; `.ai/verification.md`.
- **Scope / Non-Goals**: Independent review of `bd497dc` Development code and `c797ca5` QA automation only; no Formal QA, deployment, push, product change, or phase advance.
- **Produced Artifact or Diff**: This persistent Implementation Review conclusion only.
- **Validation Evidence**: Source/diff review and `git diff --check` completed. Supplied builds and test counts were not rerun by Reviewer.
- **Open Decisions / Blockers**: PF-05 and PF-06 require their one QA correction. No PF-01 recurrence was found.
- **Next Role and Required Action**: QA AI corrects only PF-05 and PF-06 with a persistent QA recheck Handoff. Reviewer rechecks each family once. PM AI must not issue Formal QA Run Handoff until both new families close or are escalated.
