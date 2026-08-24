# PM Disposition Handoff：DS-03 Idempotency Failure Semantics

## Task / Feature

Comment、追溯與 AI 操作紀錄能力的 DS-03：跨模組 idempotency record 與 transaction design。

## Current Phase

`Artifact Review`。這是 Human PO 已確認後的 PM disposition，不是 Reviewer 的第二次直接修正。

## Accepted Decisions

- 可預期的業務／驗證拒絕會消耗 idempotency key：同 fingerprint 重試回原失敗，不同 fingerprint 一律 conflict。
- 暫時性系統故障／未預期 5xx 不得讓不同 fingerprint 重用 key；系統保留 key 與 fingerprint，只允許同 fingerprint 重試恢復或取得最終結果。
- 成功、業務拒絕與未完成／恢復中的 request 都不得允許 payload substitution。

## Required Development Action

僅修訂 `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md` 的 module-local idempotency 設計與驗證計畫，明確描述 terminal success／terminal business rejection／incomplete transient failure 的持久化狀態、fingerprint 保護、同 transaction 邊界及 PostgreSQL concurrent evidence。不得修改產品程式、Migration、OpenAPI、測試、commit、push、部署或 phase。

## Source of Truth

- `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`
- `doc/plans/2026-08-21-comment-traceability-ai-capability-development-specification-recheck-conclusion-reviewer-ai.md`
- 本 Handoff

## Next Role

Development AI 完成後交 Reviewer AI 複查 DS-03；Reviewer 只確認此 PM disposition 是否被正確落實。未通過不得進入 `Parallel Implementation`。
