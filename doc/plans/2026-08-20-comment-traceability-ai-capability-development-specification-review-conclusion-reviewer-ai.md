# Development Specification Review Conclusion：Comment、追溯與 AI 操作紀錄能力

## Review Result

**結論：Returned for one direct correction.**

本次是 `Artifact Review`，Reviewer AI 已獨立比對唯一產品規格、指定的既有 IAM／追溯基線、QA cases、現有 OpenAPI／模組定位，以及 `csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`。Development Specification 的模組資料所有權、Controller/application-service 邊界、Bearer 與 cookie/CSRF 分離方向、PostgreSQL 驗證要求，以及 Comment 前端互動方向均可延續；但下列 finding 必須由 Development AI 在本次唯一直接修正中處理，才可交回 Reviewer 複查。

## Findings

### DS-01 — Confirmed product rules were incorrectly left open

- **Problem Family**: Spec alignment / confirmed authorization and traceability semantics
- **Severity**: P1
- **Evidence**:
  - Development Specification lines 58–60 表示 fixed-role permission matrix 尚待 PM／Human PO 確認，並拒絕推定各角色的可寫入範圍；產品規格 §9 已明列五個固定角色的逐項可寫入範圍，且所有角色在自身範圍皆可讀取與建立 Comment。
  - Development Specification lines 87、115、130 將 `BugFoundInTestRunItem` 描述為「immutable-vs-removable」衝突；產品規格 §7 明定建立後不可解除，§12 亦明定可建立、查詢、**不可解除**。QA case `TRC-04` 已正確反映此規則。
- **Required correction**: 將產品規格 §9 的固定角色矩陣轉成 stable permission mapping／operation authorization design 與對應驗證；移除 Run Item link 可解除的待決描述，明確禁止其 delete、replace、soft-delete 或其他解除途徑，並將 verification plan 對齊 QA `TRC-04`。
- **Why this blocks**: 若以待決處理，實作可能錯誤開放 AI mutation 或可移除的歷史證據，直接違反已確認產品語意。

### DS-02 — Administrator token authority exceeds the confirmed security boundary

- **Problem Family**: Token lifecycle authorization and plaintext disclosure
- **Severity**: P1
- **Evidence**: Development Specification line 39 讓 owner 或 administrator 都可「重建」token；產品規格 §9 僅授權 owner 建立／重建及查看／複製 token。administrator 僅能啟用、停用、撤銷與轉移帳號，且不得查看既有 token 明文。QA case `AI-06` 也以此為預期。
- **Required correction**: 將 token lifecycle 操作切成明確、可審查的 owner 與 administrator authorization matrix；只有 owner 的 create/re-create/view/copy response 可以含 token plaintext，administrator 的 enable/disable/revoke/transfer 永不產生或回傳 plaintext。將此矩陣映射至 OpenAPI operation、Controller policy 與 audit tests。
- **Why this blocks**: 現有文字會容許 administrator 以重建取得新 token，實質繞過「不得查看明文」的產品安全限制。

### DS-03 — Idempotency persistence boundary is not implementable as written

- **Problem Family**: Cross-module ownership and transactional idempotency
- **Severity**: P1
- **Evidence**: Development Specification line 78 要求 reservation、mutation、completion 在同一 transaction 內原子化，卻把 idempotency record entity owner 留為「PM／Reviewer 決定」；同文件又禁止 CommentManagement 寫入其他 module tables。這使 Comment、ProjectManagement、TestManagement 的 schema owner、DbContext／transaction boundary、unique constraint 和 concurrent replay evidence 無從確定。
- **Required correction**: Development AI 必須在設計文件選定並說明一個符合 module ownership 的實作邊界。若使用 module-local records，需說明各 owner 如何在自身 DbContext transaction 內保證 logical key、fingerprint conflict 與 replay；若提議 shared component，需以穩定且跨三個實際 caller 的領域中立名稱、清楚 contract、migration owner 與 dependency direction 證明其符合 Feature-Local First／Dependency Rule。這是技術設計決策，不應列為待 PM／Human PO 的產品決策。
- **Why this blocks**: 沒有 owner 就無法保證規格要求的 transaction atomicity、concurrent idempotency 與 PostgreSQL evidence，也無法開始 migration/OpenAPI 實作。

## Scope and Non-Goals

- 僅審查 Development Specification；未審查或執行產品實作、QA automation、Migration、build、test、runtime、Docker、deployment、commit 或 push。
- 不重新定義產品行為。DS-01 與 DS-02 直接依已確認的產品規格及已完成 QA case semantic baseline 提出。
- 本輪為上述每個 Problem Family 的第一次、也是唯一一次直接退回；相同 Problem Family 在修正後仍未關閉時，Reviewer AI 必須停止直接迴圈並將證據交 PM AI。

## Validation Evidence

- 已讀取：`AGENTS.md`、`.ai/INDEX.md`、`.ai/handoff-template.md`、Artifact Review Handoff、唯一產品規格、Development Specification、QA cases、既有 IAM／追溯規格及資料模型。
- 已檢視：`contract/openapi/khaikang.v1.yaml` 的既有 API／Problem Details／page convention、現有 module 與 permission 定位；未變更任何程式或 contract。
- 未執行 build、test、format、runtime、Docker 或 deployment；這些不屬於本 Artifact Review，也不構成 Formal QA。

## Handoff Package

- **Task / Feature**: Comment、追溯與 AI 操作紀錄能力。
- **Current Phase**: `Artifact Review`。
- **Source-of-Truth Paths**: `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`; `doc/zh-TW/02-specs/01-user-authentication.md`; `doc/zh-TW/02-specs/04-issue-test-traceability.md`; `doc/zh-TW/03-data-model/05-issue-test-traceability-data-model.md`.
- **Scope / Non-Goals**: 獨立設計審查與本結論；不實作、不執行 QA、不變更產品規格、不推進 phase。
- **Produced Artifact or Diff**: 本 Development Specification Review Conclusion；沒有產品程式、測試、Migration、OpenAPI 或 deployment diff。
- **Validation Evidence**: 見上節；文件與現有 contract/module 的唯讀比對完成，未執行可執行驗證。
- **Open Decisions / Blockers**: DS-01、DS-02、DS-03 必須修正。產品規格已決定的 role matrix 與 Run Item immutability 不是 open decision；token key-management operational profile 可在修訂後保留為受技術 profile／部署設定約束的 implementation detail，但不得阻擋已明確的權限矩陣。
- **Next Role and Required Action**: `Development AI` 僅修訂 Development Specification 以關閉 DS-01～DS-03，並附更新後 Handoff 給 Reviewer AI 複查。若任一相同 Problem Family 再出現，直接交 `PM AI`，不得進行第二次直接修正。PM AI 在 Reviewer 通過後才可發出 `Parallel Implementation` Handoff。
