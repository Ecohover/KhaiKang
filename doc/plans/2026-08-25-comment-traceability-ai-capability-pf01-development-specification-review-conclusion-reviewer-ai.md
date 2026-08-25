# Development Specification Review Conclusion：PF-01 AI Comment owner-derived scope

## Review Result

**Returned with one finding.** 本次為新標準 remediation track 的 `Artifact Review`，只審查 `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`。PF-01 既有 owner-derived scope 問題已由 PM／Human PO 建立新 Problem Family，因此本 finding 可接受一次精確 Development artifact correction；這不是回到先前的 Implementation Review direct-correction loop。

## Finding

### PF01-DS-ROLE-ADMISSION — `CanCreate` lacks an implementable fixed-role target admission rule

- **Severity**: P1
- **Evidence**: Development Specification lines 63–82 correctly要求 list/create 共用 fresh `ResourceActorSnapshot`，並以 `ScopeAccountId` 作 membership subject；但 lines 102–107 只說 Project resource 的 `CanCreate` 由「existing `comment.create`／fixed-role policy」決定，lines 111–116 對 Test resource 同樣未定義 AI fixed-role target admission。現行 `ProjectCommentResourceDirectory` 與 `TestCommentResourceDirectory` 的 access contract 仍只有 `accountId`，只依既有 member/permission 或 Workspace role 決定 `CanCreate`；沒有可重用的 fixed-role／assignment／review-context verdict。
- **Specification conflict**: 產品規格 §9 不只要求 owner-derived Project／Workspace intersection，也限制 Development AI 只能在自己的 Development Task、QA AI 在被指派 Test Issue/testing context、Reviewer AI 只能在對應 Development Task 留 Review Comment；PM／DevOps 也僅限各自允許的 in-scope Comment context。PM 已接受的 PF01-QA-01 role-aware target matrix 與 representative Reviewer denied target 亦要求同一行為。若設計只傳 `ScopeAccountId` 或保留「existing policy」的抽象說法，owner membership 仍可能使受限角色在不允許的可讀資源建立 Comment。
- **One direct artifact correction**: Development AI 必須在 PF-01 Development Specification 補上 resource-owner `Comment create admission` 的具體 contract／decision order：
  1. `CanRead` 維持 human 與所有有效 AI 在 owner-derived Project／Workspace scope 的 resource-read verdict；
  2. `CanCreate` 對 human 保持既有 `comment.create` 行為，對 AI 必須同時要求 fresh actor、owner scope、fixed-role allowlist 與 role-specific target/context verdict；
  3. 明確指出 ProjectManagement／TestManagement 各自如何以 authoritative application seam 判定 Development assignment、QA Test Issue/testing context、Reviewer review-context，以及 PM／DevOps 的允許 Comment context；不得由 CommentManagement、client 或 caller-supplied role/owner 推論；
  4. 未知／缺少 AI fixed role 或無法取得 required target context 必須 fail closed 為 non-disclosing `CanCreate=false`／既有 `404`；並維持 list 的 `CanRead=true, CanCreate=false` 情境；
  5. 對應更新 implementation slices 與 focused verification plan，使 accepted `PF01-QA-01` 的五角色 allow matrix及 Reviewer readable-but-create-denied row 成為 Development-owned HTTP seams，而不是只保留 QA automation 設計。

這是唯一一次對 `PF01-DS-ROLE-ADMISSION` 的直接 correction；再次 recheck 若同 family 仍不完整，Reviewer 必須升 PM AI，不得再直接交辦 Development AI。

## Independently Confirmed Alignment

- Identity owns fresh, server-derived actor/scope context；AI disabled、orphaned、inactive owner與 transfer/revoked token 均 fail closed，human session 不被轉換為 owner scope。
- CommentManagement 不讀取 Project／Test DbContext；resource owner 只回傳最小 verdict，未授權在 Comment query/idempotency reservation 前拒絕，維持 `401`／non-disclosing `404` boundary。
- `AccountId` 仍是 AI author與 idempotency subject，`ScopeAccountId` 僅為 membership subject；不會讓多個 AI 帳號共用 idempotency namespace。
- 提案的 `CommentResourceAccessQuery` 是具名稱的 cross-module parameter object，並且 Identity contract 保持在 Identity domain，符合 module ownership／dependency direction與 public-boundary clarity。
- 無 wire contract、OpenAPI、TypeScript、schema或migration impact 的判定與本 PF-01 scope 相符；若實作需要其中任一項，須停止並升 PM。

## Review Limits

- 已閱讀並對照產品規格 §4、§5、§9、§12；PF-01 PM disposition／Parallel Design Handoff；PF-01 Development Specification；accepted PF-01 QA cases；PM Artifact Review；Canonical backend and HTTP contract profiles；以及現有 actor/comment resource contracts。
- 本次沒有執行 build、test、runtime、Formal QA 或任何產品實作變更。
- PF-02、PF-03、PF-04 及其已關閉 recheck 明確不在範圍；既有未追蹤 planning drafts 未變更。

## Handoff Package

- **Task / Feature**: PF-01 AI Comment owner-derived Project／Workspace scope remediation.
- **Current Phase**: `Artifact Review`.
- **Source-of-Truth Paths**: `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-artifact-review.md`; `AGENTS.md`; `.ai/backend.md`; `.ai/contract.md`.
- **Scope / Non-Goals**: Development Specification review only; no code, test automation, migration, OpenAPI, deployment, push, Formal QA, or phase advance.
- **Produced Artifact or Diff**: This persistent Reviewer Development Specification review conclusion only.
- **Validation Evidence**: Independent document, architecture, and existing-contract inspection completed; no executable validation was run in Parallel Design or by Reviewer.
- **Open Decisions / Blockers**: PF01-DS-ROLE-ADMISSION must receive the one direct Development artifact correction. It must use existing authoritative role/target contexts; if such a context is unavailable without new product semantics, Development must stop and return that specific gap to PM.
- **Next Role and Required Action**: Development AI may correct only PF01-DS-ROLE-ADMISSION in the Development Specification and return a recheck Handoff. PM AI must receive any unresolved product-context gap. Reviewer AI will recheck only this family once; it does not issue an implementation or Formal QA handoff.
