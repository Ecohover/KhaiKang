# 產品願景與 MVP 工作流程

## 1. 產品定位

KhaiKang 是一套為個人開發者及中小型軟體團隊設計的開源、可自架式開發與測試管理平台。

平台負責管理需求、工作項目、測試案例、品質條件、環境晉級及交付紀錄；Jenkins、GitHub Actions、Playwright、pytest、NUnit 等外部工具繼續負責 Build、測試與部署的實際執行。

KhaiKang 不以複製完整 Jira 或取代 CI/CD 工具為目標，而是作為工程交付工具鏈上層的管理與協調中樞。

## 2. 工作方法

KhaiKang 採用以下原則：

- Kanban-first、Scrum-compatible。
- AI 可以介入每個節點，但不必擁有最終決策權。
- 每個節點都能選擇不檢核、自動檢核或人工檢核。
- 功能程式與測試程式依同一份計畫並行開發。
- 測試結果必須能追溯至需求、驗收條件、Artifact 與環境。
- Build once, deploy many：通過驗證的同一份 Artifact 逐步晉級。
- Template first, configurable later。
- Linear now, graph-ready later。

## 3. 目標交付流程

1. 紀錄原始需求。
2. AI 分析需求、既有架構、Git Repository 與相關測試。
3. AI 提出技術方案及程式框架變更建議。
4. 產生開發計畫、驗收條件、開發工作與測試案例。
5. 依專案規則選擇是否人工核准。
6. 功能程式與測試程式並行開發。
7. Build 並產生不可變 Artifact。
8. 執行適用的單元、元件及靜態檢查。
9. 將 Artifact 部署至第一個環境。
10. 執行該環境指定的整合、API、E2E、回歸、UAT 或人工測試。
11. 依品質條件及人工核准判斷是否晉級。
12. 產生需求層級的品質報告。
13. 通過正式部署 Gate 後晉級 Production。
14. 執行 Production Health Check、Safe Smoke Test 及監控確認。

AI 資訊不足時應將工作標記為 Needs Clarification，不應自行假設並繼續高風險變更。

## 4. 選擇性檢核

每個節點共用一致的檢核概念：

- None：不需要檢核。
- Automatic：符合條件後自動通過。
- Manual：等待指定人員核准。
- Automatic with manual fallback：自動條件不足時轉人工。
- AI recommendation with human decision：AI 提供判斷，人員決定。

人工檢核不是固定在單一 Review 階段，而是可出現在需求、AI 分析、技術方案、開發計畫、程式碼、測試案例、環境晉級與正式部署前。

## 5. 功能與測試並行開發

開發計畫核准後分為兩條工作流：

- 功能開發：API、UI、商業邏輯、資料庫與設定。
- 測試開發：單元測試、整合測試、API 測試、E2E、自動化 Fixture 及人工驗收案例。

兩條工作流在 Build 與環境驗證前匯合。每一項驗收條件應能連結至一個或多個測試案例。

核心追溯關係：

```text
Requirement
  -> Acceptance Criterion
  -> Development Task
  -> Test Case
  -> Automation Reference
  -> Test Execution
  -> Quality Report
```

## 6. 環境 Stage

環境不應寫死為固定名稱或固定數量。每個 Stage 應包含：

- Entry Conditions
- Deployment Actions
- Test Activities
- Verification Actions
- Exit Conditions
- Approval Policy
- Failure Policy

官方可提供以下傳統流程模板：

```text
DEV -> SIT -> UAT -> PRE-PROD -> PROD
```

常見預設用途：

| Stage | 主要目的 | 常見測試與活動 |
| --- | --- | --- |
| DEV | 功能整合與快速回饋 | Build、Lint、Unit、Component、Smoke |
| SIT | 系統與服務整合驗證 | Integration、API、Regression |
| UAT | 業務需求驗收 | Acceptance、Manual、必要 E2E |
| PRE-PROD | 正式部署演練 | Full Regression、Performance、Security、Migration、Rollback |
| PROD | 正式運行確認 | Health Check、Safe Smoke、Monitoring |

測試類型與環境為多對多關係，表格僅為模板預設，不應成為不可修改的系統限制。

## 7. 條件與 Gate

MVP 的 Exit Conditions 優先支援：

- Build 是否成功。
- 必要測試是否全部通過。
- 測試最低通過率。
- Critical Defect 最大允許數。
- Environment Health Check。
- 是否需要人工核准。

條件結果至少應區分：

- Passed
- Passed with warning
- Approval required
- Retry
- Failed
- Blocked
- Skipped
- Rollback required

未來再擴充 AND/OR 條件群組、自訂 Expression、Webhook 回傳值、AI 風險分數、多人核准、時間條件及分支流程。

## 8. MVP 邊界

MVP 必須完成：

- Project
- Requirement、Issue 與 Bug
- Kanban Board
- Test Case、Test Run 與 Test Result
- AI 需求分析及測試案例草稿
- 線性 Environment Stage
- Environment Template
- 基本 Exit Conditions
- 選擇性人工核准
- Generic Webhook
- Jenkins 基本整合
- Requirement Coverage 與品質報告

MVP 暫不實作：

- 完整視覺化 Workflow Designer
- 任意平行及條件分支
- 自訂條件語言
- 複雜多人簽核矩陣
- 完整自動回滾編排
- 多 Agent 自主協作
- 取代 Jenkins 或測試框架的執行引擎

## 9. MVP 環境模板

建立專案時提供：

- 個人／小型專案：DEV -> PROD
- 一般團隊：DEV -> SIT -> UAT -> PROD
- 完整交付：DEV -> SIT -> UAT -> PRE-PROD -> PROD

第一版允許修改名稱、啟用狀態、順序、基本條件、測試類型及人工核准設定。後續版本再提供完整拓撲修改能力。

## 10. 資料模型方向

不得以 dev_status、sit_status、uat_status 等固定欄位表示環境。

建議核心概念包含：

- Project
- Requirement
- WorkItem
- AcceptanceCriterion
- TestCase
- TestSuite
- TestExecution
- BuildArtifact
- Environment
- EnvironmentStage
- StageCondition
- StageExecution
- Approval
- Deployment
- QualityReport

MVP 可以只支援依 order 線性執行，但資料模型應允許未來加入相依節點及多分支拓撲。
