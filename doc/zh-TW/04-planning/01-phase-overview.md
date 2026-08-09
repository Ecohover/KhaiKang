# 階段規劃總覽

## 目的

這份文件用來集中管理目前開發重點、後續階段與文件狀態。

功能規格與資料模型不再承擔階段規劃資訊，相關內容統一放在 planning 區維護。

## 目前重點

目前優先處理的主題是 MVP 發布收尾：核心 smoke test、baseline migration 與雙語文件同步；完整人工驗收、CI、Playwright 與 AI 能力延後。

## 目前可閱讀文件

| 類型 | 主題 | 狀態 | 文件 |
| --- | --- | --- | --- |
| overview | 架構總覽 | Ready | [架構總覽](../01-overview/02-architecture-overview.md) |
| overview | 語詞總表 | Ready | [語詞總表](../01-overview/04-domain-glossary.md) |
| spec | 使用者與登入 | Synchronized | [使用者與登入規格](../02-specs/01-user-authentication.md) |
| spec | 專案管理 | Synchronized | [專案管理規格](../02-specs/02-project-management.md) |
| spec | 測試案例管理 | Synchronized | [測試案例管理規格](../02-specs/03-test-case-management.md) |
| data-model | 專案管理資料模型 | Synchronized | [專案管理資料模型](../03-data-model/02-project-management-data-model.md) |
| data-model | 測試案例管理資料模型 | Synchronized | [測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md) |
| data-model | Audit Metadata | Synchronized | [Audit Info 結構](../03-data-model/99-audit-metadata-fields.md) |
| planning | 任務管理 MVP 實作計畫 | Closure | [任務管理 MVP 實作計畫](./03-issue-management-implementation-plan.md) |
| planning | AI Agent Orchestration 設計方向 | Direction | [AI Agent Orchestration 設計方向](./04-ai-agent-orchestration-direction.md) |
| planning | 測試管理 MVP 實作計畫 | Closure | [測試管理 MVP 實作計畫](./05-test-management-implementation-plan.md) |
| planning | MVP 收尾與發布準備計畫 | Active | [MVP 收尾與發布準備計畫](./06-mvp-release-closure-plan.md) |

## 近期範圍

目前 MVP 收尾順序以[收尾與發布準備計畫](./06-mvp-release-closure-plan.md)為準，近期聚焦：

- 本機使用者登入
- 專案建立與成員管理
- 預設專案角色
- Issue 建立、指派、自由流轉、附件與完成
- Issue 列表／看板與附件核心 smoke test
- Test Workspace、Project 關聯與手動 Run 核心 smoke test
- 三個 PostgreSQL baseline migration review
- 中英文 MVP 文件同步與提交

## 後續階段

後續再處理的主題：

- Jenkins / CI 整合
- AI agents 細部設計與執行能力，依 [AI Agent Orchestration 設計方向](./04-ai-agent-orchestration-direction.md) 逐步收斂
- 更細的權限模型
- 自訂角色與自訂工作流

## 使用原則

- `specs` 只寫功能定義與規則
- `data-model` 只寫資料表責任、欄位與關聯
- `planning` 才放優先順序、階段與待處理事項
