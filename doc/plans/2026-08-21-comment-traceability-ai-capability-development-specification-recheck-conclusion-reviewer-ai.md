# Development Specification Recheck Conclusion：Comment、追溯與 AI 操作紀錄能力

## Recheck Result

**結論：DS-01、DS-02 closed；DS-03 remains open and is escalated to PM AI.**

本次僅複查先前 Reviewer conclusion 的 DS-01～DS-03，且本次修訂是每個 Problem Family 唯一允許的直接修正。因此 DS-03 不可再退回 Development AI 作第二次直接修正，必須由 PM AI 依 Remediation Cycle Limit 處理。

## Recheck Evidence

### DS-01 — Closed

修訂後的 Development Specification：

- lines 58–70 把產品規格 §9 的五個 fixed roles 轉成 stable operation authorization allowlist、明確禁止項目及 OpenAPI／Controller／frontend／focused-test 對齊要求；不再將矩陣列為 PM／Human PO 的 open decision。
- lines 112–115 明確規定三種 Bug source link（包含 `BugFoundInTestRunItem`）建立後不得 delete、replace、soft-delete、unlink 或以任何方式解除，並與產品規格 §7、§12 和 QA `TRC-04` 對齊。

此修訂已關閉「已確認角色／追溯語意被誤列為待決」Problem Family。

### DS-02 — Closed

修訂後的 Development Specification lines 39、72–84 建立了 token lifecycle matrix：

- owner 才可 create、re-create、view、copy plaintext token；
- administrator 僅可 enable、disable、revoke、transfer，且永不產生、回傳、綁定或渲染 plaintext token；
- 每一種 operation 均要求 OpenAPI、Controller policy、application command、audit test 對齊。

此修訂符合產品規格 §9 與 QA `AI-06`，已關閉 administrator 可藉 re-create 取得 token 的 Problem Family。

### DS-03 — Still open; escalate to PM AI

- **Problem Family**: Cross-module ownership and transactional idempotency.
- **Severity**: P1.
- **Closed portion**: lines 102–106 已選定 module-local records，並指定 CommentManagement、ProjectManagement、TestManagement 各自擁有 table、DbContext、single PostgreSQL transaction、unique constraint 和 replay boundary；這一部分已足以消除先前的 owner ambiguity 與跨 DbContext 寫入問題。
- **Remaining evidence**: line 104 同時規定「失敗交易完整 rollback reservation 與 mutation，讓後續合法重試可重新取得 reservation」。這不保留第一次使用 idempotency key 的 failed outcome／fingerprint。呼叫端因此可以先以某一 target、payload 或 version 使用 key 並觸發 failure，再以不同 target、payload 或 version 重新取得 reservation、執行新 mutation。
- **Specification conflict**: 產品規格 §4 要求同帳號、操作類型與 idempotency key 的重試回傳原結果；同 key 若帶不同 target、payload 或 version 必須回傳 idempotency conflict，且不得執行新操作。這要求已被使用的 key 即使首次結果是可預期的業務拒絕，也要有能阻止 payload substitution 的持久化語意。現行「完整 rollback reservation」未說明或保證該語意。
- **Required PM disposition**: PM AI 必須將此證據交 Human PO／適當技術決策管道，確認 idempotency key 對業務拒絕是否也必須永久（或依明確 retention policy）保留 fingerprint 與 stable failure outcome；在不改變產品規格 §4 的前提下，實作設計至少要能保證不同 fingerprint 永不因第一次失敗而重用同一 key。此為已用盡一次直接修正的同一 DS-03 Problem Family，Reviewer AI 不提出第二次 Development correction。

## Scope and Non-Goals

- 僅審查 DS-01～DS-03 reclosure、相關規格一致性與 module-local idempotency ownership／transaction boundary。
- 未實作或變更 code、tests、migrations、OpenAPI、規格、部署設定、commit、push 或 deployment。
- 未執行 build、test、runtime、Docker 或 Formal QA。

## Handoff Package

- **Task / Feature**: Comment、追溯與 AI 操作紀錄能力。
- **Current Phase**: `Artifact Review`。
- **Source-of-Truth Paths**: `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-review-conclusion-reviewer-ai.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`.
- **Scope / Non-Goals**: DS-01～DS-03 recheck only; no implementation, testing execution, product-rule change or phase advancement.
- **Produced Artifact or Diff**: 本 recheck conclusion；未產生產品 code、test、migration、contract 或 deployment diff。
- **Validation Evidence**: 已逐段比對修訂 Development Specification lines 39、58–84、102–106、112–115、140–145，與產品規格 §4、§7、§9、§12 及 QA `TRC-04`／`AI-06`；未執行可執行驗證。
- **Open Decisions / Blockers**: DS-03 的 failed-attempt idempotency reservation／fingerprint retention 語意仍未關閉。這阻擋 Artifact Review 通過與任何 `Parallel Implementation` handoff。
- **Next Role and Required Action**: `PM AI` 接收 DS-03 escalation，依 Human PO／技術決策權責處理並發出下一份明確 Handoff。Reviewer AI、Development AI 與 QA AI 在該 Handoff 前均不得把本 artifact 視為已通過或開始 `Parallel Implementation`。
