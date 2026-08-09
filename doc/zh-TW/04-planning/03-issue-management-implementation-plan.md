# 任務管理 MVP 實作計畫

## 目的

這份文件安排 KhaiKang 任務管理 MVP 的實作順序、階段邊界與驗收方式。

功能規則仍以[專案管理規格](../02-specs/02-project-management.md)為準，資料責任仍以[專案管理資料模型](../03-data-model/02-project-management-data-model.md)為準。本文件只管理交付順序與完成狀態。

> 2026-08-05 起，跨模組的 MVP 收尾順序改以[收尾與發布準備計畫](./06-mvp-release-closure-plan.md)為準。本文件保留任務管理功能明細；附件仍是 MVP，Tag、子任務、關聯、留言與通知則不納入 MVP。

UI 統一使用「任務」；既有程式碼、資料表與 HTTP contract 保留 `Issue` 技術命名。

## MVP 完成範圍

任務管理 MVP 完成時應包含：

- 建立、讀取與編輯任務
- 獨立的處理人指派與取消指派
- Kanban 自由狀態流轉
- 任務列表、看板與基本查詢
- 完成說明與完成時間
- 專案角色權限與稽核
- 本機附件上傳、列表、下載與移除
- OpenAPI、C# DTO、TypeScript DTO 與 HTTP client 同步
- 後端整合測試及前端行為驗證

以下項目不納入本次 MVP：

- Tag
- Parent / Sub-task
- Issue relation
- Comment
- Watcher / notification
- 自訂欄位
- 自訂工作流
- Sprint
- AI 自動執行

## 已確認決策

### 附件範圍

功能規格已將附件列為第一版能力，因此 `issue_attachments` 納入任務管理 MVP，但排在核心任務流程穩定之後實作。

Comment、Tag、Relation、Watcher 與 Transition 仍維持延伸能力，不納入本次 MVP。

### 建立任務的最小輸入

- `title` 必填
- `typeCode` 必填
- `priorityCode` 可省略，由後端預設為 `medium`
- `assigneeAccountId` 可省略，預設為未指派
- 狀態由後端預設為 `created`
- 建立人與時間由後端依登入身分及系統時間產生

### 權限邊界

- 一般內容編輯使用 `issue.update`
- 處理人變更使用 `issue.assignee.change`
- 狀態變更使用 `issue.status.change`
- 附件上傳使用 `issue.attachment.upload`
- 附件移除使用 `issue.attachment.delete`
- 讀取任務使用 `issue.read`

不得因預設角色目前同時擁有多個權限，就在單一更新端點中混用權限語意。

### 停用專案

停用專案保留讀取能力，但禁止建立、編輯、指派、流轉與附件異動。異動請求回傳穩定的 `409 Conflict` Problem Details。

## 實作階段

### 第一階段：核心合約與授權

狀態：已完成（2026-07-24）

範圍：

- 建立任務只要求標題與類型
- 優先順序未提供時由後端使用 `medium`
- 拆出獨立處理人指派 API
- 一般編輯 DTO 不再同時變更處理人
- 停用專案改為唯讀
- 補上 `issue_assignee_changed` 稽核事件
- 同步 OpenAPI、C#、TypeScript、前端與整合測試

驗收：

- 只傳 `title` 與 `typeCode` 可成功建立任務
- 建立結果為 `created`、`medium`、未指派
- 有 `issue.assignee.change` 的成員可指派或取消指派
- 僅有 `issue.assignee.change` 而沒有 `issue.update` 的成員不能修改任務內容
- 非專案成員無法存取任務
- 停用專案的任務可讀但不可異動

驗證結果：

- 後端整合測試：5 項通過
- 前端型別檢查：通過
- 前端單元測試：5 項通過
- 前端正式建置：通過

### 第二階段：新增、編輯與完成體驗

狀態：已完成，待人工驗收（2026-07-24）

範圍：

- 顯示建立人、建立時間、更新時間與完成時間
- 未儲存離開提醒
- 儲存成功提示
- `409` 版本衝突的重新載入操作
- 切換 `completed` 且處理結果為空時提供非阻擋提醒
- 狀態及處理人提供鍵盤與觸控可用的操作方式

驗證結果：

- 前端單元測試：16 項通過
- 共用 UI 與 Web 型別檢查：通過
- 前端正式建置：通過
- 後端單元測試回歸：13 項通過
- 後端整合測試回歸：6 項通過
- 桌面與手機操作保留人工驗收

### 第三階段：列表、看板與查詢

狀態：功能與自動驗證已完成；完整人工驗收延後（2026-08-09）

已完成：

- 建立共用 one-based 分頁合約與 `PagedResult<T>`
- 任務列表改為資料庫端 `Count`、`Skip`、`Take`
- OpenAPI、C#、TypeScript 與前端分頁控制同步
- Issue HTTP endpoints 整理為標準 Controller 與英文 region

範圍：

- 編號與標題關鍵字搜尋
- 類型、狀態、優先順序及處理人篩選
- 未指派篩選
- 更新時間排序
- 分頁與每頁筆數
- 看板卡片可進入任務頁
- 拖拉失敗回復與錯誤提示
- 手機版不依賴拖拉也能變更狀態

### 第四階段：稽核與併發安全

狀態：核心實作與自動驗證已完成；完整角色矩陣人工驗收延後

範圍：

- `actor_type` 取自實際帳號類型，不固定為 `human`
- 完整記錄任務建立、更新、指派與狀態事件
- `completed_at` 進入完成狀態時設定，離開完成狀態時清除
- 專案內任務流水號並行建立時由後端安全處理
- 補齊四種預設角色的權限矩陣測試

### 第五階段：附件管理

狀態：已完成（2026-08-07），本機儲存基本重啟驗證已通過

範圍：

- `issue_attachments` migration
- 本機檔案儲存與 Docker volume
- 上傳、列表、下載與軟刪除 API
- 檔案大小、檔名與路徑安全限制
- 上傳者、時間與附件稽核
- 任務編輯頁附件區塊

### 第六階段：MVP 完整驗收

狀態：MVP 基本自動驗證已完成；完整桌面與手機人工驗收延後

範圍：

- OpenAPI operation 與 C#、TypeScript 實作逐項對照
- migration review
- 後端 restore、build、format、test
- 前端 type-check、test、build
- 桌面與手機版主要流程驗收
- 文件完成狀態與 backlog 同步

## 第一階段預計異動

HTTP contract：

- 調整 `CreateIssueRequest.priorityCode` 為 nullable / optional
- 調整 `UpdateIssueRequest`，移除 `assigneeAccountId`
- 新增 `UpdateIssueAssigneeRequest`
- 新增 `PUT /api/v1/projects/{projectId}/issues/{issueId}/assignee`
- 新增停用專案的穩定 Problem Details

後端：

- `IssueService.CreateAsync`
- `IssueService.UpdateAsync`
- 新增 `IssueService.ChangeAssigneeAsync`
- `Issue` domain 的內容更新與處理人變更分離
- endpoint validation 與 mutation result mapping

前端：

- 建立 request 的預設優先順序仍可顯示 `medium`
- 編輯 request 不再夾帶處理人
- 處理人使用獨立 client method 儲存
- 權限不足時不顯示可變更處理人的控制

測試：

- 最小輸入建立
- 預設值
- 指派與取消指派
- 無效專案成員
- Reviewer 權限邊界
- 停用專案唯讀
- stale version conflict

## 風險與注意事項

- OpenAPI、C# 與 TypeScript 都是維護中的正式來源檔，必須同一階段同步。
- 目前工作區包含尚未提交的專案管理與權限功能，實作時不得覆蓋或拆散既有變更。
- 第一階段不新增資料表，原則上不需要 migration；若 EF model snapshot 發生變化，必須先確認是否為非預期變更。
- SQLite 整合測試可驗證 HTTP 與授權流程，但 PostgreSQL-specific 的並行流水號行為留到第四階段使用 PostgreSQL 驗證。
