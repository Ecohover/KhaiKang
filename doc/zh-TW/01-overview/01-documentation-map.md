# 文件導覽

## 目的

這份文件用來說明 KhaiKang 中文文件的閱讀順序與分類方式。

目前中文文件分成四個層級：

1. `01-overview`
2. `02-specs`
3. `03-data-model`
4. `04-planning`

這樣的目的是把「專案說明」、「功能規格」、「資料模型」與「階段規劃」分開，避免不同性質的資訊混在一起。

## 建議閱讀順序

第一次接觸專案時，建議依序閱讀：

1. [貢獻指南](../../../CONTRIBUTING.md)
2. [架構總覽](./02-architecture-overview.md)
3. [開發規範](./03-development-guidelines.md)
4. [.NET 開發準則](./05-dotnet-development-guidelines.md)
5. [AI 與 OpenAPI 協作開發流程](./06-ai-openapi-development-workflow.md)
6. [UI Guideline](./07-ui-guidelines.md)
7. [語詞總表](./04-domain-glossary.md)
8. [使用者與登入規格](../02-specs/01-user-authentication.md)
9. [專案管理規格](../02-specs/02-project-management.md)
10. [測試案例管理規格](../02-specs/03-test-case-management.md)
11. [Issue 與測試資產追溯規格](../02-specs/04-issue-test-traceability.md)
12. [資料模型總覽](../03-data-model/01-data-model-overview.md)
13. [階段規劃總覽](../04-planning/01-phase-overview.md)

## 文件分類

### 01-overview

用途：

- 讓閱讀者快速理解專案方向
- 統一語詞
- 建立整體閱讀地圖

目前包含：

- [架構總覽](./02-architecture-overview.md)
- [開發規範](./03-development-guidelines.md)
- [.NET 開發準則](./05-dotnet-development-guidelines.md)
- [AI 與 OpenAPI 協作開發流程](./06-ai-openapi-development-workflow.md)
- [UI Guideline](./07-ui-guidelines.md)
- [語詞總表](./04-domain-glossary.md)

### 02-specs

用途：

- 定義功能是什麼
- 定義規則、流程與責任邊界
- 不直接承載資料表細節與階段規劃

目前包含：

- [使用者與登入規格](../02-specs/01-user-authentication.md)
- [專案管理規格](../02-specs/02-project-management.md)
- [測試案例管理規格](../02-specs/03-test-case-management.md)
- [Issue 與測試資產追溯規格](../02-specs/04-issue-test-traceability.md)

### 03-data-model

用途：

- 定義資料模型與資料表責任
- 整理欄位設計方向
- 與功能規格分開維護

目前包含：

- [資料模型總覽](../03-data-model/01-data-model-overview.md)
- [登入與使用者資料模型](../03-data-model/04-authentication-data-model.md)
- [專案管理資料模型](../03-data-model/02-project-management-data-model.md)
- [Audit Info 結構](../03-data-model/99-audit-metadata-fields.md)
- [後端 Domain 可讀性重構計畫](../04-planning/10-backend-domain-refactoring-plan.md)
- [測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md)
- [Issue 與測試資產追溯資料模型](../03-data-model/05-issue-test-traceability-data-model.md)

### 04-planning

用途：

- 管理目前重點
- 說明不同階段要處理什麼
- 收斂待處理與後續工作

目前包含：

- [階段規劃總覽](../04-planning/01-phase-overview.md)
- [待處理清單](../04-planning/02-backlog.md)
- [任務管理 MVP 實作計畫](../04-planning/03-issue-management-implementation-plan.md)
- [AI Agent Orchestration 設計方向](../04-planning/04-ai-agent-orchestration-direction.md)
- [測試管理 MVP 實作計畫](../04-planning/05-test-management-implementation-plan.md)
- [MVP 收尾與發布準備計畫](../04-planning/06-mvp-release-closure-plan.md)
- [跨模組附件實作計畫](../04-planning/07-cross-module-attachments-implementation-plan.md)
- [發布自動化與交付演練計畫](../04-planning/08-release-automation-plan.md)
- [Issue 與測試資產追溯實作計畫](../04-planning/09-issue-test-traceability-implementation-plan.md)

## 文件使用原則

- Agent 開始工作前應先閱讀 repository 根目錄的 [AGENTS.md](../../../AGENTS.md)
- 公開貢獻流程以 [CONTRIBUTING.md](../../../CONTRIBUTING.md) 為準
- `overview` 文件優先回答「這個專案是什麼」
- `specs` 文件優先回答「功能要怎麼定義」
- `data-model` 文件優先回答「資料如何組織」
- `planning` 文件優先回答「現在做什麼，後面做什麼」

同一份文件應盡量只承擔一種主要目的，避免閱讀者在同一頁面來回切換上下文。

## 英文同步文件

目前 MVP 的英文核心文件由 [English Documentation Map](../../en/documentation-map.md) 統一導覽，包含 Identity、Project、Test 規格、三個資料模型、UI Guideline、MVP 收尾與附件設計。歷史性實作計畫仍可只保留繁中版本，不視為 MVP 雙語缺頁。
