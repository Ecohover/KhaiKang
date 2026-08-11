# 階段規劃總覽

## 目的

這份文件用來集中管理目前開發重點、後續階段與文件狀態。

功能規格與資料模型不再承擔階段規劃資訊，相關內容統一放在 planning 區維護。

## 目前重點

目前優先處理的主題是穩定版 `v0.1.0` 晉級：完成 Release Note、版號一致性與經審閱的 `rc` 到 `main` 流程。不可變 `sha-0fd82c0` API／Web images 已通過全新 volumes、完整 MVP smoke、附件雜湊、Test Run snapshot 與重啟持久化驗證。備份／還原自動化在真正保存使用者資料前仍須完成，但明確延後於這次 MVP 晉級；完整桌面／手機驗收、Playwright 與 AI 能力亦延後。

## 目前可閱讀文件

| 類型 | 主題 | 狀態 | 文件 |
| --- | --- | --- | --- |
| overview | 架構總覽 | Ready | [架構總覽](../01-overview/02-architecture-overview.md) |
| overview | 語詞總表 | Ready | [語詞總表](../01-overview/04-domain-glossary.md) |
| spec | 使用者與登入 | Synchronized | [使用者與登入規格](../02-specs/01-user-authentication.md) |
| spec | 專案管理 | Synchronized | [專案管理規格](../02-specs/02-project-management.md) |
| spec | 測試案例管理 | Synchronized | [測試案例管理規格](../02-specs/03-test-case-management.md) |
| spec | Issue 與測試資產追溯 | Implemented / verified | [追溯規格](../02-specs/04-issue-test-traceability.md) |
| data-model | 專案管理資料模型 | Synchronized | [專案管理資料模型](../03-data-model/02-project-management-data-model.md) |
| data-model | 測試案例管理資料模型 | Synchronized | [測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md) |
| data-model | Issue 與測試資產追溯 | Implemented / verified | [追溯資料模型](../03-data-model/05-issue-test-traceability-data-model.md) |
| data-model | Audit Metadata | Synchronized | [Audit Info 結構](../03-data-model/99-audit-metadata-fields.md) |
| planning | 任務管理 MVP 實作計畫 | Closure | [任務管理 MVP 實作計畫](./03-issue-management-implementation-plan.md) |
| planning | AI Agent Orchestration 設計方向 | Direction | [AI Agent Orchestration 設計方向](./04-ai-agent-orchestration-direction.md) |
| planning | 測試管理 MVP 實作計畫 | Closure | [測試管理 MVP 實作計畫](./05-test-management-implementation-plan.md) |
| planning | MVP 收尾與發布準備計畫 | Accepted boundary | [MVP 收尾與發布準備計畫](./06-mvp-release-closure-plan.md) |
| planning | 發布自動化與交付演練計畫 | Active | [發布自動化與交付演練計畫](./08-release-automation-plan.md) |
| planning | Issue 與測試資產追溯實作計畫 | Completed | [追溯實作計畫](./09-issue-test-traceability-implementation-plan.md) |

## 近期範圍

目前工作順序以[發布自動化與交付演練計畫](./08-release-automation-plan.md)為準；[MVP 收尾與發布準備計畫](./06-mvp-release-closure-plan.md)保留已確認的 MVP 邊界。近期聚焦：

- 完成 `v0.1.0` Release Note 與版號一致性
- 以 PR 將已驗收 `rc` 晉級到 `main`
- 保留不可變 SHA image、workflow run 與 smoke 證據
- PostgreSQL 與附件 volume 備份／隔離還原演練延後至保存真實使用者資料前
- 中英文發布文件與實測證據同步
- Issue 與測試資產追溯已完成 OpenAPI、migration、前後端與隔離 Docker smoke 驗證；後續細部能力依 backlog 排程

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
