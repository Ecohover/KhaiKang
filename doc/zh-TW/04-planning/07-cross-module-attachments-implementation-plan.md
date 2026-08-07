# 跨模組附件實作計畫

狀態：功能實作完成，待 PostgreSQL／Docker 持久化驗證（2026-08-07）

## 目的

MVP 附件能力同時支援 Project Issue、Test Case 與 Test Run Item。Test Run 的附件是個別案例執行結果的測試證據，不掛在整份 Run 上；這使截圖、log 與其他檔案能直接追溯到失敗或受阻的案例。

第一版使用 API 容器掛載的 Docker volume。資料庫不保存檔案二進位；實體檔案以 UUID 儲存，原始檔名只保存為中繼資料，供畫面顯示與下載時使用。

長文字欄位一律保存 Markdown 原文；資料庫不保存使用者輸入的 HTML。編輯器預設提供視覺編輯，也保留 Markdown 分頁供需要精確調整原文的使用者使用；閱讀模式經 sanitizer 後暫時渲染為 HTML。附件上傳成功後直接插入可點擊的檔案連結，儲存時對應為 Markdown 連結。

圖片是附件的顯示形式，而非另一套儲存機制：上傳成功後，編輯器在游標處插入 `![原始檔名](受權下載網址)` 的區塊圖片。MVP 不接受 base64、資料網址或任意外部網址。使用者選取圖片時可上移、下移或移除，藉此調整它在文件中的位置；不做自由拖曳定位，以保留 Markdown 在不同螢幕與閱讀模式下的穩定版面。

## 範圍與非目標

| 目標 | 附件掛載點 | MVP 可寫入時機 |
| --- | --- | --- |
| Issue | 既有 Issue | 專案啟用且具附件上傳權限 |
| Test Case | 既有 Test Case | Workspace 的 owner／manager 維護案例時 |
| Test Run 證據 | 既有 Test Run Item | Run 為 `in_progress`，由可執行 Run 的成員新增或移除 |

不納入 MVP：整份 Test Run 附件、步驟層級附件、外部分享連結、版本控制、預覽或縮圖、檔案內容掃毒與 MinIO／S3 的實際 provider。

Test Case 建立畫面可以先選取檔案，但只能在 Case 建立成功取得 ID 後依序上傳；若上傳失敗，Case 不回滾，畫面必須保留失敗檔案並提供重試。不可先建立無歸屬的暫存附件。

## 技術設計

### 儲存抽象

建立 domain-neutral 的 `IFileStorage`，只處理 logical `storageKey` 與 stream。MVP 介面提供：

- `WriteAsync`
- `OpenReadAsync`

使用者刪除採資料庫軟刪除，不直接刪除實體檔案；後續加入保留期清理工作時，再於 storage provider 補上實體刪除操作。

第一版登錄 `LocalFileStorage`；未來 MinIO 實作為 `S3FileStorage`。介面不得包含 `Issue`、Workspace、HTTP 的 `IFormFile` 或使用者檔名。各模組自行產生與驗證 logical key，例如：

```text
projects/{projectId}/issues/{issueId}/{attachmentId}
test-workspaces/{workspaceId}/cases/{caseId}/{attachmentId}
test-workspaces/{workspaceId}/runs/{runId}/items/{runItemId}/{attachmentId}
```

Docker 掛載點為 `/var/lib/khaikang/attachments`，以 named volume 持久化。`Attachments:Provider`、`Attachments:LocalRoot`、`Attachments:MaxFileSizeBytes` 為標準 Options；預設最大 20 MiB，可由部署環境覆寫。

### 資料模型

不建立沒有外鍵的 polymorphic `attachments` 表。各模組各自擁有中繼資料表，以保留資料完整性與模組責任：

- `issue_attachments` -> `issues.id`
- `test_case_attachments` -> `test_cases.id`
- `test_run_item_attachments` -> `test_run_items.id`

三表欄位語意一致：`id`、目標外鍵、`uploaded_by_account_id`、`original_file_name`、`storage_provider`、`storage_key`、`content_type`、`file_size`、`file_hash`、`is_deleted`、`deleted_at` 與 audit metadata。實體檔名使用附件 UUID；下載一律使用已清理的 `original_file_name` 並設定 `Content-Disposition: attachment` 與 `X-Content-Type-Options: nosniff`。

寫入流程為「檔案寫入 -> 保存中繼資料」。若保存中繼資料失敗，檔案不會形成可存取的附件，但可能留下 orphan file；此情況與軟刪除後的實體檔案，一併交由後續保留期清理工作處理，不在使用者操作中直接毀損。

## API、授權與 UI

每一種附件資源提供相同四個操作：列出、multipart 上傳、內容下載、軟刪除。API 依其父資源路由，不提供可繞過 Project 或 Workspace 邊界的全域附件 UUID 路由。

- Issue：沿用 `issue.read`、`issue.attachment.upload`、`issue.attachment.delete`。
- Test Case：成員可讀取；owner／manager 可上傳及刪除，並受 Workspace 停用限制。
- Test Run Item：有效成員可讀取；僅 Run 可執行的角色可在 `in_progress` 時上傳及刪除。`completed` 後一律唯讀。

Issue 編輯頁、Test Case 建立／編輯頁與 Test Run 執行頁各放一個相同互動語意的附件區塊：檔案清單、上傳控制、下載與可用時的移除。共用 UI 僅抽取無領域語意的檔案清單／上傳控制；資料查詢、權限與寫入流程留在各 feature。

長文字欄位使用共用的 `UiMarkdownEditor` 與 `UiMarkdownContent`：編輯器提供所見即所得的粗體、斜體、標題、清單、程式碼與連結工具列，預設不顯示 Markdown 原始碼；附件 API 完成後再啟用圖片工具列，讓它直接開啟檔案選擇與上傳流程。`UiMarkdownContent` 負責安全閱讀渲染。標題、名稱、代號與其他短文字欄位維持純文字；只將描述、前置條件、預期／實際結果、Issue 的需求與完成說明等多行內容視為 Markdown。

## 實作順序

1. [x] 新增 Options、`IFileStorage`、`LocalFileStorage` 與 Docker volume；補 provider 缺失或路徑不可用的啟動驗證。
2. [x] 完成 Issue 附件資料表、OpenAPI、後端、前端與 integration tests，作為第一個垂直切片。
3. [x] 完成 Test Case 附件；建立頁採「先建立、後上傳、失敗可重試」流程。
4. [x] 完成 Test Run Item 證據附件及 Run 狀態唯讀限制。
5. [ ] 以乾淨 PostgreSQL 套用兩個 module 的 migration，確認 Docker volume 重啟後檔案仍可下載。
6. [ ] 後續發布前 migration 收斂時，一併檢查附件 migration 的升級路徑；不得直接刪除已套用 migration。

## 驗收與安全條件

- 同名檔案上傳不互相覆蓋，下載仍顯示原始檔名。
- 檔名不會形成路徑穿越；超過大小限制、空檔與不存在的父資源均回傳明確錯誤。
- 不具父資源存取權或跨 Workspace／Project 的帳號，不能列出、下載、上傳或刪除。
- 停用 Project／Workspace、完成的 Test Run 均拒絕不允許的異動。
- 軟刪除後無法再下載，且保留 audit 與 metadata。
- OpenAPI、C#、TypeScript、前端及 integration tests 同步；Docker 重啟後附件仍可下載。

## 文件同步狀態

- [x] 專案管理規格與資料模型：保留並完成 `issue_attachments` 定義。
- [x] 測試案例管理規格與資料模型：新增 Test Case 與 Test Run Item 證據附件的功能與資料表定義。
- [x] MVP 收尾計畫與兩份模組實作計畫：將附件範圍從 Issue 擴充為三個掛載點。
