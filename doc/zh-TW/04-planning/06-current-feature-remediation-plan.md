# 現行功能修補與文件同步計畫

狀態：已結案／歷史文件（2026-08-09）。核心修補已完成；完整桌面／手機人工驗收與 Playwright E2E 已移出目前 MVP。後續狀態以 [MVP 收尾與發布準備計畫](./06-mvp-release-closure-plan.md)為準。

## 目的

本文件保留 2026-08-02 對當時 `main` 功能的盤點結果與修補順序，供追溯使用；它不再管理目前執行進度，也不取代既有功能規格、資料模型或 HTTP 合約。

## 依據與範圍

- [測試案例管理規格](../02-specs/03-test-case-management.md)
- [測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md)
- [測試管理 MVP 實作計畫](./05-test-management-implementation-plan.md)
- [文件撰寫規範](../../documentation-guidelines.md)

本輪只處理既有專案管理與測試管理功能的完整性、一致性與可驗證性；不納入 CI 匯入、AI 產生案例、Issue 觸發 Test Run、測試環境或執行代理。

## 現況摘要

目前已可使用的核心流程：

1. 系統初始化、登入、帳號與角色管理。
2. 專案、成員、設定與 Issue 管理。
3. Test Workspace、最多五層的 Test Suite 樹、Test Case 建立／修改／停用。
4. Test Plan 的案例選取、排序、建立與修改。
5. Test Run 建立、開始、取消、重新開始、完成，以及案例與步驟結果的連動。
6. Docker Compose 自架與 Docker Hub 映像發布。

## 2026-08-02 發現的落差與結案結果

| 編號 | 類別 | 當時問題 | 結案結果 | 原優先度 |
| --- | --- | --- | --- | --- |
| R-01 | i18n | 深層專案與測試頁面仍有硬編碼中文。 | 中英文產品字典、共用元件與鍵值一致性測試已補齊；完整雙語人工驗收延後。 | P0 |
| R-02 | 文件 | 測試管理計畫的完成狀態與功能不一致。 | 規格、資料模型、MVP 計畫與中英文核心文件已同步。 | P0 |
| R-03 | 文件／規格 | Test Run 重新開始規則尚未同步，且 Test Case 對外編號在盤點當時尚未實作。 | 重新開始規則已同步；Workspace Prefix、`case_no`、`{PREFIX}-TC{case_no}`、唯一索引、API、前端與整合測試均已完成。 | P0 |
| R-04 | 測試案例 | 案例搜尋、篩選、移動與 Suite 編輯入口不足。 | 核心維護流程已實作並通過回歸測試；完整畫面驗收延後。 | P1 |
| R-05 | 測試計畫 | Plan 瀏覽、篩選與操作驗證不足。 | MVP Plan 建立、讀取、更新、排序、封存與 Run 導覽已完成；進階歷程與大量資料體驗留待後續。 | P1 |
| R-06 | 品質 | 核心流程缺少 API 與瀏覽器端 E2E 回歸保護。 | API、後端與前端自動回歸已補強；Playwright E2E 依目前 MVP 邊界延後。 | P1 |

## 修補原則

1. 翻譯只處理產品提供的文字；使用者輸入的名稱、描述、案例內容與結果一律保留原文。
2. 共用操作、狀態標籤、通知、確認視窗與分頁元件優先共用，禁止在單一頁面複製另一套互動規則。
3. Test Run 的狀態、案例結果與步驟結果必須由後端驗證；前端停用按鈕僅是輔助，不可取代伺服器端規則。
4. 規格與現行實作有衝突時，先由產品確認欲保留的行為，再同步更新規格、資料模型、OpenAPI、程式與測試；不得只修改其中一處。

## 交付順序

### 第一階段：建立文件基線（P0，已完成）

- 逐條比對測試管理規格、資料模型、實作計畫與現行 API／UI。
- 將已實作的 Test Run 取消後重新開始規則同步至資料模型、實作計畫與 OpenAPI。
- 已確認 Test Case 對外編號格式、Prefix 設定位置與 Workspace 內唯一性範圍。
- 已依編號決策同步權威規格、資料模型、schema、API、前端與 baseline migration。

完成條件：不再有已知規格與現行產品行為互相矛盾的條目。

### 第二階段：完成前端 i18n（P0，核心完成）

- 清除專案、Issue、Test Workspace、Suite、Case、Plan、Run 與管理者頁面的產品文字硬編碼。
- 補齊 `en` 與 `zh-TW` 字典，並維持鍵值集合一致的自動測試。
- 確認語系切換後不顯示 key 名稱或回退成另一種語言。

完成條件：產品提供的 UI 文案可完整以英文或繁體中文顯示；使用者資料不被翻譯。

### 第三階段：測試資產維護（P1，已完成）

- 完成 Suite 的編輯、移動與停用操作。
- 完成 Test Case 的清單檢視、搜尋、狀態／Suite 篩選與移動。
- 針對編號、Prefix 與版本衝突建立前後端驗證與明確通知。

完成條件：使用者可在不離開工作區的情況下找到、整理與維護既有案例。

### 第四階段：Test Plan 與 Test Run 回歸保護（P1，核心完成）

- 補齊 Plan／Run 清單的篩選、狀態顯示與空白狀態。
- 為開始、取消、重新開始、完成與案例／步驟結果連動新增 API 測試。
- 建立 Case、建立 Plan、建立與完成 Run 的 Playwright E2E 路徑依目前 MVP 邊界延後。

完成條件：核心手動測試流程有可重複執行的自動驗證，且失敗狀態容易定位。

## 每一項修補的驗證

- 前端：`pnpm type-check`、`pnpm test -- --run`。
- 後端：`dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1`，`dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1`。
- 功能調整：同步檢查 OpenAPI、前端合約與相關文件。
- UI 調整：以英文及繁體中文各走一次受影響的主要操作流程。

## 不在本輪範圍

- 自動化測試執行與外部結果匯入。
- CI、Jenkins、Playwright 或 Issue 對 Test Run 的觸發整合。
- AI 產生、分析或維護測試案例。
- 測試環境、測試倉庫與執行代理管理。
