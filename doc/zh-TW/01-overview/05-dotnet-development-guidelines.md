# .NET 開發準則

> 狀態：Accepted。這份文件定義已採用的開發基線；SDK 選版已由 `global.json` 與 CI 落實，其餘 MSBuild、NuGet、analyzer 與 format 限制仍會在後續獨立變更處理。

## 目的

這份文件定義 KhaiKang 後端可共同維護的 .NET 工程基線。規則以開源協作、可驗證性與長期相容性為優先，不依賴特定公司的套件、命名或部署環境。

本文中的關鍵字含義如下：

- **必須**：合併前必須符合，除非已有 ADR 記錄例外。
- **應該**：預設做法；偏離時應在 pull request 說明理由。
- **可以**：依模組需求選用，不要求全專案一致採用。

## 最小開發基線

這套規則的目的不是建立固定樣板，而是讓不同貢獻者能安全地修改同一個 codebase。

- 使用標準 .NET CLI 即可 restore、build 與 test，不要求安裝公司內部工具或共用套件。
- 不強制 Planner、Builder、Reviewer、handoff 文件或特定 AI 工作流程。
- 不要求每個 service 都有 interface、base class、factory、repository 或 partial class。
- 小型功能可以保持小型；只有真實複雜度出現時才增加 abstraction 或分層。
- 只有安全性、公開 contract、資料 migration、模組邊界與行為正確性屬於不可省略的 review gate。
- 尚未由 compiler、analyzer 或 CI 自動檢查的項目，先視為 review guidance，不因個人偏好阻擋貢獻。
- 新增全專案規則前，應先確認它解決的是重複發生的問題，而不是單次實作偏好。

## 工具鏈與建置

- 使用 .NET 10 與 C# 14。
- `global.json` 是 SDK 版本與 preview policy 的 source of truth；不得使用 preview SDK 建置正式分支。
- `Directory.Build.props` 是全專案 compiler、analyzer 與 warning policy 的 source of truth。
- `Directory.Packages.props` 集中管理 NuGet 套件版本；一般 `.csproj` 不直接指定 `Version`。
- Nullable reference types、.NET analyzers 與 code style build enforcement 必須啟用。
- 正式 CI 應將 compiler 與已採用 analyzer 的 warning 視為 error；本機仍使用一般 `dotnet build`，不要求額外 wrapper script。
- 不得用全域 `NoWarn` 隱藏問題；必要的局部 suppression 必須包含具體理由。
- `dotnet restore`、`dotnet build` 與 `dotnet test` 是目前後端 pull request 的基本驗證。
- E07 建立全 repository 格式 baseline 前，修改過的 C# 檔案必須使用 `dotnet format --verify-no-changes --include <paths>` 檢查，且不得增加既有格式債務；目前尚不把全 solution format 驗證列為 pull request gate。

## Project 與依賴方向

KhaiKang 是 modular monolith。Project reference 用來建立明確依賴方向，不代表每個 project 都獨立部署。

```text
KhaiKang.Api
  -> Feature modules
  -> KhaiKang.CommonUtils.Web

KhaiKang.Worker
  -> Feature modules
  -> KhaiKang.CommonUtils

KhaiKang.CommonUtils.Web
  -> KhaiKang.CommonUtils
```

- API 與 Worker 只負責 composition、hosting 與 cross-cutting pipeline，不放商業規則。
- 功能模組預設是一個 `KhaiKang.Modules.<Feature>` class library；只有出現真實程式碼時才建立 `Domain`、`Application`、`Infrastructure`、`Endpoints` 資料夾。
- Domain code 不得依賴 ASP.NET Core、EF Core implementation、HTTP DTO 或外部系統 SDK。
- 功能 HTTP DTO 留在所屬模組，只有 host 使用的 DTO 留在 host；其 wire shape 必須實作 canonical OpenAPI contract。
- CommonUtils 必須保持 domain-neutral，且至少有兩個實際呼叫端後才抽入。
- 模組不得直接引用其他模組的 entity、DbContext configuration、endpoint type 或 internal implementation。

## C# 程式碼

- 使用 file-scoped namespace、四個空格縮排與完整大括號。
- 每個 top-level public class、record、interface、enum 或 struct 各自使用一個檔案，檔名必須與型別名稱相同；只服務所屬 type 的小型 private nested type 可以例外。
- 同一 layer 內，同一 use case 的型別放在同一個以業務命名的資料夾；例如 `Contracts/IssueCommands/` 應一起放置 Command、Outcome、Result 與 Interface。業務資料夾維持在 layer root 下 1 至 2 層。不得建立名稱為 `Enums`、`Interfaces`、`Requests`、`Responses` 或 `Results` 的純技術資料夾；`IssueCommands` 這類名稱代表業務 use case，不屬於技術分類。真正跨多個資源共用的 coordinator 或 constants type，在找到有意義的 owner 前可以暫留 layer root。
- Checked-in generated C# source 也遵守相同的 public type 與檔案配置；只有 accepted generator policy 明確定義的範圍可以例外。EF Core migration 可以保留必要的 timestamp-prefixed 檔名，但仍須遵守一檔一個 top-level public type；architecture check 不得靜默排除整個 generated 目錄。
- Public API 使用 PascalCase，parameter 與 local 使用 camelCase，private field 使用 `_camelCase`。
- 非同步方法使用 `Async` 後綴；ASP.NET endpoint handler 與 framework override 可以依慣例例外。
- 所有公開邊界 contract 都必須宣告明確的 type body 與具名 property；HTTP request、response、query、跨模組 command、directory entry 與公開 Application result 都不得使用 positional record。一至三個語意清楚的 input 必填欄位可以使用明確 constructor 與 getter-only property；必填欄位更多的 input 與 response model 使用 `required` init-only property，避免形成長 constructor。Optional 欄位使用 init-only property。只有小型 Domain Value Object 與真正 internal 的 implementation carrier，在 positional 形式確實更清楚時才能保留。
- HTTP request body contract 命名為 `{Verb}{Resource}Request`；query／filter model 可以使用能表達意圖的 `Query` 名稱。跨模組 Application input 命名為 `{Verb}{Resource}Command`，其 output 使用完整 command 名稱，例如 `CreateIssueCommandOutcome` 與 `CreateIssueCommandResult`。其他 Application result 依 use case 或有共同語意的操作族群命名。公開 Result 使用明確 type body；nullable payload 可能形成不合法狀態時，使用 private constructor 與具名 success／failure factory。沒有 payload 的操作可以直接回傳 Outcome。
- 只有 caller 不會面對不可能值、衝突的 payload invariant 或模糊 mapping 時，才共用 Outcome／Result。範圍過大的 `MutationOutcome` 應改成 use-case-specific type；若一組有共同語意的操作真正共享相同 contract，也不得機械式為每個 method 建立一組新型別。
- 必填 reference member 使用 constructor 或 `required`，不得用 `string.Empty` 或 null-forgiving operator 偽裝完成初始化。
- Public collection 優先暴露 `IReadOnlyList<T>`、`IReadOnlyCollection<T>` 或 `IEnumerable<T>`；不要公開可替換的 `List<T>`。
- 沒有設計為擴充點的 class 應考慮 `sealed`，但 EF Core 或測試需求可以保留繼承能力。
- Primary constructor 可以使用，但不是強制規則；可讀性與依賴清楚度優先。
- Domain 中封閉的狀態、類型、角色或結果必須使用 enum 或 Value Object 表達；未驗證的字串不得一路傳入 Domain 或 Application 邏輯。
- 程式碼應使用領域語言，並依序呈現判斷與流程。若較短的語法、深層巢狀 expression 或語法糖讓業務流程更難閱讀，就不算改善。
- 新增的 public Domain 與 Application API 原則上不得超過三個業務參數；`CancellationToken`、dependency-injection constructor 與 framework-required signature 不計入。此限制不得機械式套用到 private helper：清楚命名的少量 private 參數比一次性參數袋更容易閱讀。既有長參數 API 視為待清理債務；確有必要的 public 例外必須在 pull request 說明，持續性例外則以 ADR 記錄。
- 多個同型別 primitive、`string`、`Guid` 或時間參數若容易放錯位置，應以具領域名稱的 immutable Parameter Object、Value Object 或 Strongly Typed ID 組合。Parameter Object 必須代表 use case、domain concept、共同生命週期，或在有意義的邊界上承載 invariant。不得建立後立即拆回原參數而只為符合數量門檻；也不得只因具名 property 數量多，就把內聚的 creation／change model 再拆成更多小型別。
- 建立與修改同一概念的資料應使用不同且能表達意圖的 type，例如 `IssueCreation` 與 `IssueDetailsChange`；HTTP request DTO 不直接充當 Domain Parameter Object。
- Mutation context 建立後應在 use case 中完整傳遞，不拆成 actor 與發生時間後再於下游 helper 重建相同 context。
- Helper、Factory、Command 與 abstraction 必須增加領域語意、驗證、政策或已證明的重用。避免只有轉呼叫的 helper，也不以 null、optional mode value 或 boolean flag 讓一個 helper 代表不同 use case；應先拆成能表達意圖的操作，只抽出真正相同的規則。
- 多個 entity 真正共享相同 audit lifecycle 時，可以使用一層淺層的 `AuditableEntity` 基底集中 `CreatedAt`、`CreatedByAccountId`、`UpdatedAt`、`UpdatedByAccountId` 與 `Version`，以及一致的初始化與異動方法。不得建立只為形式統一的深層 entity 階層。
- 業務生命週期欄位不屬於通用 audit metadata，例如 `CompletedAt` 應保留在 `Issue`；共用基底不得吸收 feature-specific state 或規則。
- XML documentation 用英文撰寫，只要求在公開 extension point、library API 或無法從名稱理解的 contract；不強制替每個 public member 重述程式碼。
- 不加入只為減少幾行程式碼的 abstraction、base service 或 helper。

## Dependency Injection

- 依賴必須透過 constructor injection 表達。
- 禁止 property injection 與在業務程式碼使用 `IServiceProvider` 作為 service locator。
- 每個功能模組提供單一明確的 registration entry point，例如 `AddProjectManagementModule`。
- Singleton 不得直接依賴 scoped service；service lifetime 必須符合持有的 state 與 thread-safety。
- 不在 endpoint 或 application use case 中直接 `new` database、HTTP、clock、queue 等外部依賴。
- 不強制所有 service 建立 interface；只有多實作、跨模組 contract、測試替身或穩定 boundary 確實需要時才新增。

## Async、取消與時間

- Database、network、file 與 queue I/O 必須使用 async API。
- 公開 application use case 與 endpoint 必須接受並傳遞 `CancellationToken`；不得以 `default` 取代呼叫端 token。
- Library code 不使用 `.Result`、`.Wait()` 或任意 `Thread.Sleep`。
- 儲存與跨系統交換時間時使用 UTC 與 `DateTimeOffset`。
- 需要目前時間的商業邏輯注入 `TimeProvider`，避免直接依賴 `DateTime.Now` 或不可控制的 `UtcNow`。

## Configuration 與 Secret

- 使用標準 .NET Configuration pipeline 與 Options Pattern。
- Options type 由實際使用設定的 host 或功能模組擁有。
- 使用 `BindConfiguration` 綁定、validation 檢查，必要設定使用 `ValidateOnStart`。
- Feature code 透過 `IOptions<T>`、`IOptionsSnapshot<T>` 或 `IOptionsMonitor<T>` 取得設定。
- 不建立全專案環境變數 enum/helper，也不在 feature code 直接呼叫 `Environment.GetEnvironmentVariable`。
- `appsettings.json` 只放非敏感預設值；本機 secret 使用 User Secrets，部署環境使用環境變數或 secret provider。
- 不得將 secret、token、真實帳密、production URL 或 production data 提交到 repository。

## HTTP API 與 Contract

- API route 使用 `/api/v1` 與資源導向命名。
- Minimal API 與 controller 都可使用，但同一功能模組應維持一致，且 endpoint 不承載商業規則。
- 成功直接回傳 resource 或 operation DTO，不使用通用 `ApiResponse<T>` envelope。
- 建立 resource 應使用 `201 Created` 與 location；刪除成功且無 body 時使用 `204 No Content`。
- 錯誤使用 RFC 9457 Problem Details。HTTP status 是主要協議，穩定的 `type` 與必要時的 `code` extension 提供 machine-readable identity。
- Trace ID、stack trace、exception message 與 connection detail 不屬於公開 API contract。
- Application 與 Domain 不得引用 `ProblemDetails` 或 HTTP status；Web boundary 集中映射 application error semantics。
- Request shape 在 HTTP boundary 驗證，業務 invariant 在 Application 或 Domain 驗證。
- OpenAPI 是 HTTP wire contract 的 source of truth。Endpoint 必須宣告實際 request、response、status 與 authorization metadata。
- Client 會觀察到的 route、status、JSON shape、nullability 或 enum 變更，都視為 contract change 並需要測試。

## Persistence

- PostgreSQL 是 system of record，EF Core 是預設 data access technology。
- 初期直接使用明確的 `DbContext` 與 feature query/use case；不預設建立 generic repository 或 unit-of-work wrapper。
- 封閉的 enum 或 Value Object 必須以穩定英文 code 寫入資料庫，不得儲存 enum 整數值或語系化顯示文字。轉換只存在於 persistence 與 transport boundary；已持久化的 code 若要重新命名，屬於資料契約變更，必須有明確 migration 或相容方案。
- 穩定 code 的字串值必須在所屬 mapping type 集中宣告為具語意的 `private const string`，並由序列化與解析共用；不得在多個分支重複 literal，也不得以 enum 名稱、大小寫轉換或 reflection 自動推導持久化 code。
- 由資料表管理的分類資料必須分開保存穩定英文 `code` 與使用者顯示用 `name`。業務資料以分類主鍵關聯；API 在需要機器可讀識別時回傳穩定 code。
- EF Core 查詢應保持明確且容易順讀。Collection use case 原則上依序呈現正規化、過濾、排序、計數、分頁、投影與執行；當 predicate 或排序規則遮蔽主流程時，抽成 feature-local 且能表達意圖的方法，不以 generic query-options abstraction 隱藏一般 EF 行為。
- Entity 與 EF Core configuration 由所屬功能模組管理；其他模組不得直接修改其 table。
- Schema 變更必須有 migration；正式啟動流程不得使用 `EnsureCreated` 代替 migration。
- 功能分支準備合併主線前，每個受影響的 `DbContext` 只保留一份尚未發布的最終 migration。同一 context 若需保留多份，必須是已部署 migration、分階段資料搬移或分段上線等特殊情境，並在 pull request 記錄原因。
- Read-only query 應使用 projection，並在不需要 tracking 時使用 `AsNoTracking`。
- 避免 N+1 query、無界限 collection 與先載入完整 entity 再丟棄大部分欄位。
- Database transaction 以 application use case 為邊界，不跨外部 network call 長時間持有。
- 關聯式行為測試不得使用 EF Core in-memory provider 代替 PostgreSQL 語意。

## Logging 與 Observability

- 使用 `ILogger<T>` 與 structured message template，不使用字串插值組合 log。
- 高頻或效能敏感路徑應使用 `LoggerMessage` source generator；一般低頻 log 不強制拆成 partial logging file。
- Log 應包含可操作的 domain identifier 與 outcome，但不得記錄 credential、token、完整個資或敏感 payload。
- 預期的 validation、not-found 或 conflict 不應全部記為 Error。
- 不要 catch exception 只為 log 後原樣 rethrow；未預期例外由 boundary 統一記錄。
- Trace context 保留在 observability pipeline，不承諾為公開 response body 欄位。

## Testing

- 使用 xUnit。測試名稱以英文描述情境與預期結果，例如 `CreateProject_WhenNameIsDuplicate_ReturnsConflict`。
- 測試外部可觀察行為，不測 private method 或實作步驟。
- Bug fix 在可行時先建立失敗的 regression test，再修正並確認通過。
- Domain 與 application unit test 不得連線 database、queue、network 或依賴 wall-clock time。
- HTTP pipeline 使用 `WebApplicationFactory` integration test。
- Persistence、migration 與 PostgreSQL-specific behavior 使用可重現的 PostgreSQL integration environment。
- 測試不得依賴執行順序、任意 sleep、開發者機器資料或已存在的外部服務。
- Arrange、Act、Assert 能提升可讀性時使用空行分段，不加入只重述程式碼的註解。
- 新功能至少涵蓋成功路徑、主要 validation、authorization 與重要 conflict/not-found contract。
- 漸進式重構應先建立 characterization tests，保護對外行為、業務 invariant、audit metadata 與 optimistic concurrency version，再改變內部結構。
- 尚未清除的架構債務可以用明確 allowlist 建立 fitness test；allowlist 只記錄既有債務，不得讓新程式碼擴大，且每完成一項重構就必須同步移除對應項目。

## Dependency 治理

- 優先使用 .NET／ASP.NET Core／EF Core 內建能力與專案現有套件。
- 新套件必須解決明確問題，並在 pull request 說明維護狀態、license、transitive dependencies 與替代方案。
- 不預設加入 mediator、AutoMapper、generic repository、result-wrapper 或 validation framework。
- 套件版本只在 `Directory.Packages.props` 更新，Microsoft runtime-related package 應維持相同 patch family。
- NuGet vulnerability audit 不得因方便而全域停用。

## 完成條件

後端變更送出 review 前，至少確認：

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet build backend/KhaiKang.Backend.slnx --configuration Release --no-restore
dotnet test backend/KhaiKang.Backend.slnx --configuration Release --no-build
```

若本批修改 C#，還需以 `--include` 將 `dotnet format` 限定在受影響路徑。
全 solution format gate 會等 E07 記錄並清除既有 baseline 後再啟用。

若因環境限制無法執行其中一項，pull request 必須說明未執行項目、原因與替代驗證。
