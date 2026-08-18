# Issue 與測試資產追溯實作計畫

狀態：已完成並通過整合、前端與全新 PostgreSQL／Docker smoke 驗證（2026-08-11）。

## 目的

依 [追溯規格](../02-specs/04-issue-test-traceability.md) 與 [資料模型](../03-data-model/05-issue-test-traceability-data-model.md)，先建立有方向與從屬規則的 Issue 關聯，再完成 Case、Plan、Run 與 Bug 的測試追溯。

## 實作階段

| 階段 | 狀態 | 內容 | 完成條件 |
| --- | --- | --- | --- |
| 1. Spec／資料模型 | 已完成 | 定義 Issue 關聯、Case 需求、Plan 測試 Issue、Run 快照與 Run–Bug 來源 | 中英文規格、資料模型與 backlog 一致 |
| 2. Issue 關聯 OpenAPI | 已完成 | relation types、list、create、delete 合約與 Problem Details | OpenAPI 已同步 C#／TypeScript contracts |
| 3. Issue 關聯 Backend | 已完成 | Entity、DbContext、migration、方向標準化、父層與循環規則 | PostgreSQL 與整合測試通過 |
| 4. 測試追溯 OpenAPI／Backend | 已完成 | Case 需求、Plan 測試 Issue、Run 快照及 Run–Bug 來源 | 跨模組授權、快照與 unlink 規則通過 |
| 5. Frontend | 已完成 | Issue 關聯編輯、Case 需求選擇、Plan 測試 Issue、Run 快照與 Bug 區塊 | 共用關聯 UI、雙向導覽及 i18n 完成 |
| 6. 回歸與安全驗證 | 已完成 | 權限矩陣、跨 Workspace／Project、循環、重複與併發 | 前後端測試、全新 PostgreSQL migration 與 MVP smoke 通過 |

## API 切片

### Project Management

- 列出固定 Issue 關聯類型。
- 依 Issue 列出正向與反向關聯。
- 建立一筆有方向的 Issue 關聯。
- 以版本條件軟刪除關聯。

### Test Management

- 列出、建立及移除 Case 的需求 Issue 關聯。
- 在 Plan create/update/read 合約加入一張測試 Issue。
- 在 Run read 合約回傳建立時的測試 Issue 快照。
- 從 Run 建立 Bug 並回傳來源關聯；Run 可列出已開立 Bug。

## 驗證重點

- `related` A–B 與 B–A 不得重複。
- 有向關聯必須正確顯示反向語意。
- `parent_of` 禁止第二父層、自我關聯與間接循環。
- 只有 Project 權限或只有 Workspace 成員資格都不得取得測試追溯另一側資訊。
- Plan 變更測試 Issue 不影響既有 Run 快照。
- 有效測試追溯應阻止 Workspace–Project unlink。
- 重複、錯誤類型、錯誤範圍與版本衝突均有明確 Problem Details。

## 完成證據

- Backend：23 tests passed（13 unit、10 integration）。
- Frontend：29 tests passed，type-check 與 production build 通過。
- OpenAPI：YAML 可解析，包含 58 paths 與 73 schemas。
- 全新 PostgreSQL 已依序套用 baseline、`AddIssueRelations` 與 `AddIssueTestTraceability`。
- 隔離 Docker Compose 完整 MVP smoke 通過，包含附件、Run snapshot 與 PostgreSQL／API／Web 重啟持久化。
