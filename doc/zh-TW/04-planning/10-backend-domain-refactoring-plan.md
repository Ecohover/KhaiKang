# 後端 Domain 可讀性重構計畫

> 狀態：Active incremental refactoring。Project Management 的 public 長參數債務已清除，Identity 正在重構。Action Item、驗證紀錄與中斷接續位置以[後端可讀性重構執行追蹤](./11-backend-refactoring-execution-tracker.md)為準。

## 目的

讓 Domain API 以領域語言表達意圖，降低長參數、同型別參數放錯位置、primitive obsession 與重複 audit 實作造成的維護風險。重構必須保持 HTTP、資料庫欄位與既有業務行為相容。

## 目前債務基線

截至 2026-08-12，architecture fitness baseline 記錄：

- Project Management：public Domain 長參數 0，重複 audit owner 1（`Project`）。
- Identity：public Domain 長參數 4，重複 audit owner 3。
- Test Management：public Domain 長參數 25，重複 audit owner 17。

`Issue` 與 `Project` 的建立／修改 API 已改為具意圖的 creation／change model；後續不得只為減少參數或程式行數繼續拆出沒有獨立語意的小型別。

## 已確認問題與原始目標

### Issue 建立

重構前的 `Issue` 建構子曾接受 13 個參數，其中多個為 `Guid` 與 nullable `string`。呼叫端難以單靠位置確認意義，新增或調換欄位時也容易產生靜默錯誤；目前已改為具名 creation model。

目標方向：

- 以 `Issue.Create(IssueCreation creation, ChangeContext context)` 或同等的具名 factory 表達建立行為。
- `IssueCreation` 只承載建立 Issue 所需且彼此內聚的資料，可以再組合 `IssueContent`、`IssueClassification` 或 Strongly Typed ID；不建立全專案共用的萬用參數袋。
- EF Core 所需的無參數建構子保持 private，不視為 Domain 建立 API。

### Issue 修改

重構前的 `UpdateDetails` 曾接受 9 個參數，混合內容、分類與 audit context；目前已改為 `IssueDetailsChange` 與 `ChangeContext`。

目標方向：

- 改為 `UpdateDetails(IssueDetailsChange change, ChangeContext context)` 或等價名稱。
- `IssueDetailsChange` 僅包含可由此 use case 一次更新的欄位。
- `ChangeContext` 集中 actor 與發生時間，避免每個 mutation 重複排列 `Guid actorAccountId` 與 `DateTimeOffset occurredAt`。
- `ChangeStatus` 後續不再依賴裸 `statusCode` 字串判斷完成狀態，應改用經驗證的狀態參照或明確的 domain semantics。

### Audit metadata

目前盤點有 33 個 Domain type 重複宣告完整或近似的 audit 欄位與版本更新行為。

目標方向：

- 建立一層淺層 `AuditableEntity`，集中 `CreatedAt`、`CreatedByAccountId`、`UpdatedAt`、`UpdatedByAccountId` 與 `Version`。
- 基底只提供一致的初始化與異動紀錄行為；不放 HTTP、EF Core 或 feature-specific 規則。
- `CompletedAt`、`DeletedAt`、`ExecutedByAccountId` 等生命週期或業務欄位留在各自 entity。
- EF Core 仍映射到現有欄位名稱，`Version` 維持 concurrency token；正常情況不產生 schema migration。
- 不建立多層 `BaseEntity -> BaseAuditableEntity -> BaseAggregateRoot` 階層。若 composition 能更清楚表達不同 audit lifecycle，可以在實作前以小型 spike 比較 EF Core mapping 複雜度。

## 已採用的 API 規則

- 新增的 public Domain 與 Application API 原則上最多三個參數。
- 同型別 primitive 參數若容易誤置，必須先考慮 Parameter Object、Value Object 或 Strongly Typed ID。
- Parameter Object 必須有單一 use case 或 domain concept，不得只是為壓低參數數量的資料袋。
- 既有違規項目以 architecture fitness test allowlist 管理；新程式碼不得擴大 allowlist。
- 重構優先保持 observable behavior，不以追求抽象一致性改變 contract 或資料模型。

## 建議順序

1. 補齊 Issue characterization tests，鎖定建立、修改、狀態、指派、audit 與 version 行為。
2. 建立 architecture fitness baseline，列出既有長參數與重複 audit 宣告；禁止新增債務。
3. 引入 `ChangeContext` 與一層 `AuditableEntity`，先在 Project Management 小範圍驗證 EF Core mapping。
4. 重構 `Issue` 建立與 `UpdateDetails`，確認 Application 與 HTTP contract 無變化。
5. 依序處理 Project Management、Identity、Test Management 的既有 allowlist。
6. 每一批執行 unit、API integration、EF pending-model check；刪除已完成的 allowlist 項目。

## 驗收條件

- 重構後的 public Domain/Application API 不超過三個參數，或有 ADR 記錄例外。
- 呼叫端不再依賴多個同型別 primitive 的位置辨識語意。
- audit 欄位與 `Version` 行為和重構前一致。
- HTTP/OpenAPI、資料庫欄位、stable code 與既有 migration 保持相容。
- Domain unit tests、API integration tests 與兩個受影響 DbContext 的 pending-model check 通過。
