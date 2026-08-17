# 後端可讀性重構執行追蹤

> 狀態：Active
>
> 最後更新：2026-08-13
>
> 適用分支：`ecohover/refactor/backend-clean-code`
>
> 正式工程規範：[.NET 開發準則](../01-overview/05-dotnet-development-guidelines.md)
>
> 重構方向：[後端 Domain 可讀性重構計畫](./10-backend-domain-refactoring-plan.md)

## 目的

本文件是後端重構工作的單一執行狀態來源，讓人類或 AI 在工作中斷後，可以從明確的 Action Item、決策、驗證紀錄與 Resume Point 接續，不必依賴聊天紀錄。

正式規則仍以公開工程文件、OpenAPI、測試與目前程式碼為準。本文件只管理工作狀態，不建立另一套工程規範。

## 開發模式

KhaiKang 採用下列優先順序：

1. **AI-first instruction**：規範應容易定位、按需載入，並清楚說明適用範圍與例外。
2. **Machine-first enforcement**：能由 compiler、analyzer、test 或 CI 驗證的規則，不只依賴文字與 AI 判斷。
3. **Human-readable code**：程式碼以人類能檢查業務意圖為優先，不以較短或較多語法糖作為品質目標。

開源貢獻流程保持 tool-neutral，不要求外部貢獻者使用特定 AI、私有 prompt 或公司套件。

## 責任分工

| 角色 | 責任 |
| --- | --- |
| Human owner | 決定業務語意、風險接受、長期例外、合併與發布 |
| AI Builder | 盤點、補 characterization test、實作、驗證、文件同步與經授權的本機 commit |
| AI Reviewer | 不沿用 Builder 結論，獨立檢查 diff、規範、測試證據與過度抽象 |
| CI | 執行可自動化的 build、format、test、contract 與 architecture gate |

## 狀態定義

| 狀態 | 說明 |
| --- | --- |
| `pending` | 尚未開始 |
| `in-progress` | 目前批次正在處理 |
| `waiting-human` | 需要人類決定業務語意或風險 |
| `blocked` | 有明確環境或外部條件阻礙 |
| `verified-awaiting-checkpoint` | Implementation、驗證與 review 已完成，但尚未建立經授權的 checkpoint commit |
| `completed` | 驗收條件通過且已記錄證據 |

## 已接受決策

| ID | 決策 | 狀態 |
| --- | --- | --- |
| D01 | AI 是主要開發與檢視工具，但 compiler、tests、CI 與 accepted spec 才是驗證來源 | accepted |
| D02 | 公開工程規範保持 tool-neutral；AI 操作流程只放在 `AGENTS.md` 與 `.ai/` | accepted |
| D03 | 三參數規則只套用新增的 public Domain／Application 業務 API；`CancellationToken`、framework-required signature 與 private helper 不計入 | accepted |
| D04 | Parameter Object 必須代表 use case、domain concept 或共同生命週期，不為通過數量門檻建立一次性資料袋 | accepted |
| D05 | Public HTTP request 與跨模組 Application input 統一使用明確 type body 與具名 property，不使用 positional record；短必填 contract 使用明確 constructor，較多必填欄位使用 `required init` | accepted |
| D06 | Helper／Factory／Command 必須增加語意、驗證或真實重用；不建立純轉接抽象 | accepted |
| D07 | 外部或私有 repository 只作為經驗參考，不複製公司程式碼、套件假設或 AI 工作流程依賴 | accepted |
| D08 | 同一 feature branch 每個受影響 `DbContext` 原則上只保留一份未發布的最終 migration | accepted |
| D09 | 每個 top-level public type 各自一檔且檔名相同；所有公開邊界 contract 統一使用 body-form，包含 Request／Response／Query／Command／Directory Entry 與公開 Application Result；只有 Domain 或真正 internal 的小型 immutable carrier 可保留 positional | accepted |
| D10 | 同一 use case 的 Command／Outcome／Result／Interface 放在同一個業務資料夾；業務目錄維持在 layer root 下 1 至 2 層，不建立 `Enums`／`Interfaces`／`Requests`／`Responses`／`Results` 純技術資料夾；跨資源 coordinator／constants 可暫留 layer root | accepted |

## Active Principle Workstreams

重構以全 repository 原則為完成單位。Feature、module、resource 或資料夾只是在同一原則下控制 characterization、review、rollback 與 commit 風險的 checkpoint；完成單一 checkpoint 不得把全域原則標為完成。

| ID | 原則與適用範圍 | AI Builder 責任 | Human 責任 | 狀態 | 全 repository DoD | 下一個 checkpoint |
| --- | --- | --- | --- | --- | --- | --- |
| P01 | 重構安全網：unit、integration、boundary characterization、external dependency isolation | 盤點 coverage、補 observable behavior 測試、維護 test evidence | 確認預期行為與相容風險 | in-progress | 所有被重構行為在正確 boundary 有保護，測試不鎖 private implementation | 依 active principle 補 operation matrix |
| P02 | Domain／Application API 可讀性：三參數、同型 primitive、creation／change／context model | 維護 exact debt baseline，分 semantic family 重構 | 核准例外與 domain concept | in-progress | 長參數與模糊 primitive 債務歸零，或例外有 ADR 且 baseline 不增加 | I01 characterization 後處理 Identity API |
| P03 | 公開 boundary contract：body-form、一型別一檔、required／nullable／wire compatibility | 維護 architecture／contract tests與跨模組 caller | 核准 public contract 變更 | in-progress | 全部 public boundary 符合規則，OpenAPI／TypeScript／runtime 一致，例外有明確決策 | 處理 Workspace canonical drift 與 C14／C15 |
| P04 | Result／Outcome 語意：真正 operation family、payload invariant、完整 endpoint mapping | 建立全 repo inventory、characterization、垂直切片實作與 exact no-growth baseline | 決定哪些 operations 真正共享語意，核准錯誤契約變更 | in-progress | Broad Mutation／generic Result 債務歸零或例外有 ADR；不存在 impossible outcome／payload、假 `object` payload、未核准任意業務錯誤字串 | 完成 C17-1 reachable outcome／HTTP mapping characterization matrix |
| P05 | 封閉狀態與 stable code：enum／Value Object、DB English code、table code/name | 盤點 string status與 mappings，補 round-trip test | 決定狀態集合與資料相容策略 | in-progress | 封閉狀態都有型別保護，資料庫與 client code 穩定且 mapping 可驗證 | 完成 Test Management 狀態 inventory |
| P06 | Audit lifecycle 與 mutation context：actor／time／version共用邊界 | 盤點 lifecycle/nullability，補 audit/concurrency tests | 決定真正共用 lifecycle 與例外 | waiting-human | 共用 lifecycle一致、feature lifecycle留在 owner、EF mapping與 concurrency不變 | R06／I04 Human decision |
| P07 | EF query／paging 可讀性：normalize→filter→order→count→page→project→execute | 盤點遮蔽流程的 abstraction並提出 feature-local 改寫 | 核准有價值的共用 abstraction | pending | 查詢可依業務流程閱讀，沒有為統一外觀導入 generic query framework | 建立 query inventory |
| P08 | 抽象節制：helper／factory／interface／repository／base／parameter object | 提供語意、invariant或真實reuse證據；移除純轉接 | 判斷 abstraction 是否值得保留 | in-progress | 所有抽象有可說明價值，純轉接債務歸零或有核准例外 | C07／C08 inventory |
| P09 | 實體組織：resource／use-case folder、一型別一檔、禁止技術分桶 | 維護 source architecture gate | 判斷跨資源 owner與例外 | completed | 全 repo結構符合規則且 architecture allowlist不增加 | 由 architecture gate 持續防回歸 |
| P10 | Enforcement／CI／migration policy：compiler、analyzer、format、contract、branch migration | 建立低誤判客觀 gate並逐批清債 | 核准 warning policy與 migration例外 | in-progress | 客觀規則進 CI、baseline只能縮小、每個受影響 DbContext合併前只有一份未發布最終 migration或有核准例外 | E06／E07 baseline |

所有原則共用的完成條件：inventory 歸零或剩餘例外有 Human 核准與 durable rationale；適用的 Release build、unit、integration、format、contract／EF check 通過；獨立 review 無未處理 blocker／high／medium；Tracker 記錄證據與下一步。

## 風險 Gate

| 等級 | 例子 | 必要 Gate |
| --- | --- | --- |
| Low | 文件、命名、無行為差異的小型重構 | AI Builder、相關自動檢查、人工抽查 |
| Medium | 一般功能、Application flow、API 行為但不涉及敏感資料 | AI Builder、獨立 AI Review、CI、人工批准 |
| High | 權限、資料刪除、migration、安全、發布與資料相容性 | characterization／regression test、獨立 AI Review、完整 CI、人工審查與明確批准 |

## Action Items

### R：規則決策

| ID | Action Item | Owner | 狀態 | 驗收條件 |
| --- | --- | --- | --- | --- |
| R01 | 定義 Low／Medium／High 風險 Gate | Human + AI | completed | 本文件記錄分類與人工 Gate |
| R02 | 釐清三參數規則適用範圍 | Human + AI | completed | 英文與繁中 .NET 規範同步 |
| R03 | 定義 Parameter Object 成立條件 | Human + AI | completed | 正式規範包含反過度包裝條文 |
| R04 | 統一 public request／input contract 宣告方式 | Human + AI | completed | 雙語正式規範不再允許 public request／input 使用 positional record |
| R05 | 定義 Helper／Factory／Command 建立門檻 | Human + AI | completed | 正式規範包含真實語意與重用門檻 |
| R06 | 決定 `ProjectDetailsChange.Status` 等欄位是否應拆成獨立狀態轉移 | Human | waiting-human | 業務語意確認並記錄決策 |
| R07 | 定義 public type 檔案、資料夾與 Result／Outcome 風格 | Human + AI | completed | 雙語規範要求所有公開 boundary type 使用 body-form；Result 的 nullable payload 以 factory 排除不合法狀態 |

### E：規範與 Enforcement

| ID | Action Item | Owner | 狀態 | 驗收條件 |
| --- | --- | --- | --- | --- |
| E01 | 建立規則與 enforcement 對照 | AI | completed | 本文件的 Enforcement Matrix 可指出自動與人工 Gate |
| E02 | 更新公開 .NET 開發規範 | AI + Human review | completed | 英文與繁中同步且不綁特定 AI |
| E03 | 更新既有後端重構計畫與目前債務 | AI | completed | 文件反映目前實際進度 |
| E04 | 建立薄型 AI backend-refactoring 路由 | AI | completed | `.ai/` 只連結正式文件與本追蹤文件 |
| E05 | 鎖定 stable .NET 10 SDK | AI + Human | completed | `global.json`、CI 與 Docker build stage 對齊 `10.0.400`；升版必須經過明確變更 |
| E06 | 集中 compiler／analyzer policy | AI + Human | pending | 先產生 warning baseline，再分階段提高強度 |
| E07 | Backend CI 加入 format 與必要 gate | AI | pending | 先以 `.gitattributes`／checkout policy 對齊 LF baseline，再於 PR 執行 format、build、unit、integration |
| E08 | Architecture fitness test 納入 Application API | AI | pending | 排除 `CancellationToken`、DI 與 framework signature |
| E09 | 維護規範債務基線 | AI | in-progress | 現有 allowlist 不得增加，完成項目立即移除 |
| E10 | 防止純技術資料夾分類回歸 | AI | completed | Source architecture test 阻擋五個純技術目錄名稱且允許 `IssueCommands` 等業務 use-case 名稱；完整 regression、獨立 review 與 checkpoint 已完成 |

### C：修正過度精簡與假抽象

| ID | Action Item | Owner | 狀態 | 驗收條件 |
| --- | --- | --- | --- | --- |
| C01 | 移除 Identity 一次性 `AuditContext` | AI | completed | Named factories 保留，私有流程不再為參數數量包裝 |
| C02 | 移除 `ProjectAuditEvent.Create` 純轉接 helper | AI | completed | Named factory 直接建立 entity，行為不變 |
| C03 | `ChangeContext` 建立後完整傳遞 | AI | completed | 不再拆 actor/time 後於 helper 內重建 |
| C04 | 將 member Add／Update validator 分開 | AI | completed | 不以 null／optional 值切換驗證模式，補 null regression test |
| C05 | 修正 `RoleCodes` 集合語意 | AI + Human contract review | completed | Request 使用無順序集合語意、response 保留排序語意，runtime 落實 OpenAPI uniqueness |
| C06 | 盤點大型 positional requests | AI | completed | 已完成 high／medium／keep 分級與五個後續小批次 |
| C07 | 盤點 Application 直接使用 HTTP DTO | AI + Human | pending | 只在多入口或規則複雜時增加 application input model |
| C08 | 盤點純轉接 helpers | AI Reviewer | pending | 每項有明確移除理由，不進行機械式刪除 |
| C09 | Project Management Issue request 改為 body-form public contract | AI + Human contract review | completed | 必填欄位使用短 constructor、optional 欄位使用具名 init；JSON、HTTP 與 OpenAPI shape 均維持 |
| C10 | Test Case request 改為 body-form public contract | AI + Human contract review | completed | Create／Update／Step 已統一宣告形式；required、optional nullable 與 tag 三態語意已對齊 OpenAPI／TypeScript 並完成 regression matrix |
| C11 | Test Plan request 改為 body-form public contract | AI + Human contract review | completed | 已隨 C18 完成宣告、wire characterization 與 checkpoint |
| C12 | Run Bug 與 Issue application bridge request 調整 | AI + Human contract review | completed | HTTP、跨模組 application contract、完整 regression 與 checkpoint 已完成 |
| C13 | Suite／Workspace／Tag 與中型 request 後續整理 | AI | completed | 已隨 C18 完成宣告、contract shape tests 與 checkpoint；既有 Workspace canonical drift 另案處理 |
| C14 | 落實 OpenAPI `additionalProperties: false` | AI + Human contract review | pending | 先盤點未知欄位的相容風險，再讓 runtime 與全域 canonical contract 一致 |
| C15 | 統一 validation error key casing | AI + Human contract review | pending | 明確選擇 JSON camelCase 或 CLR property casing，建立相容策略與 regression matrix |
| C16 | 清理其餘 public positional request／input contracts | AI | completed | 三模組 public request／input 已全部轉換、allowlist 歸零並完成 checkpoint |
| C17 | 拆分聚合 public type 檔案並收斂 operation Result／Outcome | AI + Human contract review | in-progress | 一型別一檔與 payload invariant 已落實；operation-family superset outcome／code 仍需後續語意重構 |
| C18 | 全盤標準化公開邊界 contract | AI + Human contract review | completed | 三模組與 API host 公開 boundary contract 已 body-form、三類 architecture allowlist 歸零、完整驗證與 checkpoint 已完成 |
| C19 | Contracts／Application 依業務資源與 use case 分類 | AI + Human review | completed | Identity 26、Project Management 49、Test Management 14，共 89 個檔案依業務路徑整理；namespace／API／production behavior 不變且已完成 checkpoint |

### C17 Result／Outcome 全域語意子項

C17 是 P04 的 umbrella item；一型別一檔與資料夾整理已分別歸 P03／P09，不代表 Result／Outcome 語意已完成。不得要求每個 method 機械建立獨立 Result；只有 outcome 集合、payload invariant 或 caller mapping 不同時才拆分。

| 子項 | Scope | 狀態 | 完成條件 |
| --- | --- | --- | --- |
| C17-1 | 全 repo operation inventory | in-progress | 每個 Application operation 記錄實際可達 Outcome、成功 payload／no payload、error identity、endpoint mapping、caller、tests與 keep-family／split／waiting-human 決策；operation identity baseline 已完成，reachable outcome／HTTP mapping test matrix 待補 |
| C17-2 | Project mutations | in-progress | Project Member reachable outcome／HTTP mapping characterization 已完成；Issue、Project Member、Issue Relation 仍須依真正 semantic family 收斂，不可能 Outcome 與 payload 組合歸零 |
| C17-3 | Attachments | pending | Upload、Delete、OpenContent 依不同 payload invariant 與 reachable failures 建模 |
| C17-4 | Test Management | pending | `TestManagementResult<T>` 依 resource／operation family 分批移除，不以新 generic Result重新包裝 |
| C17-5 | Completion gate | pending | Broad Result exact baseline歸零或有核准ADR；無假 `object` payload、未核准任意業務錯誤字串、漏接 Outcome mapping |

### C17-1 Application Result／Outcome Inventory Baseline

2026-08-13 source inventory 共找到 60 個使用 Application Result／Outcome 的 public operations。8 個已採 operation-specific model 且 payload invariant 已由 factory／direct Outcome 表達；52 個仍列為 broad Result semantic debt。這 52 個 operation identity 已寫入 `ApplicationResultSemanticBaselineTests`，新債務或已解決但未移除的 baseline entry 都會使 architecture test 失敗。

| 語意族群 | Operations | 目前模型 | 成功 payload | 初步決策 | 尚需 characterization |
| --- | ---: | --- | --- | --- | --- |
| Identity account／authentication | 5 | operation-specific Result／Outcome | 依 use case 為 Account、Session＋User 或 no payload | `keep-family`；納入 completion review | 每個 reachable failure 與 endpoint mapping 完整性 |
| Project create／update＋cross-module create issue | 3 | operation-specific Result／Outcome | Project 或 Issue；factory 排除 invalid payload | `keep-family`；`CreateIssueCommand` 的 defensive mapping 另檢查 | catch-all／upstream impossible mapping characterization |
| Project Members | 3 | `ProjectMemberMutationResult` superset | Add／Update 有 Member；Remove 無 payload | `split` | Add／Update／Remove 的 reachable failure、HTTP status／problem code |
| Issues | 4 | `IssueMutationResult` superset | 全部回 Issue，但 outcome 集合不同 | `split` 或經 Human 核准的 semantic family | Create／Update／Status／Assignee outcome matrix與跨模組 caller |
| Issue Relations | 2 | `IssueRelationMutationResult` superset | Create 有 Relation；Delete 無 payload | `split` | Create-only hierarchy failures與Delete concurrency mapping |
| Issue Attachments | 3 | 共用 `IssueAttachmentOutcome`，Upload／Delete 共用 Mutation Result | Upload 有 Attachment；Delete 無 payload；Open 有 stream metadata | `split` | Upload／Delete／OpenContent各自 reachable failure與storage mapping |
| Test Management generic operations | 34 | `TestManagementResult<T>`＋五值 Outcome＋任意 `Code` | 31 個真 payload；3 個 `object` 假 no-payload | `split` by resource／semantic family | 50 個 observed error codes、HTTP mapping、cross-module mapping與no-payload語意 |
| Test Case Attachments | 3 | 共用 Outcome，Upload／Delete 共用 Mutation Result | Upload有Attachment；Delete無payload；Open有stream metadata | `split` | Upload／Delete／OpenContent matrix |
| Test Run Item Attachments | 3 | 共用 Outcome，Upload／Delete 共用 Mutation Result | Upload有Attachment；Delete無payload；Open有stream metadata | `split` | Run lifecycle failure與storage mapping |

Exact baseline 只 enforcement 已知 broad type 的 operation identity，不能自動判斷兩個 operation 是否真的屬於同一 semantic family。新增或保留例外仍需 Human 與獨立 AI Review；不得以重新命名 generic Result、每個 method 機械建立新型別，或把 error code 搬到另一個任意字串欄位規避此原則。

Project Member characterization checkpoint（2026-08-13，尚未 commit）：

- `ProjectMemberOutcomeMappingTests` 已分別鎖定 Add、Update Roles、Remove 三個 public operations。
- Add：Project／Account not found、invalid roles、already active、forbidden與201 payload。
- Update Roles：Member not found、invalid roles、last owner、forbidden、version conflict與200 payload。
- Remove：Member not found、last owner、forbidden、version conflict與204 no-content。
- Problem codes `project_member_already_active`、`project_last_owner_required`、`project_member_version_conflict` 已納入 observable behavior 保護。
- Targeted integration tests：3/3 passed；新增測試檔 format verification passed。
- 加入 P04 baseline／characterization 後完整 Domain unit tests：169/169 passed；完整 API integration tests：109/109 passed；`git diff --check`：passed。
- 本 checkpoint 只補 characterization，尚未拆分 `ProjectMemberMutationResult`／Outcome，不得宣稱 C17-2 或 P04 完成。
- 2026-08-17 checkpoint 前重新驗證：9 個 backend projects restore 成功；Release build 0 warnings／0 errors；unit 169/169、integration 109/109；frontend type-check、29/29 tests、production build皆通過。Vite build 保留既有 766.93 kB chunk size warning，沒有 build failure。
- Structural／public-boundary／P04 baseline implementation checkpoint：`82cf8c9`（`refactor: standardize backend boundaries and organization`）；尚未 push。

### I：Identity

| ID | Action Item | Owner | 狀態 |
| --- | --- | --- | --- |
| I01 | 補 Account characterization tests | AI | pending |
| I02 | 重構 Account 建立 API | AI | pending |
| I03 | 重構 `Account.Rename` | AI | pending |
| I04 | 決定 Account audit nullability 與共用 lifecycle | Human + AI | waiting-human |
| I05 | 重構 LoginSession 建立方式 | AI | pending |
| I06 | 重構 SystemRolePermission | AI | pending |
| I07 | 清除 Identity architecture allowlist | AI | pending |

### T：Test Management

| ID | Action Item | Owner | 狀態 |
| --- | --- | --- | --- |
| T01 | 建立 Case／Plan／Run 行為基線 | AI + Human behavior review | pending |
| T02 | 分類 25 個長參數項目 | AI | pending |
| T03 | 分類 17 個 audit 重複類別 | AI + Human exceptions | pending |
| T04 | 分批重構 TestCase | AI | pending |
| T05 | 分批重構 TestPlan | AI | pending |
| T06 | 分批重構 TestRun | AI | pending |
| T07 | 分批重構 Attachment／Link entities | AI | pending |
| T08 | 清除 Test Management architecture allowlist | AI | pending |

## C06 Request Declaration Inventory

本段盤點只涵蓋 request 與跨模組 application input；所有公開 response 的 body-form 與一型別一檔工作由 C18 統一追蹤。後續轉換必須保留既有 JSON property name、OpenAPI required／nullable 語意與 TypeScript shape。

### High priority

| 批次 | Request | 原因與前置條件 |
| --- | --- | --- |
| C09 | `CreateIssueRequest`、`UpdateIssueRequest` | 多個同型別與 optional string；已有主要 HTTP integration coverage |
| C10 | `CreateTestCaseRequest`、`UpdateTestCaseRequest` | 8／10 欄，包含多個 nullable 內容欄位與 collections |
| C11 | `CreateTestPlanRequest`、`UpdateTestPlanRequest` | `TestIssueId` 為近期演進欄位，create／update contract 必須同步 |
| C12 | `CreateTestRunBugRequest`、`CreateIssueCommand` | HTTP request 與跨模組 application contract 需一起維持語意 |
| C13 | `UpdateTestSuiteRequest` | 6 欄且容易錯置；目前缺直接 update happy-path integration coverage，必須先補測試 |

### Medium priority

- `UpdateTestWorkspaceRequest`
- `CreateTestSuiteRequest`
- `UpdateTestTagRequest`
- `UpdateProjectRequest`

三欄但仍包含 optional 或重複 primitive 的 request 也必須轉為明確 type body；為了讓 contract diff 可審查，排入 C16 分模組處理，不做全 repository 一次性機械改寫。

### Short request migration backlog

- Identity 的短小 request，例如 Login、Change Password 與 Account status。
- Project／Workspace member、狀態更新、單一 ID link、Case step、Run 建立及 Issue relation 等一至三欄 request。
- C16 第一批先處理 `AddProjectMemberRequest` 與 `UpdateProjectMemberRolesRequest`，驗證一致寫法不改變既有 `roleCodes` wire contract。

### 每批驗證方式

1. 先用固定 camelCase JSON 建立 serialization／deserialization characterization，不以 typed `JsonContent` 作為唯一證據。
2. 依 canonical OpenAPI 的 required 清單逐欄決定明確 constructor 或 `required init`；不得把 nullable 誤當 optional，或把 optional 誤改為 required。
3. Build 驗證所有 C# 建構呼叫點，HTTP integration test 驗證 raw JSON binding 與 observable behavior。
4. Wire shape 未改時，OpenAPI 與 TypeScript 不做無意義 churn，但仍執行人工對照與 frontend type-check。
5. 目前不為此盤點引進 OpenAPI parser 或 code generator；完整自動 schema gate 另列 enforcement 工作。

## Public Type 與 Result／Outcome Inventory

2026-08-12 盤點時共有 16 個檔案同時宣告多個 top-level public type，合計 84 個 public types；C12 拆分 `IIssueCommandService.cs` 後剩 15 個 aggregate files。另有 3 個單一 public type 的檔名與型別名不一致。Architecture baseline 已記錄現有債務，新增債務或已清除項目未移出 allowlist 都會失敗。

後續依下列順序處理，不做全 repository 一次性搬移：

1. 拆 Application public types 前，先盤點跨 project usage；不需要跨 assembly 的 accidental public API 優先縮為 `internal`，避免把債務永久化。
2. C11／C13／C16 轉換 Request 時，同批依 Cases／Plans／Runs／Workspaces 等功能拆分 `TestManagementContracts.cs`。
3. Identity／Project Contracts 依 resource 拆分；不建立 `Enums`／`Requests`／`Results` 等純技術資料夾。
4. 先將真正 public、操作專屬 Outcome／Result 做純拆檔，再另批修正語意；檔案搬移不順便改行為。
5. `IssueMutationResult`、`ProjectMemberMutationResult`、`IssueRelationMutationResult` 目前被 outcomes 不完全相同的操作共用，需依 use case 或真正共享語意的操作族群重新設計。
6. 三組 Attachment `MutationResult` 同時服務 Upload／Delete，且 Content result 使用四個 nullable／同型別欄位；應依 Upload／Delete／OpenContent 語意分開檢視。
7. `TestManagementResult<T>` 以模組級 Outcome 與 string Code 承載多個 use case，是較高風險的語意債務，最後配合 Service 拆分處理。
8. 所有公開 Response 都改為 body-form；長 Response 使用 required init-only properties 避免 positional constructor 錯置，短 Response 也採同一公開邊界宣告方式以消除 AI 產生兩套風格的空間。
9. `CreateIssueCommandResult` 已用 Success／Failure factory 排除成功無 payload 與失敗攜帶 payload；`CreateIssueCommandOutcome.NotFound` 仍代表建立成功後 directory read 失敗，create service 對既有 `VersionConflict` 的 defensive mapping 暫時保留，不能因此宣稱所有 operation outcome debt 已清除。

## Enforcement Matrix

| Rule ID | 規則 | Gate | 目前狀態 |
| --- | --- | --- | --- |
| KK-DOTNET-001 | Nullable reference types 必須啟用 | compiler | enforced |
| KK-DOTNET-002 | 新 public Domain／Application API 最多三個業務參數 | architecture test + AI review | Domain enforced；Application planned |
| KK-DOTNET-003 | Parameter Object 不得是無語意的一次性資料袋 | AI review + human review | documented |
| KK-DOTNET-004 | Public request／Application input 不使用 positional record | source architecture baseline + contract tests | enforced；baseline debt 已歸零 |
| KK-DOTNET-005 | 同一 mutation context 應完整傳遞 | unit test + AI review | planned |
| KK-DOTNET-006 | Helper 不以 null／mode flag 隱藏不同 use case | AI review | documented |
| KK-DOTNET-007 | 每個 top-level public type 一檔且檔名相同 | source architecture baseline + AI review | enforced；baseline debt 已歸零 |
| KK-DOTNET-008 | Result／Outcome 依操作表達，不以 superset 隱藏不可能狀態 | exact source baseline + characterization tests + AI／Human review | partially enforced；payload invariant 已保護，52 個 broad Result operations 由 exact no-growth baseline 追蹤，operation-family debt tracked by C17 |
| KK-DOTNET-009 | 公開 boundary contract 使用 body-form，不使用 positional record | source architecture baseline + contract tests | enforced；baseline debt 已歸零 |
| KK-DOTNET-010 | Source folder 依業務資源／use case 分類，不使用五個純技術目錄名稱 | source architecture test + AI review | enforced；完整 regression 與獨立 review 已通過，尚未 commit |
| KK-PERSIST-001 | Stable code 以命名常數集中 mapping | unit test + AI review | partially enforced |
| KK-MIGRATION-001 | 每個 branch／DbContext 一份未發布最終 migration | PR review + EF pending-model check | documented |
| KK-CONTRACT-001 | HTTP contract 先修改 OpenAPI並同步 C#／TypeScript | contract tests + review | partially enforced |
| KK-VERIFY-001 | 完成回報必須列出實際執行與未執行檢查 | AI completion report + PR template | documented |
| KK-FORMAT-001 | Repository 格式符合 `.editorconfig` | `dotnet format` + CI | CI planned |
| KK-SDK-001 | Build 使用 stable .NET 10 SDK | `global.json` + CI | enforced |

## 已完成重構基線

截至 2026-08-12：

| 模組 | 長參數 allowlist | 重複 audit allowlist | 狀態 |
| --- | ---: | ---: | --- |
| Project Management | 0 | 1 | 第一階段完成；`Project` audit nullability 待決策 |
| Identity | 4 | 3 | `AuditEvent` 已完成，其餘待處理 |
| Test Management | 25 | 17 | 尚未進入主要重構 |

相關本機 commits：

- `49ca4d0` test: establish backend refactoring safety baseline
- `b103117` refactor: establish backend clean code baseline
- `8e3fb57` refactor: simplify issue domain API
- `aadc6ac` refactor: simplify project domain API
- `3ef90b7` refactor: reuse project member audit lifecycle
- `5e6ee57` refactor: reuse member role audit lifecycle
- `21e5bab` refactor: simplify issue relation lifecycle
- `3c454e0` refactor: simplify issue attachment lifecycle
- `8ad7f9f` refactor: consolidate project catalog audit metadata
- `e564f2e` refactor: centralize project audit event creation
- `0f8f496` refactor: centralize identity audit event creation
- `df08a2c` docs: establish backend refactoring execution workflow
- `280dcf1` refactor: clarify audit and member validation flows
- `c77e52f` refactor: clarify project role collection semantics
- `afdd727` docs: standardize public request declarations
- `055700f` test: characterize project member request contracts
- `fb0003f` refactor: standardize project member requests
- `b004ddf` test: characterize test case request contracts
- `fb409af` fix: reject null test case steps
- `a71f500` refactor: standardize test case requests

## Verification Record

最近一次 production code 驗證（2026-08-11）：

- Domain unit tests：105/105 passed。
- API integration tests：19/19 passed。
- Project Management EF pending model：none。
- Identity EF pending model：none。

這是上一批次證據，不代表後續修改自動通過。每個 production code batch 都必須重新執行受影響的檢查。

本次文件工作流批次驗證（2026-08-12）：

- `git diff --check`：passed。
- Markdown 相對連結檢查：passed。
- 獨立 AI review：修正兩項狀態一致性問題後，沒有剩餘 blocker。
- Production code 與 contract 未修改，因此本批未重跑 .NET tests。

C01–C04 批次驗證（2026-08-12，commit `280dcf1`）：

- Changed-file `dotnet format --verify-no-changes`：passed。
- Backend solution build：passed，0 warnings／0 errors。
- 受影響 unit tests：6/6 passed。
- 新增 member validation integration tests：4/4 passed。
- 完整 Domain unit tests：105/105 passed。
- 完整 API integration tests：23/23 passed。
- Identity EF pending model：none。
- Project Management EF pending model：none。
- 獨立 AI review：沒有 blocker／high finding。
- 當時的 SDK 限制：本機使用 `10.0.400-preview.0.26322.102`，因此該批未將 E05 標成完成；此為歷史驗證環境，後續 stable SDK 狀態以較新的批次紀錄為準。

C05 批次驗證（2026-08-12，commit `c77e52f`）：

- Changed-file `dotnet format --verify-no-changes`：passed。
- Backend solution build：passed，0 warnings／0 errors。
- Project member targeted integration tests：8/8 passed。
- 完整 Domain unit tests：105/105 passed。
- 完整 API integration tests：25/25 passed。
- Frontend `pnpm type-check`：passed；TypeScript `roleCodes: string[]` 維持不變。
- OpenAPI／wire review：JSON 仍為 array，runtime 現在會拒絕 `uniqueItems: true` 所禁止的完全相同重複值。
- 獨立 AI review：沒有 blocker／high finding。
- 未執行 EF pending-model check：本批未修改 entity、mapping 或 migration。
- 第一次平行執行 build 與會觸發 build 的 test 曾因共用 `obj` 發生 `CS2012` file lock；改用 `--disable-build-servers -m:1` 單序列後完整通過。
- 目前 stable SDK `10.0.400` 已安裝；E05 已解除環境阻擋，但需獨立批次新增 `global.json` 才算完成。

E05 批次驗證（2026-08-12，commit `73377a1`）：

- Repository root `dotnet --version`：`10.0.400`，且 `dotnet --info` 讀取本批新增的 `global.json`。
- `global.json`：固定 `10.0.400`、`rollForward: disable`、`allowPrerelease: false`。
- Backend Release build：passed，0 warnings／0 errors。
- 完整 Domain unit tests：105/105 passed。
- 完整 API integration tests：25/25 passed。
- GitHub Actions YAML：兩份 workflow 均可解析，且只使用 `global-json-file: global.json`，不再另行宣告 `10.0.x`。
- API Docker build：passed；build stage 使用 `sdk:10.0.400`，驗證映像 `khaikang-api:e05-verify` 使用 non-root UID `1654`、OCI version `0.0.0-sdk-pin` 與 runtime `10.0.11`。
- API runtime base 暫時維持 `aspnet:10.0` 以取得同 major/minor 的 runtime security patch；因此本批保證 SDK 選版一致，不宣稱映像 digest 完全可重現。
- Full-repository `dotnet format --verify-no-changes`：未通過，原因是既有大量 CRLF 與 `.editorconfig` LF baseline 不一致；本批沒有修改 C#，不得藉 E05 大量重寫既有程式。E07 啟用前必須另批清理格式 baseline。
- 未執行 EF pending-model check：本批未修改 entity、mapping 或 migration。

C09 characterization baseline（2026-08-12，commit `7404f84`，named-init 重構前）：

- 新增 `IssueRequestContractTests`，以固定 camelCase JSON 鎖定 Create minimal payload、Update full payload與序列化欄位集合。
- Raw HTTP 鎖定 Create 可省略 optional 欄位並使用預設 `medium` priority。
- Raw HTTP 鎖定既有 validation key：Create 缺 `title` 回傳 `Title`，Update 缺 `version` 回傳 `version`；本批只保存行為，不順便統一 key casing。
- Targeted changed-file format：passed。
- Targeted characterization tests：5/5 passed。
- Canonical OpenAPI 為 source of truth；TypeScript 已存在 optional／nullable drift，C09 轉換時只做靜態型別放寬，不改 JSON wire shape。

C09 implementation 驗證（2026-08-12，commit `25688ea`）：

- `CreateIssueRequest`：`title`／`typeCode` 使用 2 欄 constructor；5 個 optional nullable 欄位使用 init-only property。
- `UpdateIssueRequest`：OpenAPI required 的 `title`／`typeCode`／`priorityCode`／`version` 使用具名 constructor；4 個 optional nullable 欄位使用 init-only property。
- 全部 C# typed call sites 已改用 named arguments 與 initializer；未新增 Application command、factory 或純轉接 abstraction。
- TypeScript optional nullable 欄位已對齊 canonical OpenAPI；JSON wire shape 不變，frontend type-check passed。
- Raw HTTP required-field matrix：Create 2 欄、Update 4 欄全部保留既有 validation key；Update 省略 optional 欄位會以 PUT replacement semantics 清除為 `null`。
- Targeted integration tests：11/11 passed；完整 Domain unit tests：105/105 passed；完整 API integration tests：36/36 passed。
- Backend Release build：passed，0 warnings／0 errors。
- 兩個全新 contract 與本批 contract test changed-file format：passed；既有受影響檔案因 Windows CRLF checkout 與 `.editorconfig` LF baseline 不一致而無法通過整檔 format，E07 必須先建立 line-ending policy，不在 C09 大量改寫換行。
- `git diff --check`：passed；三方獨立 AI review 最終均為 0 blocker／0 high／0 medium。
- 未執行 EF pending-model check：本批未修改 entity、mapping 或 migration。
- 既有 contract debt 已另列 C14／C15：runtime 尚未落實 `additionalProperties: false`，validation key casing 亦未統一；不得在結構重構中無聲改變。

C10 characterization 與 null-step 邊界修正（2026-08-12，commits `b004ddf`、`fb409af`）：

- 新增 `TestCaseRequestContractTests`，雙向鎖定 Create／Update 的 camelCase JSON 欄位集合、step 欄位、穩定 validation path 與 `tagIds` 省略語意。
- Raw HTTP 實測確認既有 contract drift：Create 省略 `description`／`preconditions`／`overallExpectedResult`／`sortOrder` 仍回 `201`，省略 `suiteId` 回 `404`；Update 省略前述三個文字欄位或 `sortOrder` 仍回 `200`。這些一次性診斷案例未保留成 regression test，避免把違反 canonical OpenAPI 的現況固定成目標行為。
- 修正前 Create／Update 收到 `steps: [null]` 均回 `500`；`fb409af` 讓共用 HTTP boundary validator 明確接受不受信任的 nullable element，並穩定回傳 `400 testCase`。
- Targeted Test Case contract tests：14/14 passed。
- Backend Release build：passed，0 warnings／0 errors。
- 完整 Domain unit tests：105/105 passed；完整 API integration tests：50/50 passed。
- `git diff --check`：passed；未執行 EF pending-model check，因本批未修改 entity、mapping 或 migration。
- 此 characterization checkpoint 當時尚未修改 C10 request type；後續決策與完成狀態以 `a71f500` 的 implementation record 為準。
- C14／C15 仍為 `pending`：本批只保存部分現況，不代表已落實 unknown-property policy 或統一 validation error key casing。

C16 第一批 Project Member request 宣告統一（2026-08-12，commits `055700f`、`fb0003f`）：

- `AddProjectMemberRequest` 與 `UpdateProjectMemberRolesRequest` 已由 positional record 改為與 `CreateIssueRequest` 一致的 body-form `sealed record`，短必填欄位使用明確 constructor 與 getter-only property。
- 所有 typed call sites 已改用 named arguments；未加入 `required` metadata，既有 JSON binding、validation path、OpenAPI 與 TypeScript wire contract 均未改變。
- Targeted Project Member contract／endpoint tests：13/13 passed；獨立 AI review：0 blocker／0 high／0 medium。
- C16 是跨模組的 umbrella item；目前仍有 27 個 positional public requests／inputs 待分批處理，因此狀態維持 `in-progress`。

C10 implementation 驗證（2026-08-12，commit `a71f500`）：

- Human 已確定所有 public Request／public cross-module input 使用 body-form type declaration；這項規則不擴張到 response、domain value object、application result 或 private／internal record。
- `CreateTestCaseRequest`、`UpdateTestCaseRequest` 與 `CreateTestCaseStepRequest` 已各自獨立成單一 public type 檔案，使用最多三個語意核心 constructor 參數；其餘 required scalar 使用 `required init`，optional nullable 欄位使用一般 init property。
- Canonical contract 決策：三個文字欄位為 optional nullable；`tagIds` 為 optional nullable unique array。Create omitted／`null` 建立空集合；Update omitted／`null` 保留、`[]` 清除；duplicate 明確回 `400 tagIds`。
- `suiteId` 缺欄或空值會在 HTTP boundary 回 `400 testCase`；缺少 required-init 欄位會回帶 `status: 400` 的 ProblemDetails；step 缺少 `action`／`expectedResult`、step element 為 `null`、Update `status: null` 均有 regression coverage。
- Production change 前的 red phase 精確出現 6 個預期失敗；實作後 targeted Test Case contract tests：32/32 passed。
- Backend Release build：passed，0 warnings／0 errors；完整 Domain unit tests：105/105 passed；完整 API integration tests：70/70 passed。
- Frontend type-check：passed；web tests：29/29 passed；production build：passed（只有既有 large-chunk warning）。
- OpenAPI YAML parse、new-file changed-format 與 `git diff --check`：passed；獨立 AI review 最終為 0 blocker／0 high／0 medium。
- 未執行 EF pending-model check：本批未修改 entity、mapping 或 migration。

C12 Run Bug／Issue command bridge 驗證（2026-08-12，尚未 commit）：

- Red phase：architecture baseline 精確抓到 `IIssueCommandService.cs` 四個 public types 同檔、`IssueCommandRequest` 與 `CreateTestRunBugRequest` positional 宣告；raw HTTP 亦證明省略 `priorityCode`／`description`／`assigneeAccountId` 時 runtime 原本錯誤接受並建立 Bug。
- `CreateTestRunBugRequest` 已改為 body-form，五個 OpenAPI required 欄位全部使用 `required init`；省略任一欄位回 `400`，required-but-nullable 欄位明確傳 `null` 仍可成功建立。
- 跨模組 input 已改為 `CreateIssueCommand`，使用 Title／TypeCode constructor 與 optional init properties；interface、command、outcome、result 已放在 `Contracts/IssueCommands/` 並各自一檔，namespace 維持不變。
- `CreateIssueCommandResult` 使用 private constructor 與 Success／Failure factories，排除成功無 Issue、失敗攜帶 Issue 與 `Failure(Succeeded)`；Result invariant tests：4/4 passed。
- 成功 integration flow 已驗證 Title／Description／Priority 完整傳入 Project Issue；invalid priority、invalid assignee、inactive Project 與既有 traceability mapping 均有 regression coverage。
- Source architecture baseline 以 path + type identity 記錄現有 aggregate debt，並檢查檔名及所有公開 boundary contract 的 positional 宣告，涵蓋 Request／Response／Query／Command／Directory Entry／Result 與 record class／struct／partial／readonly／generic variants；EF migration 只有 timestamp filename scoped exception，仍受一檔一 public type 檢查。
- Backend Release build：passed，0 warnings／0 errors；完整 Domain unit tests：118/118 passed；完整 API integration tests：79/79 passed。
- Frontend type-check：passed；web tests：29/29 passed；production build：passed（只有既有 large-chunk warning）。
- New-file changed-format 與 `git diff --check`：passed；三個獨立 AI review 最終均為 0 blocker／0 high／0 medium。
- OpenAPI／TypeScript wire shape 未修改；本批將 runtime required behavior 對齊既有 canonical OpenAPI。未執行 EF pending-model check，因未修改 entity、mapping 或 migration。
- 本批尚未取得 commit／push 授權；C12 維持 `in-progress`，commit 後再改為 `completed` 並補入實際 hash。

C18 公開邊界 contract 全盤標準化驗證（2026-08-12，尚未 commit）：

- Identity、Project Management、Test Management 與 API host 的公開 Request／Response／Query／Command／Directory Entry 已統一為 explicit body-form；required／nullable 與 JSON property shape 由 integration contract tests 保護。
- 原本的 aggregate contract／result 檔案已按資源與操作拆成一個 top-level public type 一檔，檔名與型別名一致；source architecture tests 的 multiple-public-type、filename mismatch、public positional boundary 三個 allowlist 均已歸零。
- Identity／Project Management Result 與三組 attachment Result 已用 intention-revealing factories 排除成功缺 payload、失敗攜帶 payload及 `Failure(Succeeded)`；`TestManagementResult<T>` 已是 explicit body-form，validated internal constructor 與 Success／Failure factories會拒絕不合法狀態。
- `TestManagementResult<T>` 仍服務多個 operation family，但直接 construction 已封閉，production callsites 全部使用 `Success／Failure` factory；operation-specific outcome／error code 的長期拆分仍由 C17 逐個業務族群處理。
- 共用 `PagedResult<T>` 與 Project 短 Response 也已納入 body-form；architecture gate 會掃描 `backend/src` 全部非 Domain public positional record，避免依 Contracts／Application 路徑漏檢真正 HTTP boundary。
- Release solution build：passed，0 warnings／0 errors；Domain unit tests：155/155 passed；API integration tests：106/106 passed；architecture baseline 包含在 unit suite 並已通過。
- Frontend type-check：passed；web tests：29/29 passed；production build：passed，僅保留既有 large-chunk warning。
- 本批變更 C# 檔 `dotnet format --verify-no-changes`：passed；`git diff --check`：passed。未執行 EF pending-model check，因未修改 entity、mapping 或 migration。
- Canonical OpenAPI 與 TypeScript 未因 C# 宣告形式而變更；Create Project／Workspace description optionality、Workspace update prefix、Account lastLoginAt、C14 unknown-property 與 C15 error-key drift 均明確留在後續契約決策，未靜默混入本批。
- 本批尚未取得 commit／push 授權；C11／C12／C13／C16／C17／C18 依 tracker workflow 維持 `in-progress`，建立 checkpoint 後再填入實際 commit hash。
- 最終獨立 review 已複查共用 `PagedResult<T>`、短 Response、architecture gate 與 `TestManagementResult<T>` factory；前輪 findings 均已關閉，結果為 0 blocker／0 high／0 medium。

C19 業務資料夾分類與防回歸 gate（2026-08-12，尚未 commit）：

- Identity 26 個、Project Management 49 個、Test Management 14 個檔案，共 89 個檔案只搬移實體路徑；既有 namespace、public API、wire contract 與 production behavior 均未修改。
- Contracts／Application 改依 Accounts、Authentication、Issues、IssueCommands、TestCases、TestRuns 等業務資源或 use case 分組；同一 use case 的 Command／Outcome／Result／Interface 放在一起。跨資源使用的 coordinator／constants 可保留在 layer root，不為了清空 root 製造假的 owner。
- `PublicTypeLayoutBaselineTests` 新增 source folder gate，以不分大小寫的完整目錄名稱阻擋 `Enums`、`Interfaces`、`Requests`、`Responses`、`Results`；`IssueCommands` 與 `TestRuns` 的 self-test 明確證明業務目錄不會被誤擋。
- Backend restore：passed，9 個 projects 均完成；Release solution build：passed，0 warnings／0 errors。
- Domain unit tests：163/163 passed；API integration tests：106/106 passed；targeted architecture tests：26/26 passed。
- Frontend type-check：passed；web tests：29/29 passed；production build：passed，只保留既有大於 500 kB 的 bundle chunk warning。
- Changed C# format：159/159 files passed；`git diff --check`：passed。
- 獨立 AI review：0 blocker／0 high／0 medium；C19 implementation／testing 與 E10 gate 均已完成。
- 本批未取得 commit／push 授權，工作樹保留未提交狀態。

## 每批更新方式

每一批必須隸屬一個 Active Principle Workstream。Module／feature 只作為實作 checkpoint，不是完成原則的依據。

開始前：

1. 確認 branch 與 `git status`。
2. 確認 active principle 的全 repo inventory 與 exact debt baseline。
3. 將一個 Action Item 或 principle checkpoint 標成 `in-progress`。
4. 記錄本批 observable behavior、行為邊界、風險與測試策略。
5. 行為或相容性有風險時先建立 characterization，不直接修改 production code。

完成後：

1. 記錄實際執行的 format、build、test 與 EF check。
2. 確認 active principle 的 exact debt baseline 淨減且沒有新增例外。
3. 建立單一原則、單一垂直切片的本機 commit；未經授權不 commit／push。
4. 在本文件記錄 commit、剩餘全 repo 債務、未驗證項目與下一個 Resume Point。
5. 只有全 repo DoD 達成，或剩餘例外有 Human 核准與 durable rationale，才把 principle 標成 `completed`。

## Resume Point

目前預期狀態：

- Branch：`ecohover/refactor/backend-clean-code`
- Remote base：`origin/rc`
- Last completed production fix：Test Case null step boundary validation，commit `fb409af`。
- Last completed docs rule：Public Request body-form declaration，commit `afdd727`。
- Last completed enforcement batch：E05 stable SDK 鎖版，commit `73377a1`。
- Last completed request-contract batch：C10 Test Case request，characterization `b004ddf`、null-step fix `fb409af`、implementation `a71f500`。
- C16 first batch：Project Member characterization `055700f`、implementation `fb0003f`；umbrella item 維持 `in-progress`。
- Last structural／boundary checkpoint：`82cf8c9`；C11／C12／C13／C16／C18／C19 與 E10 已完成提交，尚未 push。
- Active principle model：P09 實體組織已完成；P03 公開邊界宣告標準化已完成，但因 Workspace canonical drift 與 C14／C15 尚在，原則維持 `in-progress`；P04 Result／Outcome 語意為下一個全域 active refactoring principle。單一 Project Member、Issue、Attachment 或 Test Management slice 只會是 P04 checkpoint，不得單獨宣告 P04 完成。
- P04 inventory checkpoint：已盤點 60 個 Application Result／Outcome operations，其中 8 個初步 `keep-family`、52 個 broad Result semantic debt；`ApplicationResultSemanticBaselineTests` 已建立 exact no-growth baseline並以 targeted tests 6/6通過。C17-1 尚缺 reachable outcome／HTTP mapping characterization matrix，不得標為完成。
- P04 first characterization slice：Project Member Add／Update Roles／Remove 的 reachable outcome、HTTP status、problem code與成功 payload／no-content已由 targeted integration tests 3/3保護；production Result／Outcome尚未修改。
- Current debt target：aggregate public-type file、檔名 mismatch、Contracts／Application public positional record 與五個純技術 source folder 名稱目前均為零；Domain／真正 internal carrier 不納入公開邊界 gate，業務 use-case 目錄不視為技術分類。
- Checkpoint evidence：提交前 cached scope為204 files（25 rename、115 add、35 delete、29 modify），6,285 insertions／1,829 deletions；0 unstaged、0 untracked、0 binary、0禁止路徑，`git diff --cached --check`通過。
- Expected working tree：本 tracker checkpoint 提交後應為 clean。不得把 Workspace prefix、Account lastLoginAt、C14 unknown-property 或 C15 error-key drift 靜默混入後續 P04 批次。
- Next step：以 local runtime smoke test驗證 `82cf8c9`，接著補完 C17-1 reachable outcome／HTTP mapping characterization matrix，再選第一個 P04 垂直切片；未獲授權不得 push。
- Human decisions：R06 與 I04 尚未決定；公開 boundary contract 宣告風格已由 Human 改為單一 non-positional type body，不得再保留小型 response／public Result positional 例外。TestManagement operation-family Result 與 error Code 的長期拆分粒度另列後續語意重構，不阻擋本次 factory invariant 收斂。

若此處與 Git／測試現況不一致，應先以 Git、正式規範與可重現測試為準，再更新本文件。
