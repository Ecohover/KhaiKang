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
| D05 | Positional record 限於短小、穩定且不易混淆的 transport contract；欄位多、optional、多個同型別或持續演進時使用具名 `init` properties | accepted |
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
| R04 | 定義 positional record 使用條件 | Human + AI | completed | 正式規範包含短小 contract 例外與切換條件 |
| R05 | 定義 Helper／Factory／Command 建立門檻 | Human + AI | completed | 正式規範包含真實語意與重用門檻 |
| R06 | 決定 `ProjectDetailsChange.Status` 等欄位是否應拆成獨立狀態轉移 | Human | waiting-human | 業務語意確認並記錄決策 |

### E：規範與 Enforcement

| ID | Action Item | Owner | 狀態 | 驗收條件 |
| --- | --- | --- | --- | --- |
| E01 | 建立規則與 enforcement 對照 | AI | completed | 本文件的 Enforcement Matrix 可指出自動與人工 Gate |
| E02 | 更新公開 .NET 開發規範 | AI + Human review | completed | 英文與繁中同步且不綁特定 AI |
| E03 | 更新既有後端重構計畫與目前債務 | AI | completed | 文件反映目前實際進度 |
| E04 | 建立薄型 AI backend-refactoring 路由 | AI | completed | `.ai/` 只連結正式文件與本追蹤文件 |
| E05 | 鎖定 stable .NET 10 SDK | AI + Human | blocked | `global.json` 與 CI 使用 stable SDK；目前本機只有 preview SDK |
| E06 | 集中 compiler／analyzer policy | AI + Human | pending | 先產生 warning baseline，再分階段提高強度 |
| E07 | Backend CI 加入 format 與必要 gate | AI | pending | PR 上執行 format、build、unit、integration |
| E08 | Architecture fitness test 納入 Application API | AI | pending | 排除 `CancellationToken`、DI 與 framework signature |
| E09 | 維護規範債務基線 | AI | in-progress | 現有 allowlist 不得增加，完成項目立即移除 |

### C：修正過度精簡與假抽象

| ID | Action Item | Owner | 狀態 | 驗收條件 |
| --- | --- | --- | --- | --- |
| C01 | 移除 Identity 一次性 `AuditContext` | AI | completed | Named factories 保留，私有流程不再為參數數量包裝 |
| C02 | 移除 `ProjectAuditEvent.Create` 純轉接 helper | AI | completed | Named factory 直接建立 entity，行為不變 |
| C03 | `ChangeContext` 建立後完整傳遞 | AI | completed | 不再拆 actor/time 後於 helper 內重建 |
| C04 | 將 member Add／Update validator 分開 | AI | completed | 不以 null／optional 值切換驗證模式，補 null regression test |
| C05 | 修正 `RoleCodes` 集合語意 | AI + Human contract review | pending | 評估 `IReadOnlyCollection<string>` 並同步 OpenAPI／clients |
| C06 | 盤點大型 positional requests | AI | pending | 先處理欄位多、同型別或 optional 的 contract |
| C07 | 盤點 Application 直接使用 HTTP DTO | AI + Human | pending | 只在多入口或規則複雜時增加 application input model |
| C08 | 盤點純轉接 helpers | AI Reviewer | pending | 每項有明確移除理由，不進行機械式刪除 |

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

## Enforcement Matrix

| Rule ID | 規則 | Gate | 目前狀態 |
| --- | --- | --- | --- |
| KK-DOTNET-001 | Nullable reference types 必須啟用 | compiler | enforced |
| KK-DOTNET-002 | 新 public Domain／Application API 最多三個業務參數 | architecture test + AI review | Domain enforced；Application planned |
| KK-DOTNET-003 | Parameter Object 不得是無語意的一次性資料袋 | AI review + human review | documented |
| KK-DOTNET-004 | 大型或易混淆 contract 不使用 positional record | AI review + contract tests | planned |
| KK-DOTNET-005 | 同一 mutation context 應完整傳遞 | unit test + AI review | planned |
| KK-DOTNET-006 | Helper 不以 null／mode flag 隱藏不同 use case | AI review | documented |
| KK-PERSIST-001 | Stable code 以命名常數集中 mapping | unit test + AI review | partially enforced |
| KK-MIGRATION-001 | 每個 branch／DbContext 一份未發布最終 migration | PR review + EF pending-model check | documented |
| KK-CONTRACT-001 | HTTP contract 先修改 OpenAPI並同步 C#／TypeScript | contract tests + review | partially enforced |
| KK-VERIFY-001 | 完成回報必須列出實際執行與未執行檢查 | AI completion report + PR template | documented |
| KK-FORMAT-001 | Repository 格式符合 `.editorconfig` | `dotnet format` + CI | CI planned |
| KK-SDK-001 | Build 使用 stable .NET 10 SDK | `global.json` + CI | blocked |

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
- SDK 限制：本機仍使用 `10.0.400-preview.0.26322.102`；E05 保持 blocked，不把 preview 當成完成狀態。

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
- Last completed batch：C01–C04 低風險可讀性修正與 member validation null regression tests，commit `280dcf1`。
- Expected working tree：完成 tracker checkpoint commit 後應為乾淨；若不乾淨，先確認修改所有權與目前 batch。
- Next batch：先處理 C05 contract collection 語意與 C06 大型 positional request 盤點；不得只為統一外觀修改短小且清楚的 request。
- Human decisions：R06 與 I04 尚未決定；不得由 AI 為了結構一致性自行改變業務狀態或 nullability。

若此處與 Git／測試現況不一致，應先以 Git、正式規範與可重現測試為準，再更新本文件。
