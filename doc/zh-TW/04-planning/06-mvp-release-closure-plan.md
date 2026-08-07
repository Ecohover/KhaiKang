# MVP 收尾與發布準備計畫

狀態：進行中（2026-08-07）

## 目的

本文件是目前 MVP 的唯一收尾優先順序。它整合專案／任務管理與手動測試管理的跨模組工作；各模組的既有實作計畫仍保留功能明細，不再各自定義 MVP 的最終範圍。

## 已確認的 MVP 邊界

### 納入 MVP

- 專案、成員、任務、指派、自由狀態流轉、列表／看板與完成資訊。
- Issue、Test Case 與 Test Run Item 附件：上傳、列表、下載與軟刪除；第一版使用 local Docker volume。
- Test Workspace、Suite、Case、Plan、Run 與手動結果的完整閉環。
- Test Workspace 與多個 Project 的關聯。
- PostgreSQL migration 整理、實際套用與發布前驗證。
- 專案與測試管理的端到端人工驗收，以及必要的授權、併發與合約驗證。

### 不納入 MVP

- Test Plan 依 Tag 或搜尋結果批次加入案例。
- Test Plan 硬刪除；以封存取代。
- Issue Tag、子任務、關聯、留言、Watcher／通知。
- 自訂角色、工作流、自訂欄位。
- CI、Jenkins、Playwright、外部結果匯入與 AI 自動化。

## 已確認設計原則

### 協作與隔離

Test Workspace 是共同維護的協作邊界。有效 Workspace 成員依固定角色共同讀取與維護 Suite、Case、Plan 與 Run，不建立 Case、Plan 或 Run 的個人擁有者 ACL。不同 Workspace 仍必須完全隔離。

### Test Run 執行人

Test Run 是否需要一位明確的「指派執行人」尚未決定。MVP 目前只需要正確保存 Run 建立者及每筆結果的實際操作人／時間；不先增加指派欄位、待辦分派或鎖定機制。

### 跨模組附件儲存

附件是 MVP 必要能力，第一版已選擇 `local`：檔案放在服務掛載的 Docker volume，適合單機或單一自架節點。程式仍透過 `IFileStorage` 存取，未來可新增 S3-compatible provider，不讓 Issue、Test Case 或 Test Run Item 直接依賴本機路徑。

資料庫只保存中繼資料與 `storage_key`，不保存檔案二進位；實體檔以 UUID key 儲存，原始檔名保留在 metadata。功能與自動化測試已完成，尚待真正 PostgreSQL 套用 migration、Docker volume 重啟與備份驗證。

### Test Workspace 與 Project 關聯

一個 Test Workspace 可以關聯多個 Project，一個 Project 也可以關聯多個 Test Workspace。採關聯表而非把單一 `project_id` 放進 Workspace 主表；本 MVP 只提供關聯與列表導覽，不新增 Issue 對 Case／Plan 的細部追溯。

## 目前工作順序

| 順序 | 工作 | 交付與完成條件 |
| --- | --- | --- |
| 1 | 完成 Issue 列表／看板收尾 | 關鍵字、類型、狀態、優先度、處理人、未指派、排序與手機替代操作皆可驗收。 |
| 2 | 決定附件儲存提供者（已完成） | 第一版採 `local` Docker volume，保留 `IFileStorage` 供未來新增 S3-compatible provider。 |
| 3 | 實作附件（功能已完成） | Issue、Test Case 與 Test Run Item 已完成 migration、上傳／列表／下載／軟刪除、權限與自動化測試；Docker 持久化仍待驗證。 |
| 4 | 定義並實作 Workspace–Project 多對多關聯 | 同步規格、資料模型、OpenAPI、後端、前端與驗收案例。 |
| 5 | 整理 migration（現在） | 盤點尚未發布的 migration，整併為可讀、可重現的一組；以乾淨 PostgreSQL 執行驗證。 |
| 6 | 端到端驗收 | 以真實使用者流程驗收 Project → Issue 與 Workspace → Case → Plan → Run；確認共同維護、Workspace 隔離、版本衝突與 Run 結果操作者資料。 |
| 7 | 發布前 migration 整理與驗證 | 凍結 schema 後再整併一次；於乾淨 PostgreSQL 套用、重啟、升級路徑與備份／還原演練皆成功。 |

## MVP 完成條件

- 使用者可管理專案與任務，並以列表或看板掌握工作。
- 使用者可在同一 Workspace 共同維護測試資產，完成一次可追溯的手動 Test Run。
- Issue、Test Case 與 Test Run Item 附件的資料與檔案可在 local Docker volume 下持久化。
- Workspace 可關聯多個 Project，且不造成跨 Workspace 資料洩漏。
- PostgreSQL migration 可從空白資料庫建立，也可在發布前指定升級路徑安全套用。
- OpenAPI、C#、TypeScript、前後端檢查及桌面／手機人工驗收皆有實際結果。

## 仍待產品決策

- Test Run 是否需要指派執行人；若需要，再定義是否允許多人共同填寫與如何呈現責任人。
