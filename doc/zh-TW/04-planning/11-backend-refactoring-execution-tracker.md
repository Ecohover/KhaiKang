# 後端可讀性重構執行追蹤

> 狀態：Active
>
> 最後更新：2026-08-12
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
| C09 | Project Management Issue request 改為具名 init contract | AI + Human contract review | completed | 必填欄位使用短 constructor、optional 欄位使用具名 init；JSON、HTTP 與 OpenAPI shape 均維持 |
| C10 | Test Case request 改為具名 init contract | AI + Human contract review | waiting-human | Characterization 與 null-step 邊界修正已完成；request shape 等待 required／nullable 與 tag 語意決策 |
| C11 | Test Plan request 改為具名 init contract | AI + Human contract review | pending | 保留 CaseIds 與 TestIssueId wire contract |
| C12 | Run Bug 與 Issue application bridge request 調整 | AI + Human contract review | pending | HTTP 與跨模組 application contract 同批驗證 |
| C13 | Suite／Workspace／Tag 與中型 request 後續整理 | AI | pending | 先補缺少的 update endpoint characterization tests |
| C14 | 落實 OpenAPI `additionalProperties: false` | AI + Human contract review | pending | 先盤點未知欄位的相容風險，再讓 runtime 與全域 canonical contract 一致 |
| C15 | 統一 validation error key casing | AI + Human contract review | pending | 明確選擇 JSON camelCase 或 CLR property casing，建立相容策略與 regression matrix |
| C16 | 清理其餘 public positional request／input contracts | AI | in-progress | 分模組、小批次轉換；每批維持 JSON／OpenAPI／TypeScript wire contract |

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

盤點只涵蓋 request 與跨模組 application input；大型 response 不因欄位多就改成 mutable class。後續轉換必須保留既有 JSON property name、OpenAPI required／nullable 語意與 TypeScript shape。

### High priority

| 批次 | Request | 原因與前置條件 |
| --- | --- | --- |
| C09 | `CreateIssueRequest`、`UpdateIssueRequest` | 多個同型別與 optional string；已有主要 HTTP integration coverage |
| C10 | `CreateTestCaseRequest`、`UpdateTestCaseRequest` | 8／10 欄，包含多個 nullable 內容欄位與 collections |
| C11 | `CreateTestPlanRequest`、`UpdateTestPlanRequest` | `TestIssueId` 為近期演進欄位，create／update contract 必須同步 |
| C12 | `CreateTestRunBugRequest`、`IssueCommandRequest` | HTTP request 與跨模組 application contract 需一起維持語意 |
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
2. 依 canonical OpenAPI 的 required 清單逐欄決定 `required init`；不得把 nullable 誤當 optional，或把 optional 誤改為 required。
3. Build 驗證所有 C# 建構呼叫點，HTTP integration test 驗證 raw JSON binding 與 observable behavior。
4. Wire shape 未改時，OpenAPI 與 TypeScript 不做無意義 churn，但仍執行人工對照與 frontend type-check。
5. 目前不為此盤點引進 OpenAPI parser 或 code generator；完整自動 schema gate 另列 enforcement 工作。

## Enforcement Matrix

| Rule ID | 規則 | Gate | 目前狀態 |
| --- | --- | --- | --- |
| KK-DOTNET-001 | Nullable reference types 必須啟用 | compiler | enforced |
| KK-DOTNET-002 | 新 public Domain／Application API 最多三個業務參數 | architecture test + AI review | Domain enforced；Application planned |
| KK-DOTNET-003 | Parameter Object 不得是無語意的一次性資料袋 | AI review + human review | documented |
| KK-DOTNET-004 | Public request／Application input 不使用 positional record | AI review + contract tests | documented；existing debt tracked by C09–C13／C16 |
| KK-DOTNET-005 | 同一 mutation context 應完整傳遞 | unit test + AI review | planned |
| KK-DOTNET-006 | Helper 不以 null／mode flag 隱藏不同 use case | AI review | documented |
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
- `b004ddf` test: characterize test case request contracts
- `fb409af` fix: reject null test case steps

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
- C10 request type 尚未修改，狀態維持 `waiting-human`。完成前必須決定：(1) required + nullable 欄位缺欄時拒絕或放寬 OpenAPI；(2) `suiteId`／`sortOrder` 缺欄的 boundary 行為；(3) `tagIds` omitted／`null`／`[]`／duplicate 的 canonical 語意。
- C14／C15 仍為 `pending`：本批只保存部分現況，不代表已落實 unknown-property policy 或統一 validation error key casing。

## 每批更新方式

開始前：

1. 確認 branch 與 `git status`。
2. 將一個 Action Item 標成 `in-progress`。
3. 記錄本批行為邊界、風險與測試策略。

完成後：

1. 記錄實際執行的 format、build、test 與 EF check。
2. 建立單一目的的本機 commit；未經授權不 push。
3. 在本文件記錄 commit、完成項目、未驗證項目與下一個 Resume Point。

## Resume Point

目前預期狀態：

- Branch：`ecohover/refactor/backend-clean-code`
- Remote base：`origin/rc`
- Last completed production fix：Test Case null step boundary validation，commit `fb409af`。
- Last completed docs checkpoint：C06 request inventory，commit `2d27f4a`。
- Last completed enforcement batch：E05 stable SDK 鎖版，commit `73377a1`。
- Last completed request-contract batch：C09 Issue request，characterization `7404f84`、implementation `25688ea`。
- Latest C10 checkpoint：characterization `b004ddf`、null-step fix `fb409af`；request type 尚未重構。
- Current batch：C16 第一批，先將 Project Member requests 統一為 `CreateIssueRequest` 類型本體風格；C10 contract implementation 緊接其後。
- Expected working tree：本規則調整尚待文件 commit，之後只應包含當前單一 contract 批次。
- Next step：先完成 Project Member request 宣告統一與 wire regression；再依已提出的 Test Case required／nullable 與 `tagIds` 語意完成 C10，不混入 C14／C15 或 Test Case domain 重構。
- Human decisions：R06 與 I04 尚未決定；公開 request 宣告風格已由 Human 改為單一 non-positional type body，不得再保留短 request positional 例外。

若此處與 Git／測試現況不一致，應先以 Git、正式規範與可重現測試為準，再更新本文件。
