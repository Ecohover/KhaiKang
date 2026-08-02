# Audit Info 結構

## 目的

本文件定義 KhaiKang 各資料表共用的 `audit_info` 結構。

資料模型文件中的 `audit_info` 是統一的操作紀錄欄位，資料表規格以單一欄位表示；其內部欄位名稱仍遵循 PostgreSQL 的 `snake_case` 命名規則。

本文件只定義主資料的建立、更新與版本資訊。不在此處展開完整的獨立 Audit Log、欄位差異或事件歷程設計。

## 結構規格

| 項目 | 內容 |
| --- | --- |
| 結構名稱 | `audit_info` |
| 說明 | 保存資料建立、最後更新與樂觀鎖定所需的共用資訊。 |
| 型別 | `-` |
| 備註 | 在資料表規格中作為統一欄位引用；實際資料庫映射方式於實作階段決定。 |

## 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `created_at` | 建立時間 | `timestamp with time zone` | Y | N | 使用 UTC，資料第一次建立時寫入。 |
| `created_by_account_id` | 建立者帳號識別 | `uuid` | N | N | 對應 `accounts.id`；系統初始化或背景處理可為空。 |
| `updated_at` | 最後更新時間 | `timestamp with time zone` | Y | N | 使用 UTC，每次更新時刷新。 |
| `updated_by_account_id` | 最後更新者帳號識別 | `uuid` | N | N | 對應 `accounts.id`；系統初始化或背景處理可為空。 |
| `version` | 版本號 | `integer` | Y | N | 用於 optimistic concurrency；建立時由 `1` 開始，每次更新遞增。 |

## 使用原則

- 資料表與欄位名稱一律使用 `snake_case`；帳號外鍵統一使用 `*_account_id`，不使用 `*_user_id`。
- `created_at` 與 `updated_at` 必須使用 UTC 的 `timestamp with time zone`。
- `created_by_account_id` 與 `updated_by_account_id` 為可空欄位，避免系統初始化、資料匯入或背景工作缺少明確操作者時無法寫入資料。
- 不在 `audit_info` 重複保存帳號顯示名稱；需要顯示時以帳號資料查詢或由未來獨立 Audit Log 保存當下快照。
- `version` 用於避免兩個使用者同時覆寫資料。更新時應帶入讀取時的版本值，成功更新後遞增。
- `audit_info` 不取代未來的 Audit Log。前者只記錄目前資料的建立與最後更新資訊，後者才保存完整歷程與欄位差異。

## 資料表引用方式

主資料表欄位規格統一以以下一列表示：

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考本文件。 |

目前資料模型中的 Entity 與 Mapping 預設採用本結構。若特定資料表不需要版本控制，必須在該資料表的備註中明確說明例外原因。

## 非目標

本文件目前不處理：

- 獨立 `audit_logs` 資料表設計
- 欄位變更的 patch、diff 或 JSON 結構
- 自訂欄位異動紀錄
- retention policy
- Audit 查詢 API 與 UI 顯示設計
