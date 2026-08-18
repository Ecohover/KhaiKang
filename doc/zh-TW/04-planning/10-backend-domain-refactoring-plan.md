# 後端可讀性重構計畫

> 狀態：Active principle-driven refactoring。Action Item、驗證證據與中斷接續位置以[後端可讀性重構執行追蹤](./11-backend-refactoring-execution-tracker.md)為準。

## 完成模型

重構的完成單位是「套用到全 repository 的工程原則」。Feature、module、resource 或資料夾只是同一原則下用來建立 characterization、review、rollback 與 commit 的 checkpoint。

完成一個 Project Management、Identity 或 Test Management 切片，不代表該原則完成。只有全 repository 盤點債務歸零，或所有剩餘例外都經 Human 明確核准並留下理由與不可成長的精確 baseline／ADR，才能把原則標為完成。

這項規則不代表一次修改整個 repository。每個原則仍要透過小型、完整的垂直切片實作，保持 observable behavior，並讓每個 checkpoint 都可獨立 review 與回復。

## 執行模型

每個 active principle 都依下列流程處理：

1. 定義全 repository invariant 與例外邊界。
2. 修改 production code 前，完成現況 debt inventory。
3. 依風險補 observable behavior、wire contract、persistence、audit 與 concurrency characterization。
4. 每批只處理一個 semantic family 或垂直切片，不混入其他原則。
5. 精確債務 baseline 必須下降；不得用另一個寬泛 abstraction 包裝原有債務。
6. 執行 targeted 與完整驗證，再進行獨立 review。
7. Tracker 記錄證據、剩餘債務、核准例外與下一個安全 checkpoint。

## 原則型工作流

| ID | 原則 | 全 repository 完成條件 |
| --- | --- | --- |
| P01 | 重構安全網 | 所有被調整的行為都在正確 boundary 有保護；外部系統、資料庫、檔案儲存與時間來源在適合層級隔離或 mock；測試保護 observable behavior，不鎖 private implementation。 |
| P02 | Domain／Application API 可讀性 | Public business API 不再依賴長參數或易混淆 primitive 位置；creation、change、context model 代表真實概念；例外有紀錄且 baseline 不增加。 |
| P03 | 公開邊界契約 | HTTP 與跨模組 public contract 使用明確 body-form、一個 public type 一檔；canonical required、nullable、JSON、OpenAPI 與 TypeScript 行為保持一致。 |
| P04 | Result／Outcome 語意 | 不存在 caller 不可能遇到的 outcome 或 payload 組合；只有真正 semantic family 共用型別；無 payload 操作不用假 `object`；endpoint mapping 完整；任意業務錯誤字串歸零或經明確核准。 |
| P05 | 封閉狀態與 stable code | 封閉集合使用 enum／Value Object 語意，資料庫保存 stable English code；資料表管理的分類提供穩定 client code 與 display name；mapping 使用具名常數並有 round-trip test。 |
| P06 | Audit lifecycle 與 mutation context | 只有 lifecycle 與 nullability 真正一致的 created／updated actor、time、version 行為才共用；mutation context 完整傳遞；feature-specific lifecycle 留在 owner。 |
| P07 | Query／paging 可讀性 | EF query 依 normalize、filter、order、count、page、project、execute 順序閱讀；優先使用原生 EF；generic query framework 不隱藏業務條件。 |
| P08 | 抽象節制 | Helper、factory、interface、repository、base type、parameter object 只在增加 domain language、invariant、boundary isolation 或已證明 reuse 時成立。 |
| P09 | 實體組織 | Public type 依 resource／use case 分類，不以技術型別分桶；一個 top-level public type 一個同名檔案；跨資源 coordinator 在有真實 owner 前保留 layer root。 |
| P10 | Enforcement 與交付 | Compiler、analyzer、architecture test、format、contract check、CI 漸進落實客觀規則；合併前檢查 branch migration 原則；所有 baseline 只能縮小。 |

## Human 與 AI 責任

- Human owner：決定業務語意、相容性、資料意義、公開契約變更、例外、風險接受、merge 與 release。
- AI Builder：完成 inventory、characterization、受控實作、驗證、文件與 Tracker 同步。
- 獨立 AI Reviewer：檢查 outcome reachability、抽象價值、contract drift、測試品質，以及全域債務是否真的下降。
- CI：只 enforcement 客觀規則；不能替代 Human 判斷兩個操作是否真的屬於同一 semantic family。

## 每批邊界

- 一批只處理一個全域原則；同一垂直切片需要時可以同時涵蓋 service、endpoint、domain 與 tests。
- 不得把結構重構與 wire contract、schema、業務規則或 error identity 變更靜默混在一起。
- 行為或相容性有風險時，production 變更前先建立 characterization。
- Checkpoint 要小到能獨立 review／回復，但 checkpoint 完成不代表全域原則完成。
- 不建立新的 generic `OperationResult<T>`、query framework、多層 base hierarchy 或 property bag，只為讓局部程式碼外觀看起來一致。

## 全 repository 驗收條件

只有符合以下條件，才能把一項原則標為完成：

- inventory 歸零，或所有例外都有 Human 明確核准與可長期追蹤的理由；
- architecture／debt baseline 不可增加；
- observable behavior 與適用的 HTTP、OpenAPI、TypeScript、persistence、audit、concurrency 語意都有保護；
- 受影響的 Release build、unit、integration、format，以及 contract／EF check 通過；
- 獨立 review 無未處理 blocker、high、medium finding；
- Tracker 記錄驗證證據、核准例外與下一個安全動作。
