# 開發規範

## 目的

這份文件定義 KhaiKang 的工程開發規則。`AGENTS.md` 是精簡的 Agent 操作入口；本文件提供人類與自動化開發工具共同遵守的設計原則。

## 工程優先順序

原則衝突時，依下列順序判斷：

1. 正確性與安全性
2. 可讀性與可維護性
3. 與既定模組邊界的一致性
4. 簡單性
5. 重用與抽象化

不得為了減少檔案、縮短程式碼或追求流行架構而犧牲以上原則。

## 開發流程

- 實作前先閱讀相關功能規格與資料模型。
- 會影響行為的未決問題，應先收斂再寫入程式。
- 每次完成一個可以驗證的最小垂直切片。
- 規格、程式碼、測試、資料庫 migration 與 API contract 必須保持一致。
- 若決策會改變模組邊界、資料策略、公開合約、安全或部署拓撲，應留下簡短設計紀錄或 ADR。
- 跨領域工作可以先做計畫，但本專案不強制 Planner、Builder、Reviewer 或 handoff 流程。

## 模組化單體

KhaiKang 是 modular monolith，由少量 process host 部署。Solution 中有多個 project 是為了建立編譯期邊界，不代表各自都是微服務。

### Host

- `KhaiKang.Api` 是 HTTP host 與 composition root。
- `KhaiKang.Worker` 是背景工作 host。
- Host 負責註冊模組與基礎設施，不承載商業規則。

### 功能模組

後端商業能力依功能放在 `backend/src/modules/`，project 命名為 `KhaiKang.Modules.<Feature>`。

初期每個功能模組只建立一個 class library project，並在內部按需要使用以下資料夾：

```text
KhaiKang.Modules.ProjectManagement/
  Domain/
  Application/
  Infrastructure/
  Endpoints/
```

- `Domain`：entity、value object 與 invariant。
- `Application`：use case 與模組對外 contract。
- `Infrastructure`：EF Core 設定與外部系統 adapter。
- `Endpoints`：將 HTTP request 轉交給 application use case。

除非獨立編譯邊界已帶來明確價值，否則不要預設把 Domain、Application、Infrastructure 各拆成一個 project。
不得為了架構外觀先建立空的分層資料夾；只有目前垂直切片確實有對應程式碼時才新增資料夾。

### 模組互動

- 模組擁有自己的資料與 persistence configuration。
- 其他模組不得直接讀寫該模組的資料表。
- 跨模組行為使用明確的 application contract 或 domain event。
- 不得引用其他模組的 endpoint type、EF entity 或 infrastructure class。
- 預設使用 process 內呼叫；只有確實需要非同步傳遞或重試語意時才加入 message queue。

## 共用元件

`KhaiKang.CommonUtils` 與 `KhaiKang.CommonUtils.Web` 只放後端共用元件。程式碼必須同時符合下列條件才可搬入：

- 不包含領域語意；
- 至少兩個模組需要相同行為；
- API 已穩定到足以支援多個呼叫端；
- 搬入後不會讓不同功能模組透過 common 層互相耦合。

適合的內容包含時間抽象、middleware 與 observability primitive。Domain entity、feature DTO、專案權限與 workflow rule 必須留在所屬模組。設定應使用標準 Configuration 與 Options Pattern，不建立自訂環境變數 helper。

從其他 repository 參考的程式碼，必須視為新的 KhaiKang 程式碼重新審查。不得保留公司專屬名稱、header、secret、基礎設施假設或業務行為，並且必須確認授權允許重用。

## API 設計

### Route 與 HTTP method

- 公開 API 使用 `/api/v1` 版本前綴。
- Route 使用小寫複數資源名稱，例如 `/api/v1/projects`。
- 查詢使用 `GET`、建立使用 `POST`、完整替換使用 `PUT`、部分更新使用 `PATCH`、刪除使用 `DELETE`。
- 一般篩選、排序與分頁使用 query parameter；只有無法安全且清楚放入 URL 的複雜查詢才建立 search endpoint。
- Route 不使用 `create`、`update`、`query` 等實作動作名稱。

### Response 與錯誤

- 成功時直接回傳 resource 或明確的 operation DTO。
- Collection 查無資料時回傳 `200` 與空 collection 或空 page。
- 建立資源時，在合理情況下使用 `201 Created` 並提供 location。
- 錯誤使用 ASP.NET Core `ProblemDetails` 或 `ValidationProblemDetails`。
- HTTP status code 是主要判斷依據，不在 envelope 內重複 status。
- Problem Details 的 `type` 應是穩定的錯誤種類識別；client 需要依業務錯誤分支時，可加入穩定的 `code` extension，但不得依賴人類訊息。
- Trace 與 correlation identifier 屬於 observability data，不納入公開錯誤合約；後端 log 仍應保留可供關聯請求的 trace context。
- 不得把 exception message、stack trace、connection detail 或其他敏感診斷資訊回傳給 client。

Application 與 Domain 不得引用 ASP.NET Core、HTTP status 或 `ProblemDetails`。模組定義領域與 application error semantics，Web 層集中將它們映射為 HTTP 回應。預設映射如下：

| 錯誤語意 | HTTP status |
| --- | --- |
| Request shape、格式或 boundary validation 失敗 | `400 Bad Request` |
| 未驗證身分 | `401 Unauthorized` |
| 身分已確認但無操作權限 | `403 Forbidden` |
| Resource 不存在 | `404 Not Found` |
| 唯一性、版本或狀態轉移衝突 | `409 Conflict` |
| 未預期錯誤 | `500 Internal Server Error` |

### Contract

- `contract/openapi/khaikang.v1.yaml` 是 HTTP wire contract 的唯一來源。
- C# DTO／endpoint 與 TypeScript DTO／HTTP client 是依 OpenAPI 維護的正式原始碼，不視為可任意覆蓋的 generated code。
- 公開合約先修改 OpenAPI，再由 AI 讀取現有程式並同步兩端；必須保留仍符合新合約的客製行為。
- 功能 C# DTO 留在所屬 backend module，TypeScript wire type 留在 frontend；`contract/` 不放特定語言的 implementation project。
- Route、status、request、response、nullability 與 enum 的變更都視為 API contract 變更。
- 外部系統透過 adapter 隔離，將外部 wire contract 轉為 KhaiKang 內部語意。
- 完整流程與檢查項目以 [AI 與 OpenAPI 協作開發流程](./06-ai-openapi-development-workflow.md) 為準。

## .NET 規則

後端詳細規則以 [.NET 開發準則](./05-dotnet-development-guidelines.md) 為準；本節只保留跨專案的摘要。

- 使用 .NET 10、啟用 nullable reference type，並使用 file-scoped namespace。
- Repository 根目錄的 `.editorconfig` 是可執行的格式規則來源。C# 使用四個空格、控制流程加大括號，每個檔案只有一個主要 public type。
- Public member 與 type 使用 PascalCase，local 與 parameter 使用 camelCase，可等待方法使用 `Async` 後綴。
- Identifier、comment、log template 與 XML documentation 使用英文。
- Request 與 response 優先使用 immutable record 或 init-only property。
- Public collection 不允許外部替換或修改時，應暴露唯讀 abstraction。
- 使用 constructor injection，不使用 service locator 或 property injection。
- I/O 使用 async API，並傳遞 `CancellationToken`。
- 預設不加入自動 mapping、mediator、generic repository 或 result-wrapper library；只有在問題與效益被記錄後才引入。
- 使用穩定 template 的 structured logging；不得只為 log 而 catch 後原樣 rethrow。

## 設定與 Secret

- 使用 ASP.NET Core 標準 configuration pipeline。
- 設定由實際使用它的 host 或功能模組擁有，不集中成全專案環境變數 enum 或 helper。
- 相關設定使用 `BindConfiguration` 綁定為 options class，並透過 `ValidateDataAnnotations`、自訂 validation 或等價機制驗證。
- 必要設定使用 `ValidateOnStart`，讓錯誤在啟動階段發生。
- Feature code 透過 `IOptions<TOptions>`、`IOptionsSnapshot<TOptions>` 或 `IOptionsMonitor<TOptions>` 取得設定，不直接呼叫 `Environment.GetEnvironmentVariable`。
- `appsettings.json` 只放非敏感預設值。
- 部署覆寫使用標準階層式環境變數名稱，例如 `Postgres__ConnectionString`；本機 secret 使用 .NET user secrets。
- 只有 placeholder 的 `.env.example` 可以提交，真實值不得進版控。
- 必要設定缺失時，啟動應以清楚錯誤直接失敗。

## 資料存取

- PostgreSQL 是 system of record，EF Core 是預設 persistence mechanism。
- Table、column、index 與 constraint 使用 `snake_case`。
- 主鍵預設使用 UUID，其他策略必須有明確模型理由。
- 時間以 UTC `timestamp with time zone` 儲存。
- Modular monolith 使用一個 database，各模組擁有自己的 entity 與 EF Core configuration。
- 初期使用一個 application `DbContext`；只有模組隔離或營運需求足以抵銷 migration 複雜度時才拆分。
- 每次 schema 變更都必須有 migration，並考慮 rollback 或資料復原方式。
- 功能分支準備合併主線前，尚未發布的 schema 異動必須依每個受影響的 `DbContext` 收斂為一份最終 migration；開發途中產生的同一 context 增量 migration 不保留。
- 同一分支若修改多個模組各自擁有的 `DbContext`，每個 context 可各保留一份 migration，並維持既定套用順序。只有 migration 已部署、需要分階段資料搬移或營運上必須分段上線等特殊理由，才可在同一 context 保留多份，且必須在 pull request 說明原因。
- Seed 只放穩定的系統參考資料，不在 migration 隱藏環境或使用者資料。
- Transaction 以 application use case 為邊界，不得跨外部網路呼叫長時間持有 database transaction。

## Validation、Error 與 Authorization

- API boundary 驗證 request shape、必填、格式與基本範圍。
- Application 或 Domain 驗證業務 invariant 與狀態轉移。
- Endpoint 不重複撰寫授權邏輯，改用 policy 與明確的 project membership check。
- 系統全域角色與 project scope role 必須分開。
- Client 需要依業務錯誤分支時，error identifier 必須穩定且可由機器判斷；人類訊息不是 identifier。
- 安全與重要業務狀態變更必須 audit，但不得記錄 credential 或 secret value。

## 前端規則

### i18n 邊界

- i18n 只處理產品與系統提供的介面文字，例如頁面標題、導覽、欄位名稱、按鈕、狀態名稱、說明、placeholder、驗證、錯誤與通知。
- 使用者或外部資料寫入的實際內容不得翻譯，例如專案名稱與代號、Issue 標題與描述、使用者名稱、留言、測試案例內容及自由輸入欄位。
- 動態內容可以作為翻譯訊息的參數插入，但參數值必須保持原文。

### 寫入操作回饋

- 所有成功的新增與修改操作，都必須呼叫共用 `useSaveNotice`，由應用程式層唯一的 `SaveNoticeHost` 在右下角顯示通知；通知預設五秒後消失，且允許使用者提前關閉。
- 一般成功通知不得直接寫在頁面內容流中，也不得由各頁自行複製 Toast 樣式。
- 當新增或修改後有一次性資訊、重要指示、不可忽略的後續步驟或需要使用者確認的內容時，除右下成功通知外，應使用共用 `UiActionDialog`。Dialog 內容使用 slot 擴充，不為單一功能複製 modal。
- Dialog 必須有遮罩、鍵盤焦點、`Esc` 關閉、明確關閉按鈕與 `aria-modal`；是否允許點擊遮罩關閉由使用情境設定。
- 支援連續新增的表單，必須使用共用 `UiCreateActions`，提供「建立」與「建立並繼續」。前者完成既定導向或關閉表單；後者清空成功建立的資料、保留新增畫面並將焦點移回第一個欄位。
- 一次性敏感資訊不可放入五秒通知；通知只顯示成功與識別資訊，敏感或需保存內容放在 `UiActionDialog`。

- 使用 Vue 3、TypeScript strict mode、Composition API 與 `<script setup lang="ts">`。
- Component 使用 PascalCase，composable 使用 `use` 前綴，資料夾依 feature 組織。
- 不使用 `any` 或未檢查的強制轉型。TypeScript API model 必須能逐項對應 canonical OpenAPI，不得在 feature 內建立另一份重複 wire type。
- View 與 store 透過 feature API module 或 composable 呼叫 API，不直接使用 HTTP client。
- 只有跨 component 的 application state 才放 shared store；頁面內狀態留在頁面或 page-scoped composable。
- 工作流程需要時，component 必須包含 loading、empty、error 與 disabled state。
- Accessibility、鍵盤操作、responsive layout 與清楚的 focus state 都屬於功能完成條件。

## 測試

- 測試外部可觀察行為與公開 contract，不測 private implementation detail。
- Bug fix 在可行時加入 regression test。
- Unit test 必須具決定性，不依賴真實 database、queue、外部 API、目前時間或任意 sleep。
- Integration test 驗證 HTTP pipeline、authorization、EF Core mapping、migration 與 infrastructure boundary。
- 需要驗證 relational behavior 時，優先使用 PostgreSQL 相容的 integration environment，不使用 EF in-memory provider 取代關聯式資料庫語意。
- Contract test 保護 route、status code、problem details、JSON shape、nullability 與 TypeScript client。
- Arrange、Act、Assert 能提升可讀性時應採用，但不加入只重述程式碼的註解。

## Git 與 Review

- Commit message 遵循 Conventional Commits。
- Commit 與 pull request 應保持單一目的且容易審查。
- 除非同一變更確實需要，不要混合 formatting、dependency upgrade、refactor 與功能行為。
- Pull request 說明行為、設計取捨、migration 影響與驗證結果。
- 送 review 前執行目前可用的 build、test、format 與 static analysis。
