# Remediation Intake：Comment、追溯與 AI 操作紀錄能力

## Intake

- Workflow Role：PM AI
- Current Phase：`Implementation Review`
- Source：`2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md`
- Applicable Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`

## Classification

| Problem family | Classification | PM disposition |
| --- | --- | --- |
| PF-01 AI Comment read 未使用 owner-derived scope | 已確認產品規格的修正；不是新語意。 | 同一 Problem Family 已用盡 Reviewer direct correction，依 Remediation Cycle Limit 升交 human PO 決定後續 remediation route；在此之前不得交由 Development AI 再次直接修正。 |
| PF-02 Per-DbContext final migration discipline | 技術維護／standards correction；不變更產品行為。 | 授權 Development AI 進行一次指定修正與乾淨 PostgreSQL migration-chain evidence。 |
| PF-03 token create/re-create 缺 success notice | 已確認前端互動規格的修正；不變更產品語意。 | 授權 Development AI 進行一次指定修正，保留 token acknowledgement dialog。 |
| PF-04 permanent token 缺 mandatory persistent key source | 已確認 token lifecycle／安全邊界的修正；不變更產品語意。 | 授權 Development AI 進行一次指定修正，加入非 Testing 環境 startup validation 與 key-ring continuity evidence。 |

## Scope and Guardrails

- PF-02 至 PF-04 只可修正 Reviewer 指出的行為與必要測試、migration、非祕密部署設定文件；不得順帶改寫產品語意、固定角色矩陣、token plaintext boundary 或 public contract。
- PF-01 暫無 Development direct-correction authorization。它必須先由 human PO 確認下一步處置，且後續不得再以同一 Reviewer-to-Development 直接修正循環處理。
- 不進入 Formal QA、不 push、不 deploy、不宣告驗收或 release。

## Required Evidence

- PF-02：每個 affected DbContext 留一個 final unpublished migration、重新產生 snapshot，並以乾淨 disposable PostgreSQL 完整 migration chain 證明可套用。
- PF-03：token create 與 re-create 成功均呼叫 `useSaveNotice`；一次性 token acknowledgement 仍保留；補或更新 focused frontend test。
- PF-04：非 Testing 環境缺少 persistent protected key source 時 startup 必須明確失敗；文件只描述非祕密設定方式；以 restart/key-ring continuity 證明 owner 後續仍能安全取回 token，且不暴露 key 或 token。

## Next Action

PM AI 發出限縮的 Development Remediation Handoff（PF-02、PF-03、PF-04）。PF-01 等待 human PO 的 remediation disposition。
