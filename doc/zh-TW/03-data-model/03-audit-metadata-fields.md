# Audit Metadata 欄位表

## 目的

這份文件整理 KhaiKang 第一階段建議採用的標準 audit metadata 欄位語意。

這裡先只定義欄位概念與用途，不在此文件中展開完整 audit log 實作策略。

目前設計方向先採用一致的 audit fields 語意：

- `CreatedAt`
- `CreatedByUserId`
- `CreatedByUserName`
- `LastUpdatedAt`
- `LastUpdatedByUserId`
- `LastUpdatedByUserName`
- `Version`

## 使用原則

- 需要保存主檔建立與更新資訊的 entity，應採用一致的 audit metadata
- 建立時應寫入建立者與最後更新者資訊
- 更新時應刷新最後更新者資訊與版本號
- 時間欄位應統一使用 UTC
- 若執行當下缺少操作者資訊，系統仍應可正常寫入時間欄位

## 欄位定義

| 標準名稱 | 中文名稱 | 說明 | 備註 |
| --- | --- | --- | --- |
| `CreatedAt` | 建立時間 | 記錄資料第一次建立的時間。 | 應使用 UTC。 |
| `CreatedByUserId` | 建立者帳號識別 | 記錄建立該資料的帳號識別。 | 對應系統中的 account identity。 |
| `CreatedByUserName` | 建立者名稱 | 記錄建立該資料時的操作者名稱。 | 作為顯示用途。 |
| `LastUpdatedAt` | 最後更新時間 | 記錄資料最近一次更新的時間。 | 應使用 UTC。 |
| `LastUpdatedByUserId` | 最後更新者帳號識別 | 記錄最近一次更新該資料的帳號識別。 | 對應系統中的 account identity。 |
| `LastUpdatedByUserName` | 最後更新者名稱 | 記錄最近一次更新該資料時的操作者名稱。 | 作為顯示用途。 |
| `Version` | 版本號 | 記錄主檔版本遞增值。 | 可用於 optimistic concurrency 或變更追蹤。 |

## 第一階段套用建議

第一階段建議以下主資料優先採用一致的 audit metadata：

- `accounts`
- `projects`
- `project_members`
- `issues`
- `issue_types`
- `issue_statuses`
- `issue_priorities`

是否對所有主資料表全面套用，可在資料表細化時再逐一確認。

## 非目標

這份文件目前不處理：

- 獨立 audit log table / collection 設計
- detail patch / diff 記錄格式
- retention policy
- audit 查詢 API
- audit UI 顯示設計
