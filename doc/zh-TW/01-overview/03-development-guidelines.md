# 開發規範

## 目的

這份文件定義 KhaiKang 的開發原則、程式碼邊界與實作風格。
它的角色是讓後續功能開發、重構與審查都能維持一致。

## 適用範圍

- `src/backend/`
- `src/frontend/`
- `src/worker/`
- `src/contracts/`
- `deploy/`

## 基本原則

1. 先保留可維護性，再考慮抽象化。
2. 先保留一致性，再考慮個人寫法。
3. 先支持人類工作流，再預留 AI 工作流。
4. 先做可部署、可理解的模組化單體，再考慮拆分。
5. 先把規格寫清楚，再開始實作。

## 架構原則

- 採用 modular monolith。
- 功能模組要有清楚邊界。
- 共用規則放在 contracts 或共用基礎層，不要散落在各模組。
- 不要過早拆微服務。
- 不要把跨模組規則寫死在 controller 或 page 裡。

### 範例

好的做法：

```text
Authentication -> Project -> Work Item -> Test Management
```

不好的做法：

```text
Controller A 直接呼叫 Controller B
Page A 直接複製 Page B 的流程
```

## 文件與規格原則

- 所有功能開發前先有 spec。
- spec 先用中文寫，確認後再補英文。
- 文件格式要固定。
- 每份 spec 都要有索引、範圍、非目標、流程、規則與驗收條件。

### 範例

每份 spec 的基本章節順序：

1. 目的
2. 相關文件
3. 範圍
4. 非目標
5. 核心模型
6. 流程
7. 狀態與規則
8. 安全與稽核
9. 驗收條件
10. 未決問題

## 命名原則

- 類別、介面、DTO、enum 使用英文。
- 程式碼內註解使用英文。
- 文件可以先用中文撰寫，確認後再補英文。
- 權限字串使用 `.` 作為分隔符。

### 範例

好的權限字串：

```text
user.manage
project.read
project.write
testcase.create
```

不建議：

```text
USER::MANAGE
ProjectRead
test-case-create
```

## 權限與授權原則

- 初期先使用 `admin` 與 `user` 兩個全域角色。
- 專案層級角色獨立於全域角色。
- 權限字串只描述能力，不描述範圍。
- 範圍透過 membership / assignment 表示。
- 後續需要時再加入 workspace scope。

### 範例

使用者 A：

- 全域角色：`user`
- Project X 角色：`project_admin`
- Project Y 角色：`project_member`

這表示：

- A 可以管理 Project X
- A 可以參與 Project Y
- A 不能因為在某個專案是 admin，就自動成為系統 admin

## AI 與人類工作流原則

- 第一階段先以人類工作流為主。
- AI 相關能力要先預留 queue / job / hook。
- 人類使用 UI 操作。
- AI 使用 queue / background job / agent worker。
- 通知走 webhook、站內通知、或其他通知管道。
- AI 的動作必須可審核、可回退、可追蹤。

### 範例

人類流程：

```text
User -> UI -> Change Status -> Audit Log -> Notification
```

AI 流程：

```text
User -> Queue Job -> AI Agent -> Proposal -> Approval -> Apply -> Audit Log
```

## 開發實作原則

- 小步驟實作。
- 每個功能先做最小可用版本。
- 不要一次把所有未來需求都做完。
- 先讓流程跑通，再補抽象。
- 有明顯共用邏輯時，再抽成共用元件或服務。

### 範例

好的：

```text
先完成 login
再完成 user management
再完成 project base
再完成 work item base
```

不好的：

```text
一次做完整的 user / project / test / AI / CI 全模組
```

## 審查原則

- 變更前先看 spec。
- 實作後要回頭對照 spec。
- 不要為了方便而破壞文件一致性。
- 不要把測試、規格、程式碼三者的語意寫成不同版本。

