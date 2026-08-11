# 待處理清單

## 目的

這份文件用來集中管理目前尚未進入規格定稿或尚未列入近期實作的主題。

這些內容先集中放在 planning 區，不直接混入功能 spec 或資料模型文件。

## 產品功能待處理

- Issue tag
- Issue tag、留言、watcher 與通知
- 自訂角色
- 自訂工作流規則
- 自訂欄位模板

`Test Workspace` 與 `Project` 的多對多關聯已移入[目前 MVP 收尾計畫](./06-mvp-release-closure-plan.md)，不再列為後續 backlog。

基本 Issue 關聯與需求到測試的追溯已依[正式規格](../02-specs/04-issue-test-traceability.md)與[實作計畫](./09-issue-test-traceability-implementation-plan.md)完成。Run Item 細部證據、跨 Run Bug 重現與追溯報表仍保留後續討論。

## 自訂欄位設計方向待處理

- 自訂欄位不是目前 MVP 必要功能，先不列入近期實作
- 未來方向傾向採用獨立值表，而不是直接把所有自訂欄位塞進 `issues` 主表
- 目前偏向後續採用：
  - `custom_fields`
  - `custom_field_contexts`
  - `issue_custom_field_values`
- 自訂欄位實際值後續可保留 `jsonb` 彈性，但不預設依賴 Elasticsearch
- 初期仍以 PostgreSQL 作為唯一 source of truth
- 後續若全域搜尋、跨專案查詢、聚合分析需求明顯增加，再評估導入 Elasticsearch
- 自訂欄位的 audit 不建議直接沿用固定欄位 attribute 模式，而應走資料驅動 diff
- 等專案管理 MVP 主流程穩定後，再展開自訂欄位的資料表細節、驗證規則與 audit 策略

## 整合能力待處理

- 測試自動化執行流程與 Test Run 整合
- Test Repository、Test Environment、Test Execution Agent 與憑證管理
- 自動化測試結果回寫與外部結果格式匯入
- Jenkins 連線
- Job 對應
- Build 結果匯入
- JUnit XML 匯入
- 外部通知與 webhook

## AI 相關待處理

- 整體方向以 [AI Agent Orchestration 設計方向](./04-ai-agent-orchestration-direction.md) 為準
- `ai_agents` 細部欄位設計
- AI Agent、Role、Prompt Template、Execution、Workflow 與 Runner 的責任邊界
- Prompt Catalog、版本管理與依任務動態選擇 Prompt
- AI prompt / instruction 設計
- AI 觸發方式與執行模式
- AI 建立 Issue
- AI 產生測試案例草稿
- AI 與 queue / webhook / agent runtime 的整合
- 單 Agent 閉環完成後，再導入 SwarmForge 類 worktree 隔離、結構化 handoff、
  durable state、commit-based evidence 與多 Agent 協作

## 後續原則

- 若某個主題開始進入正式討論，應移到對應 spec 或 data-model 文件
- 若某個主題只是想法或方向，先留在這份文件即可
