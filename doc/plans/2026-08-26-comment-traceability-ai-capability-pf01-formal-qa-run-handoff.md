# Formal QA Run Handoff：PF-01 AI Comment owner-derived scope

## Task / Feature

PF-01 AI Comment owner-derived Project／Workspace scope與固定角色 Comment target admission。

## Current Phase

`Formal QA Run`。本 handoff 在 Implementation Review 全數關閉後由 PM AI 發出；僅 QA AI 參與本 phase。

## Implementation Review Gate Evidence

- PF-01 implementation review: `96cb2fa`，核心 implementation scope CLOSED。
- PF-05／PF-06 QA recheck: `6ce7bf9`，CLOSED。
- Review documents：
  - `2026-08-25-comment-traceability-ai-capability-pf01-implementation-review-conclusion-reviewer-ai.md`
  - `2026-08-25-comment-traceability-ai-capability-pf01-qa-implementation-review-recheck-conclusion-reviewer-ai.md`

## Source-of-Truth Paths

1. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`（PF01-QA-01～16、PF01-REG-01～09）。
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`。
4. 本次兩份 Implementation Review conclusion。
5. `AGENTS.md`、`.ai/INDEX.md`、`.ai/verification.md`。

## Authorized QA Run

- 以 public HTTP seams 執行 PF01-QA-01～16，並執行選定 legacy PF01-REG-01～09。
- 明確驗證：五角色 allow matrix、Reviewer readable-but-create-denied、Project／Workspace intersection、owner transfer／disable／orphan、human cookie/CSRF、Bearer no-CSRF、metadata non-disclosure與 denied create 不消耗 idempotency key。
- 產出可追溯的 QA Run evidence（命令、結果、TRX/log 路徑、實際 commit range、環境限制）與 Bug Report（若有）。

## Known Coverage Limits to Report, Not Invent

- physical removed/missing owner lifecycle state；每個獨立 Workspace membership/link transition；wrong-CSRF distinct from missing-CSRF，仍是已知未自動化範圍。
- 這些限制不得在無觀察證據時標示為 Bug，也不得自行新增產品語意或測試資料的 private/DB workaround。

## Failure Handling

若觀察到任何失敗，QA AI 必須保留可重現證據並建立 Bug Report／QA Run Handoff 給 PM AI。QA 不得判定原因、不直接要求 Development 修正、不自行重跑修改後的程式宣告通過，或推進 Disposition。

## Non-Goals

不實作、修改產品程式、OpenAPI、schema、migration、部署、push或宣告 release／human acceptance。

## Next Role and Required Action

QA AI 執行此 Formal QA Run，回交 persistent QA Run evidence及任何 Bug Report給 PM AI。PM AI 後續只整合證據並請 human PO 決定 Disposition。
