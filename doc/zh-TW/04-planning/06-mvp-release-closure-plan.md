# MVP 收尾與發布準備計畫

狀態：核心實作與 PostgreSQL baseline 驗證已完成（2026-08-09）；完整人工驗收與正式發布演練延後。

## 目的

本文件是目前 MVP 的唯一收尾優先順序。它整合專案／任務管理與手動測試管理的跨模組工作；各模組的既有實作計畫仍保留功能明細，不再各自定義 MVP 的最終範圍。

## 已確認的 MVP 邊界

### 納入 MVP

- 專案、成員、任務、指派、自由狀態流轉、列表／看板與完成資訊。
- Issue、Test Case 與 Test Run Item 附件：上傳、列表、下載與軟刪除；第一版使用本機 `IFileStorage`，容器部署時可掛載 Docker volume。
- Test Workspace、Suite、Case、Plan、Run 與手動結果的完整閉環。
- Test Workspace 與多個 Project 的關聯。
- 固定 Issue 關聯，以及 Case 需求、Plan 測試 Issue、Run 快照與 Run 開立 Bug 的基本追溯。
- PostgreSQL migration 整理、實際套用與發布前驗證。
- 專案與測試管理的核心流程 smoke check，以及必要的授權、併發與合約自動驗證；完整桌面與手機人工驗收延後。

### 不納入 MVP

- Test Plan 依 Tag 或搜尋結果批次加入案例。
- Test Plan 硬刪除；以封存取代。
- Issue Tag、子任務、留言、Watcher／通知，以及可自訂的關聯類型。
- 自訂角色、工作流、自訂欄位。
- CI、Jenkins、Playwright、外部結果匯入與 AI 自動化。

## 已確認設計原則

### 協作與隔離

Test Workspace 是共同維護的協作邊界。有效 Workspace 成員依固定角色共同讀取與維護 Suite、Case、Plan 與 Run，不建立 Case、Plan 或 Run 的個人擁有者 ACL。不同 Workspace 仍必須完全隔離。

### Test Run 執行鎖定

Test Run 進入 `in_progress` 後鎖定建立當下的案例與步驟快照，不再變更執行範圍或快照結構。MVP 不指定或限制單一執行人；任何具備 Run 執行權限的有效 Workspace 成員都可共同填寫結果，系統保存 Run 建立者及每筆結果的實際操作人／時間。

### 跨模組附件儲存

附件是 MVP 必要能力，第一版已選擇 `local`：檔案放在 API 設定的本機持久化目錄；容器部署時可將該目錄掛載為 Docker volume，適合單機或單一自架節點。程式仍透過 `IFileStorage` 存取，未來可新增 S3-compatible provider，不讓 Issue、Test Case 或 Test Run Item 直接依賴本機路徑。

資料庫只保存中繼資料與 `storage_key`，不保存檔案二進位；實體檔以 UUID key 儲存，原始檔名保留在 metadata。功能、自動化測試、空白 PostgreSQL baseline 套用與本機 API 重啟持久化基本驗證已完成；Docker volume 與備份／還原演練延後至正式發布準備。

### Test Workspace 與 Project 關聯

一個 Test Workspace 可以關聯多個 Project，一個 Project 也可以關聯多個 Test Workspace。採關聯表而非把單一 `project_id` 放進 Workspace 主表；關聯本身只提供導覽與授權範圍，Case／Plan／Run 的追溯資料由獨立關聯與快照保存。

## 目前工作順序

| 順序 | 工作 | 交付與完成條件 |
| --- | --- | --- |
| 1 | 完成 Issue 列表／看板收尾（已完成） | 關鍵字、類型、狀態、優先度、處理人、未指派、排序與手機替代操作均已實作並通過自動驗證；完整人工驗收延後。 |
| 2 | 決定附件儲存提供者（已完成） | 第一版採本機 `IFileStorage`；容器部署時可掛載 Docker volume，並保留介面供未來新增 S3-compatible provider。 |
| 3 | 實作附件（功能已完成） | Issue、Test Case 與 Test Run Item 已完成 migration、上傳／列表／下載／軟刪除、權限與自動化測試；MVP 只做一次本機儲存重啟後仍可下載的基本驗證。 |
| 4 | 定義並實作 Workspace–Project 多對多關聯（已完成） | 已同步規格、資料模型、OpenAPI、後端、前端與整合測試，並已在重置後的 PostgreSQL baseline 中套用。 |
| 5 | 整理 migration（已完成） | 尚未發布的 migration 已依三個 DbContext 各自整併為單一 baseline，並於重置後的測試 PostgreSQL 從空白資料庫成功套用。 |
| 6 | 基本人工驗收（延後補強） | MVP 只做可啟動與核心流程 smoke check；完整桌面、手機與細部問題修正延後處理。 |
| 7 | MVP 發布確認（基本驗證已完成） | Baseline 已從空白 PostgreSQL 建立，API 重啟後本機附件儲存內容仍存在；升級路徑與備份／還原演練延後至正式發布準備。 |

## MVP 完成條件

- 使用者可管理專案與任務，並以列表或看板掌握工作。
- 使用者可在同一 Workspace 共同維護測試資產，完成一次可追溯的手動 Test Run。
- Issue、Test Case 與 Test Run Item 附件的資料與檔案可在設定的本機儲存目錄持久化，容器部署時可改由 Docker volume 保存。
- Workspace 可關聯多個 Project，且不造成跨 Workspace 資料洩漏。
- PostgreSQL migration 可從空白資料庫建立；既有正式資料庫的升級路徑留待正式發布前演練。
- OpenAPI、C#、TypeScript 與前後端自動檢查已有實際結果；完整桌面／手機人工驗收延後。

## MVP 後續工作

- 完整桌面／手機人工驗收、CI、Playwright、自動匯入與 AI 測試均延後到 MVP 可用版本完成後。
- 發布自動化與隔離備份／還原演練改由[發布自動化與交付演練計畫](./08-release-automation-plan.md)追蹤。
