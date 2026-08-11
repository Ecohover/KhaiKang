# 測試管理 MVP 實作計畫

## 目的

這份文件安排 KhaiKang 手動測試管理 MVP 的實作順序、階段邊界與驗收方式。

功能規則以[測試案例管理規格](../02-specs/03-test-case-management.md)為準，資料責任以[測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md)為準。本文件只管理交付順序與完成狀態。

> 2026-08-05 起，跨模組的 MVP 收尾順序改以[收尾與發布準備計畫](./06-mvp-release-closure-plan.md)為準。Test Plan 依 Tag／搜尋批次加入案例與硬刪除不納入 MVP；以目前的案例選取、排序與封存即可。

## MVP 邊界

MVP 建立以下手動測試閉環：

```text
Test Workspace
  -> Test Suite
  -> Test Case
  -> Test Plan
  -> Test Run
  -> Manual Result
  -> Test Run Report
```

Playwright、CI 匯入、Test Environment、Test Repository、Execution Agent 與 AI 功能不納入本次 MVP。基本 Issue 關聯與測試追溯已由後續追溯階段完成。

## 第一階段：規格決策與合約邊界

狀態：已完成（2026-07-25）

已確認決策：

- MVP 僅支援手動測試。
- Test Workspace 與 Project 採多對多導覽關聯，但關聯不取代 Workspace 成員權限。
- Workspace 使用 `owner`、`manager`、`tester`、`viewer` 四種固定角色。
- 同一帳號在一個 Workspace 中只有一個有效角色。
- Suite 樹最多五層。
- Test Case 使用 UUID 作為內部主鍵，並已提供 Workspace Prefix 與 Workspace 內穩定案例流水號。
- Test Plan 第一版只要求名稱與測試目的。
- `completed` Test Run 完全唯讀；`cancelled` Run 可重新開始並保留既有結果，另可建立新 Run 作為獨立重測紀錄。

合約邊界：

- 新模組命名為 `KhaiKang.Modules.TestManagement`。
- HTTP contract 使用 `/api/v1/test-workspaces` 作為根路徑。
- 所有 HTTP operation 必須同步 canonical OpenAPI、C# contract 與 TypeScript contract。
- 跨 Workspace 存取一律拒絕，不因知道資源 UUID 而放寬。

驗收：

- 功能規格與資料模型不再包含自動化測試 MVP 的矛盾敘述。
- Workspace 成員與權限有明確資料責任。
- 後續每階段都有獨立的可操作驗收點。

## 第二階段：Test Workspace 與 Test Suite

狀態：已實作並通過自動驗證；完整畫面驗收延後（2026-08-09）

範圍：

- 建立 `KhaiKang.Modules.TestManagement`。
- 建立 Workspace、Workspace Member、Suite domain 與 EF Core migration。
- 建立 Workspace 時自動加入第一位 owner。
- Workspace 成員加入、角色調整、移除與恢復。
- Suite 建立、修改、排序、移動與停用。
- 防止跨 Workspace parent、自己指向、循環與超過五層。
- 建立 Workspace 列表、設定、成員與 Suite tree 前端頁面。

驗收：

- 授權使用者可建立 Workspace 與五層內的 Suite tree。
- 未授權使用者不可讀取或修改 Workspace 資產。
- 停用 Workspace 後維持可讀但不可異動。
- 每個 Workspace 永遠至少保留一位 owner。

## 第三階段：Test Case、Step 與 Tag

狀態：已實作並通過回歸測試；完整畫面驗收延後（2026-08-09）

已完成：

- Test Case 建立與依 Workspace、Suite 查詢。
- 建立案例時同時保存至少一個有序 Test Step。
- Suite tree 顯示案例，並可從指定 Suite 進入案例建立頁。
- Test Case 修改、移動、停用與版本衝突處理。
- Test Tag 建立、查詢與案例多對多關聯。
- 依標題、Suite、Tag 與狀態搜尋。
- 步驟重新排序、案例編輯頁未儲存提醒，以及對外案例編號與既有資料補齊。
- 案例建立／編輯採用共用表單模板；建立頁可建立後繼續新增。

範圍：

- Test Case 建立、查詢、修改、移動與停用。
- 至少一個可排序 Test Step，每步包含操作與預期結果。
- Test Tag 建立、查詢與案例多對多關聯。
- 依標題、Suite、Tag 與狀態搜尋。
- 案例編輯頁、步驟排序、未儲存提醒與 `409` 版本衝突處理。

驗收：

- 使用者可維護一份包含多步驟與多標籤的案例。
- 案例必須屬於同 Workspace 的一個有效 Suite。
- 停用案例不再加入新 Plan，但保留歷史追溯。

## 第四階段：Test Plan

狀態：核心功能與自動驗證已完成；完整端到端驗收延後（2026-08-09）

範圍：

- Test Plan 與 Test Plan Item 的建立、讀取與更新；封存取代硬刪除。
- 由 Suite 與樹狀案例選取加入案例。
- 個別移除、去重與排序。
- Plan 至少包含一個有效案例。
- Plan Item 固定範圍，不隨 Suite 或 Case 移動自動改變。
- 建立計畫改為獨立表單頁，使用共用表單操作列；由啟用計畫建立 Run 時會預選該計畫。

驗收：

- 使用者可由 Suite 快速建立計畫範圍，再手動調整。
- 同一案例在同一 Plan 中不重複。
- 既有 Plan 範圍不受案例目錄變更影響。

## 第五階段：Test Run 與手動執行

狀態：已實作並通過回歸測試；完整畫面驗收延後（2026-08-09）

範圍：

- 由有效 Test Plan 建立 Test Run。
- 在同一 transaction 建立 Case 與 Step 快照。
- Run 狀態與案例、步驟結果維護。
- `not_run`、`passed`、`failed`、`blocked`、`skipped` 結果。
- 執行頁、案例切換、快速通過、進度與結果統計。
- `completed` Run 鎖定；`cancelled` Run 可重新開始並保留既有結果。

驗收：

- Case 修改後，既有 Run 仍顯示建立當時的完整快照。
- 同一 Plan 可建立多次 Run。
- 完成的 Run 可作為唯讀手動測試報告。
- 建立 Run 改為獨立表單頁，避免在清單頁以 dialog 建立。

## 第六階段：稽核與 MVP 完整驗收

狀態：MVP 基本自動驗證已完成；完整人工驗收延後

範圍：

- 補齊 Workspace、Suite、Case、Plan、Run 與 Result audit events；Workspace 資產由成員共同維護，不建立個人擁有者 ACL。
- 固定角色權限矩陣、共同維護流程與跨 Workspace 隔離測試。
- PostgreSQL transaction、migration 與 concurrent update 驗證。
- OpenAPI、C# 與 TypeScript operation 逐項對照。
- 後端 restore、build、format、test。
- 前端 type-check、test、build。
- 桌面與手機主要流程人工驗收延後至 MVP 可用版本後。
- Test Run 進入 `in_progress` 後鎖定快照結構，但不指定單一執行人；保存每筆結果的實際操作者。

## 後續階段

MVP 完成後再規劃：

- Run Item／Step 層級的細部證據追溯。
- 從失敗 Run Item 一鍵建立並帶入證據的 Bug。
- Test Environment、Repository 與 Execution Agent。
- Playwright 與 CI 結果匯入。
- 自動化診斷、簽核、進階報表與 AI。
