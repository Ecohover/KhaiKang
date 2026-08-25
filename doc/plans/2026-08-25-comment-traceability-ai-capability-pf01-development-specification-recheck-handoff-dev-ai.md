# PF-01 Development Specification Recheck Handoff：PF01-DS-ROLE-ADMISSION

## Handoff Package

- **Workflow Role**：Development AI
- **Task / Feature**：PF-01 AI Comment owner-derived Project／Workspace scope remediation；`PF01-DS-ROLE-ADMISSION` Development artifact correction。
- **Current Phase**：`Artifact Review`。本次已使用此Problem Family唯一一次direct correction；未推進phase。
- **Applicable Technology Profiles**：`csharp-dotnet-aspnet-core`、`http-contract-quality`。

## Source-of-Truth Paths

1. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-review-conclusion-reviewer-ai.md`
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`，accepted `PF01-QA-01`
4. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-artifact-review.md`
5. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`
6. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12
7. `doc/zh-TW/02-specs/04-issue-test-traceability.md`

## Scope / Non-Goals

只修正Reviewer指定的`PF01-DS-ROLE-ADMISSION`：補齊resource-owner Comment create admission contract與decision order、human／AI分流、五個fixed-role target matrix、Project assignment／review context、Test testing context、missing-context fail-closed，以及對應implementation slices與Development-focused HTTP verification plan。

沒有實作產品code或test automation；沒有修改OpenAPI、TypeScript contract、schema、migration、frontend、PF-02～PF-04或既有planning drafts；沒有commit、push、deploy、Formal QA或phase advancement。

## Produced Artifact or Diff

- 修訂：`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`
- 新增：本persistent recheck handoff。

Correction明確固定：

1. human維持既有resource-owner`comment.create`／Workspace writable-role行為。
2. AI create依序要求fresh actor、owner-derived read scope、known fixed role、role target allowlist與authoritative target/context verdict。
3. ProjectManagement以Issue type、assignee、active`tests` relation與Identity fresh assignee actor判定Development／QA／Reviewer／PM／DevOps target context。
4. TestManagement以Case immutable source、Plan current Test Issue、Run immutable Test Issue snapshot與active Workspace–Project link判定testing context，再透過Project-owned named boolean seam確認assigned Test Issue。
5. missing／unknown／inactive context只fail closed create；若read scope仍成立則保留`CanRead=true, CanCreate=false`，POST維持non-disclosing`404`且不建立idempotency reservation。
6. Development verification將accepted `PF01-QA-01`五個allowed rows與Reviewer readable-but-create-denied row納入public HTTP seams。

## Validation Evidence

- 已逐條對照Reviewer finding與accepted `PF01-QA-01` role-aware matrix。
- 已唯讀確認現有authoritative data：ProjectManagement擁有Issue type、`AssigneeAccountId`與`tests` relation；TestManagement擁有`TestCaseSourceLink`、Plan `TestIssueId`、Run Test Issue snapshot及Workspace–Project link；Identity擁有fresh `ResourceActorSnapshot`。
- 設計使用named application seams與boolean verdict，不讓CommentManagement或client讀取／推論owner module metadata，不形成DbContext／entity跨module依賴。
- 本次是artifact correction，未執行build、test、runtime或Formal QA；不宣稱任何 executable pass。

## Open Decisions / Blockers

目前無blocker，也未發現需要新增Reviewer assignment、Issue subtype、public contract或schema的產品語意缺口。`PF01-DS-ROLE-ADMISSION`已使用唯一直接修正；若recheck仍出現同family finding，Reviewer必須升PM AI，不得再次直接交辦Development AI。

## Next Role and Required Action

Reviewer AI只recheck `PF01-DS-ROLE-ADMISSION`：確認create admission decision order、Project／Test owner seams、five-role allow matrix、readable-but-not-creatable fail-closed與Development-focused verification coverage。Reviewer不得發出implementation、Formal QA或phase advancement；recheck結果回PM AI。
