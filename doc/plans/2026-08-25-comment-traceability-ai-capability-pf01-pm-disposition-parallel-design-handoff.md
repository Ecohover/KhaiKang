# PM Disposition 與 Parallel Design Handoff：PF-01 AI Comment owner-derived scope

## PM Checkpoint

- Workflow Role：PM AI
- PO disposition：Human PO 已於 2026-08-25 同意依既有產品規格 §9 開啟新的標準 remediation delivery，修正 PF-01。
- Rationale：PF-01 是已確認語意未完整實作，不是新產品需求；但它已用盡原 Implementation Review 的 direct correction，故不得回到同一 Reviewer-to-Development 修正迴圈。
- Track boundary：本文件只管理 PF-01。PF-02、PF-03、PF-04 仍依 `2026-08-25-comment-traceability-ai-capability-development-remediation-handoff.md` 的一次指定修正進行。

## Task / Feature

讓 AI Bearer 的 Comment read 與 Comment create 一致套用 owner-derived Project／Workspace scope，並以可重用的 actor/scope seam 防止相同權限問題再次發生。

## Current Phase

新標準 remediation track 的 `Parallel Design`。本 handoff 僅授權設計 artifact，不授權 PF-01 產品程式、migration、OpenAPI、測試 automation、commit、push、deploy 或 Formal QA。

## Source-of-Truth Paths

1. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md`（PF-01 evidence）。
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-remediation-intake.md`。
4. `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`。
5. `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`。
6. `AGENTS.md`、`.ai/INDEX.md`，以及 `.ai/backend.md`、`.ai/contract.md`。

## Confirmed Behavior

所有 AI 操作均以 current AI actor、固定角色與 owner-derived Project／Workspace scope 的交集判定。Comment create 已採此方式；Comment list 必須採同一 actor/scope interpretation，不能以 AI account ID 直接當成 Project 或 Workspace 成員資格。

| Scenario | Expected result |
| --- | --- |
| AI Bearer、active AI account、owner 對目標 Project／Workspace 有 read scope | 可依固定角色與資源規則列出 Comment。 |
| owner 沒有任一必要 Project／Workspace scope | 不可讀取 Comment，且不得洩漏資源或留言 metadata。 |
| AI account disabled、orphaned，或 owner transfer 後既有 token | 不可再透過 Bearer 讀取 Comment。 |
| human session | 維持既有人類 read scope 與 cookie/CSRF 邊界，不因本修正改變。 |

## Parallel Design Assignments

| Role | Required output |
| --- | --- |
| Development AI | PF-01 Development Specification：界定 single actor/scope seam 的擁有 module、Comment read/create 如何共同取得 effective actor context、跨 Project／Workspace 的最小 access verdict、錯誤不洩漏策略，以及必要 contract／migration impact。不得實作。 |
| QA AI | PF-01 QA cases：從上述 confirmed behavior 導出 AI Bearer allow/deny、owner Project/Workspace intersection、owner transfer／disable／orphan、human regression 與無 metadata leakage cases。不得實作 automation 或執行 Formal QA。 |
| PM AI | 收到兩份設計 artifact 後，對照產品規格完成 Artifact Review，發出 Reviewer Development Specification review handoff。 |
| Reviewer AI | 此 phase `Not Involved`，等待 PM 的 Artifact Review handoff。 |

## Non-Goals / Guardrails

- 不改 AI 固定角色矩陣、token lifecycle、Comment whitelist、idempotency 語意或外部 AI Skill。
- 不以 owner 的 System Admin 高權限繞過 AI fixed role 或 Project／Workspace intersection。
- 不將先前 PF-01 當作第二次 direct correction；後續實作必須有新的經確認設計與 handoff。
- 不進入 Formal QA、不 push、不 deploy、不宣告驗收。

## Next Role and Required Action

Development AI 與 QA AI 並行產出上述 PF-01 設計 artifact 與 Handoff Package，交回 PM AI。PM 不會在兩者 artifact 到齊前推進本 remediation track。
