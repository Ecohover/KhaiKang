# AI Agent Orchestration 設計方向

## 文件狀態

- 狀態：Direction
- 階段：MVP 後續能力
- 性質：產品與架構方向，不是目前的功能規格或正式資料模型

## 目的

本文件記錄 KhaiKang 未來 AI Agent 能力的設計方向，供後續規格、
資料模型與實作工作使用。

KhaiKang 不只追蹤 AI 或自動化工具的結果。未來應能在專案政策、
安全限制與人工檢核之下，選擇並協調適合的 AI Agent、角色、Prompt、
工具及外部工程系統，持續推進工作，直到產生可驗證的成果、需要人工
決策，或達到明確的停止條件。

這項方向不代表目前任務管理 MVP 要立即實作多 Agent 自主協作。
近期仍依階段規劃完成 Project、Issue、Test Management、品質證據及
外部整合基礎。

## 核心概念

### AI Agent

AI Agent 表示實際執行者及其執行能力，例如：

- Provider 與 Model
- 執行位置
- 可使用的工具
- Context 與 Token 限制
- 成本、時間及重試限制
- 啟用狀態與憑證參照

AI Agent 不應永久等同單一工程角色。

### AI Role

AI Role 表示一次工作中的責任，例如：

- Requirement Analyst
- Planner
- Builder
- Reviewer
- Tester
- Diagnostician
- Release Coordinator
- 專案自訂角色

同一個 Agent 可以依任務扮演不同角色；同一個角色也可以由不同 Agent
執行。

### Prompt Template

Prompt Template 是可版本化、可選擇的工作指令。至少應能描述：

- 適用的角色、任務類型及技術範圍
- 指令內容與版本
- 所需工具與 Context
- 預期輸出格式
- 驗證與完成條件
- 風險等級及適用的 Approval Policy

Prompt 不應與 Agent、角色或單一 Workflow 寫死在一起。

### AI Execution

每次 AI Execution 應保存當次實際使用的組合，而不是只參照可變動的
目前設定：

```text
Task
+ Agent
+ Role
+ Prompt Version
+ Context Snapshot
+ Tool Policy
+ Approval Policy
= AI Execution
```

實際欄位與資料表應等 AI Agent 功能進入正式規格階段後再定義。

## Prompt 選擇與組合

### 選擇順序

同一任務可能符合多個 Prompt。預設選擇優先順序應為：

1. Task 明確指定。
2. Workflow Node 指定。
3. Project Prompt Selection Rule。
4. AI 從允許的 Prompt 集合中選擇並記錄理由。
5. Role Default Prompt。
6. System Fallback Prompt。

AI 可以依任務從核准的 Prompt Catalog 中選擇適合的 Prompt，但不得
任意改寫安全基線、權限政策或工具限制。高風險選擇可以要求人工核准。

### Prompt 組合層次

執行時的有效 Prompt 可以由多層內容組成：

```text
System Safety Policy
+ Organization Policy
+ Project Instructions
+ Role Prompt
+ Task-type Prompt
+ Repository Context
+ Current Task
+ Required Output Schema
```

KhaiKang 應保存解析後 Prompt 的版本資訊或內容雜湊，讓執行結果可以
重現及稽核。Repository Context 應按任務需要載入，不應預設把整個
Repository 或所有文件送入 Context。

## 執行架構方向

KhaiKang 應將管理層與實際 Agent 執行環境分離：

```text
KhaiKang Control Plane
        |
        v
Agent Runtime Provider
        |
        +-- Local Runner
        +-- Container Runner
        +-- Remote Runner
        +-- Future Provider
```

Control Plane 管理 Task、Execution、Policy、Approval、Audit 與成果；
Runtime Provider 負責準備工作目錄、啟動 Agent、執行工具及回報結果。

tmux、特定 Agent CLI、Docker 或其他 process manager 都只能是 Runner
內部實作，不應成為 KhaiKang 核心領域模型的必要條件。

## 可參考的 SwarmForge 類概念

後續實作 Agent Runtime 時，可以研究並吸收 SwarmForge 類工具的以下
概念：

### 隔離工作目錄

- 使用 Git worktree、容器或其他隔離方式，避免不同 Execution 互相覆蓋。
- KhaiKang 保存 Repository、Base Revision、Branch、Commit 等可移植資訊，
  不把本機 worktree path 當成核心業務識別。

### Commit-based Handoff

- Agent 之間優先傳遞可驗證的 Commit、Patch、Artifact 或結構化成果。
- 後續角色應針對同一 Revision 進行 Review、測試或修正。
- 自然語言摘要是輔助資訊，不能取代可重現的交付證據。

### Durable State

- Execution、Task、Handoff 與 Retry 狀態不能只存在聊天 Context。
- Agent 或 Runner 重啟後，應能從持久化狀態恢復。
- 每次狀態變更要能追溯時間、執行者、原因與相關成果。

### 結構化 Handoff

- Handoff 應使用明確的 sender、recipient、task、priority、revision、
  result 及 verification evidence。
- Queue 或訊息格式應由系統驗證，不依賴 Agent 自由產生任意指令文字。

### 角色分工與獨立驗證

- 視任務風險選擇單 Agent 或多 Agent。
- Reviewer 或 Tester 應能獨立驗證 Builder 的成果。
- 不要求所有工作都經過固定數量或固定名稱的角色。

## 不直接沿用的限制

KhaiKang 不應直接複製特定工具的完整流程：

- 不固定為 two-pack、four-pack 或 six-pack。
- 不把角色與 Prompt 固定成單一作者的方法論。
- 不要求所有專案使用相同的 TDD、Gherkin、Mutation、CRAP 或 DRY 工具。
- 不把檔案 inbox / outbox 當成 KhaiKang 中央資料來源。
- 不在未固定版本與完整性驗證時下載最新 Script 或 Prompt。
- 不允許 Agent 繞過 KhaiKang Approval、Audit、Credential 與 Tool Policy
  自動 Merge、Push 或 Deploy。

SwarmForge 類工具是 Runtime 與 Handoff 的設計參考，不是 KhaiKang
產品模型或預設工程方法的唯一來源。

## 建議執行生命週期

未來正式規格可從下列狀態方向開始收斂：

```text
Pending
-> Ready
-> Running
-> WaitingForApproval | NeedsClarification | RetryRequested
-> Succeeded | Failed | Cancelled
```

每次執行至少應能提供：

- 使用的 Agent、Role、Prompt 版本與 Policy
- Repository Revision 與輸出 Revision
- 執行過的工具與驗證結果
- Commit、Draft PR、Artifact 或其他成果
- Token、成本、時間與重試次數
- 停止、失敗、要求澄清或要求核准的原因

## 導入順序

建議依下列順序發展，避免直接跳入複雜多 Agent Workflow：

1. 完成 Project、Issue、Test Management 與 Audit 基礎。
2. 建立 Git、CI、Webhook 與外部結果回寫能力。
3. 定義通用 Execution、Evidence、Approval 與 Policy 規格。
4. 完成單 Agent 的 `Task -> Execution -> Commit -> Test -> Draft PR` 閉環。
5. 加入 Prompt Catalog、版本管理及動態 Prompt Selection。
6. 加入角色交接、獨立 Review、Retry 與多 Agent Orchestration。
7. 再評估可設定 Workflow、遠端 Runner 與更複雜的執行拓撲。

## 後續實作守則

當 AI 或開發者開始設計這項能力時：

- 先閱讀本文件、產品願景、相關功能規格與當時的資料模型。
- 先區分 Agent、Role、Prompt、Execution、Workflow 與 Runner。
- 先提出正式 Spec 與資料模型，不直接依本方向文件建立資料表。
- 優先完成單 Agent 閉環，再加入多 Agent。
- 將安全、成本、停止條件、人工核准、Audit 與可重現性視為核心需求。
- 研究 SwarmForge 或其他工具時，移植經驗與可驗證機制，不複製固定角色、
  固定 Prompt 或未經確認的工程方法。

## 待收斂問題

- Prompt Catalog 的 Organization、Project 與 User scope。
- Prompt Selection Rule 的規則格式與人工覆寫方式。
- Agent 是否可以建立 Prompt 變體，以及變體的核准與版本策略。
- 本機 Runner、遠端 Runner 與 Container Runner 的第一版邊界。
- Repository credential、AI provider credential 與 tool permission 的隔離方式。
- Token、金額、時間與重試 Budget 的計算及停止策略。
- Agent Handoff、Workflow Node 與一般 WorkItem 之間的關係。
- AI 產生 Commit、Push、Draft PR、Merge 與 Deploy 的預設授權邊界。
