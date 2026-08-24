# Work Handoff：Comment、追溯與 AI 操作紀錄能力（Parallel Design）

## Objective

依 Human PO 已確認的產品規格，產出可進 Artifact Review 的 Development Specification 與 QA Cases；本交接不授權產品實作、Migration、測試實作、commit、push、部署或 phase transition。

## Accepted Decisions

- 唯一產品規格來源為 `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`；舊的 13 準備草案不具產品授權，14 Skill 指南待 KhaiKang 完成後更新。
- KhaiKang 提供可授權、可追溯的 API 與紀錄能力；Skill workflow、角色切換、Grill、`auto` 與 orchestration 不在本次。
- AI 帳號、token、固定角色、Comment、追溯關聯、不可變歷史來源、軟刪除、Markdown、idempotency 與前端辨識規則，均以產品規格第 3 至 12 節為準。

## Current State

- Branch：`ecohover/feature/20260820`。
- Working tree 已有其他未提交 Project Management 修改與舊 PM 準備文件；不得覆寫、混入或提交它們。
- 本交接只啟動 `Parallel Design`。Development 與 QA 可各自撰寫設計／案例 artifact，但不得開始 implementation。

## Work Packages

### Development AI

建立或更新 Development Specification，提出：模組責任、OpenAPI-first contract、資料模型／migration 切片、AI 帳號與 token 安全邊界、固定角色授權矩陣、跨 Project／Workspace access contract、前端切片、idempotency persistence 與可驗證的技術風險。遇到未定產品語意必須回報 PM，不得自行決定。

### QA AI

從唯一產品規格推導完整 QA cases、風險矩陣與既有 regression selection，特別涵蓋 AI account／token lifecycle、API/human 操作來源、權限、Comment、追溯、軟刪除、idempotency、Markdown 與前端 AI 標記。不得寫 automation、執行 Formal QA 或修改產品程式。

## Verification

- PM 已完成 Human PO Grill 與決策收斂。
- 已讀取產品規格、AI Skill 整合指南、既有 Traceability 規格／資料模型、IAM 規格、workflow template 與 repository instructions。
- 未執行 build、test、runtime、Docker 或 deployment 驗證。

## Next Role and Required Action

Development AI 與 QA AI 完成各自 artifact 並回交 PM。PM 先審 QA cases，再把 Development Specification 交 Reviewer AI 進行 Artifact Review；未收到下一份 PM Handoff 前，任何角色不得進入 Parallel Implementation。

## Context To Load

- `AGENTS.md`
- `.ai/INDEX.md`，再依 task 載入對應 context
- `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`
- `doc/zh-TW/02-specs/01-user-authentication.md`
- `doc/zh-TW/02-specs/04-issue-test-traceability.md`
- `doc/zh-TW/03-data-model/05-issue-test-traceability-data-model.md`
- `doc/zh-TW/04-planning/14-ai-skill-khaikang-integration-guide.md`
- 本交接包
