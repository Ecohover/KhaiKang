# PM Artifact Review：PF-01 AI Comment owner-derived scope

## Review Scope

- Workflow Role：PM AI
- Current Phase：PF-01 remediation track `Parallel Design`
- Reviewed artifacts：
  - `2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`
  - `2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`
- Product source：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。

## Conclusion

### Development Specification — Accepted for Reviewer review

Development Specification correctly preserves the confirmed semantic boundary:

- `AccountId` remains the AI actor／Comment author／idempotency subject; `ScopeAccountId` is only the owner-derived membership subject.
- List and create share one fresh Identity-owned actor context and resource-owner verdict; no caller-supplied owner／role／scope is trusted.
- Project／Workspace intersection, inactive/orphan/transfer states, human-session regression and `404` non-disclosure are explicitly covered.
- No public HTTP contract, schema, migration or frontend behavior change is inferred. Any discovery requiring one must return to PM.

### QA Cases — Returned once for targeted correction

**Problem family PF01-QA-ROLE-TARGET**

`PF01-QA-01` currently asks all five fixed roles to create/list Comment on one generic Project Issue. This is too broad against the confirmed fixed-role matrix: Development AI is limited to its assigned Development Task, Reviewer AI to its corresponding Development Task review Comment, QA AI to its assigned Test Issue/testing context, and PM／DevOps must use their respectively permitted in-scope context. The test must not accidentally imply that every role can comment on every Issue subtype.

This is a QA design-artifact precision correction, not a change to product semantics or PF-01 owner-derived scope. It is the one permitted direct correction for this Problem Family.

## Required QA Correction

Replace PF01-QA-01 with a role-aware, parameterized target matrix that:

1. keeps the purpose intact: every allowed role’s AI Bearer create then list must use the same owner-derived scope;
2. selects an existing role-matrix-allowed target for each role and includes assignment preconditions where required;
3. separately asserts a representative fixed-role-denied Comment target remains denied, without weakening PF01-QA-02 through PF01-QA-16;
4. does not introduce a new role permission, Issue subtype rule, public error code or external AI Skill behavior.

## PM Checkpoint Status

- PF-01 QA artifact recheck: **accepted**. QA completed the one permitted correction in `PF01-QA-01`: its role-aware target matrix now uses an allowed target and necessary assignment/review-context precondition for each fixed role, and separately verifies Reviewer AI cannot create on an otherwise readable non-corresponding User Story. The correction retains the owner-derived scope objective and introduces no product semantic change.
- PF-01 Development Specification can proceed to Reviewer Development Specification review after the PM issues its handoff.
- PF-01 Parallel Design remains open until Reviewer completes the Development Specification review; QA design work is complete.
- PF-02／PF-03／PF-04 remain in their separate Implementation Review recheck; this artifact review does not advance either track or Formal QA.

## Next Role and Required Action

Reviewer AI reviews the accepted PF-01 Development Specification for omissions, conflicts, technical feasibility, architecture and standards alignment. The review must not implement code, run Formal QA, or change product semantics. If a Development Specification finding requires correction, apply the one-direct-correction rule for its new Problem Family.
