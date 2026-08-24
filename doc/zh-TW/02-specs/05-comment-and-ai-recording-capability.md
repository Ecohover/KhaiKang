# Comment、追溯與 AI 操作紀錄能力規格

## 文件狀態

- 狀態：Human PO 已確認；2026-08-20 PM／Human PO 決策已收斂，可進入正式交接
- 產品階段：MVP 後續能力
- 本文件只定義 KhaiKang 產品能力；AI 流程、角色切換與自動化由外部 Skill 負責。

## 1. 目標

提供五人 AI／人員團隊共用的紀錄能力，使需求、開發、測試、Review 與交接結果能在 KhaiKang 中留下可查詢、可追溯且不重複的紀錄。

## 2. 支援資源

Comment 第一階段支援：

- Issue：User Story、Task、Test Issue、Bug
- Test Case
- Test Plan
- Test Run

未列入白名單的資源不得建立或讀取 Comment。

## 3. Comment 資料模型

建立獨立的共用 Comment Entity，不為每一種資源建立不同留言表。

必要欄位：

| 欄位 | 規則 |
| --- | --- |
| `id` | UUID 主鍵 |
| `resource_type` | 白名單資源類型 |
| `resource_id` | 來源資源識別碼 |
| `author_account_id` | 由伺服器依登入身分產生，可為 human 或 ai_agent |
| `content` | Markdown 原文 |
| `created_at` | 由伺服器產生，不接受呼叫端指定 |
| `version` | 供併發／冪等處理使用 |
| 共通軟刪除／稽核欄位 | 依現有資料模型規範保存 |

索引至少包含 `(resource_type, resource_id, created_at)` 與 `author_account_id`。

留言為平面序列，不支援留言類型、回覆、巢狀分支、編輯或刪除。

Comment 與其他可稽核資源異動必須保存不可由呼叫端偽造的操作來源。AI token API 呼叫視為 AI 操作；人為 Web session 操作視為 human 操作。歷史資料保存操作當下的 AI 帳號名稱、固定角色與真人負責人快照；擁有者轉移或帳號改名不得回寫既有紀錄。

## 4. Comment API

由獨立 Comment Controller 與 Service 提供共用 API。API 應支援：

- 依資源類型與資源 ID 分頁查詢留言。
- 建立留言。
- 回傳伺服器產生的作者、時間與 `accountType`。
- 接受冪等鍵；同一帳號、操作類型與冪等鍵重試時回傳原結果。

留言查詢沿用資源 read 權限；建立留言檢查 `comment.create`。API 不信任前端傳入的作者、時間或資源權限資訊。

同一帳號、操作類型與冪等鍵若帶入不同目標、payload 或版本，必須回傳 idempotency conflict，不得執行新操作或靜默回傳舊結果。冪等紀錄的保留／清理策略不在本階段定義，後續由備份與資料保留策略處理。

已進入可預期業務／驗證拒絕（例如長度、權限、版本或領域規則）的請求，也會消耗其 idempotency key：相同 fingerprint 重試回傳原失敗結果，不同 fingerprint 一律回傳 idempotency conflict。若第一次因逾時、連線中斷或未預期 5xx 無法確認完成結果，系統必須保留 key 與 fingerprint；只允許同一 fingerprint 重試恢復或取得最終結果，不允許以不同內容重用該 key。

## 5. Comment 前端元件

所有支援資源共用同一留言元件，元件包含：

- 置頂 Markdown 編輯／預覽輸入區。
- 最新留言在上方。
- 排序固定為 `created_at DESC, id DESC`，確保分頁穩定。
- 作者名稱、AI／人員標記、角色、建立時間與 Markdown 內容。
- 沿用既有分頁 API，但使用留言專用簡化控制列：上一頁、下一頁、目前頁次；不顯示 Page Size 下拉選單或總筆數。
- 新增成功後回到第 1 頁。
- 留言 API 獨立載入，不阻塞主資源畫面。
- 載入中顯示讀取圖示；讀取失敗自動重試最多 3 次，仍失敗時顯示「目前留言功能忙碌中，請稍後再試」與「重新讀取」按鈕。寫入失敗不得背景自動重送；呼叫端以原 idempotency key 明確重試。

## 6. Markdown 與系統設定

Markdown 安全策略為全系統設定，由系統管理者調整，不提供專案級或單則留言設定。

預設：

- 允許標題、清單、表格、程式碼與一般 URL。
- 只允許 `https://`／`http://` 一般連結與 KhaiKang 授權附件連結；圖片只可使用已上傳附件。
- 禁止原始 HTML、Script、`data:`、檔案路徑、任意外部圖片與外部嵌入元件。
- 留言長度上限 10,000 Unicode 字元，前後端都驗證；上限可調整。

## 7. 追溯關聯

沿用既有關聯模型與白名單機制，補足下列關聯：

| 關聯 | 規則 |
| --- | --- |
| `IssueParentChild` | 只允許 User Story 為父、Task／Test Issue 為子；沿用既有 `parent_of` |
| `TestCaseGeneratedByTestIssue` | Test Case 可無來源；若有，僅一個原始 Test Issue，且來源不可變更或解除 |
| `BugFoundByTestIssue` | Bug 最多一個發現它的 Test Issue，建立後不可解除 |
| `BugFoundInTestRun` | Bug 最多一個來源 Test Run，建立後不可解除 |
| `BugFoundInTestRunItem` | Bug 可無 Run Item 來源；若有，最多一個，且必須屬於同一來源 Run，建立後不可解除 |

一般 `IssueParentChild` 與 `tests` 關聯可由 PM AI 在授權範圍內建立、解除或調整；解除只解除關聯，不刪除資源。上述 Test Case／Bug 來源關聯則是不可變歷史證據。關聯類型不可由使用者或 AI 任意建立。

建立／解除關聯後，由外部 Skill 新增一般 Comment 說明，不由 KhaiKang 產生特殊系統留言。

## 8. Test Plan 與 Test Run

- Test Plan 為獨立計畫，不建立計畫繼承。
- Test Case 從 Test Plan 移除只解除收納關係，不刪除 Test Case。
- Test Run 建立時固定當下 Test Plan 的案例／測試範圍快照。
- 後續 Test Plan 異動不得改寫既有 Test Run。

## 9. AI 身分與權限

- AI 帳號使用既有 `accountType = ai_agent`；真人帳號維持 `human`，不得啟用 AI API 使用。
- 每個 AI 帳號有唯一真人擁有者、唯一不可變的固定角色與全域唯一的 `ai-` 名稱；同一真人可擁有多個 AI 帳號。僅管理員可轉移擁有者，轉移後既有 token 立即失效。擁有者不存在、移除或停用時，AI 帳號成為孤兒並立即禁止 Web／API 使用，歷史保留。
- 可建立 AI 帳號的真人帳號使用 `ai_account.create`；建立者自動成為擁有者。建立時只能選擇 PM AI、Development AI、QA AI、Reviewer AI、DevOps AI 之一，角色與權限矩陣固定且不可自訂。AI 帳號繼承擁有者可讀的 Project 與 Test Workspace 範圍；每次操作仍同時依固定角色、Project 與 Workspace 授權判斷。
- AI 帳號仍可由人類登入。建立時系統產生強初始密碼，只向建立者顯示一次；後續依一般帳號流程管理。
- AI API 使用 flag 預設關閉，僅擁有者或管理員可啟用／停用。啟用後擁有者可建立唯一一組永久有效 token；重新建立立即使舊 token 失效。token 僅以 `Authorization: Bearer` 呼叫 API，不能登入 Web，也不使用 cookie／CSRF。
- token 可於日後由擁有者查看／複製；系統需以可還原的安全機密保存且不得寫入 log、Comment 或稽核明文。管理員可啟用、停用、撤銷與轉移帳號，但不可查看既有 token 明文。token 建立、重建、查看／複製、撤銷與擁有者轉移均需留下不含明文的稽核事件。
- AI 標記可在帳號、留言作者與資源操作歷程中由 API／前端辨識。前端顯示 AI 標記、操作當下角色與真人負責人；主資源業務欄位不另加 AI flag。

固定 AI 角色的產品權限矩陣如下；所有角色皆可在自身 Project／Workspace 範圍內讀取資源及建立 Comment。

| 固定角色 | 可寫入範圍 |
| --- | --- |
| PM AI | 建立／更新 User Story、Task、Test Issue；調整 `IssueParentChild`／`tests`；指派 Development AI 或 QA AI；建立交接 Comment。不得處理測試資產、Run 結果或 Bug。 |
| Development AI | 補完自己被指派的 Development Task，回填實作狀態、完成說明、Commit／驗證證據與 Comment。不得建立或改寫需求、指派他人、異動測試資產、Bug 或追溯關聯。 |
| QA AI | 補完被指派的 Test Issue；建立／更新 Test Case、Test Plan、Test Run、Run Item 結果與自己處理的 Bug，並留下測試／缺陷證據 Comment。不得改寫 PM 已確認需求或不可變來源關聯。 |
| Reviewer AI | 只能在對應 Development Task 留下 Review Comment／建議；不得修改被審查資源、指派、關聯或流程狀態。 |
| DevOps AI | 本次僅可讀取自身範圍與建立 Comment 留下診斷／處理紀錄；不提供 Task、指派或其他產品資源寫入流程。 |

本次需在 KhaiKang 前端提供 AI 帳號與 token 管理入口：具 `ai_account.create` 的真人可建立及管理自己擁有的 AI 帳號；擁有者可啟用／停用 AI API、建立／查看／複製／撤銷 token；管理員可治理帳號與轉移擁有者。

## 10. 軟刪除與恢復

- Issue、Test Case、Test Plan、Test Run 等資源採軟刪除。
- 資源軟刪除時，Comment 與其他關聯資料保留，不另行軟刪除。
- 資源恢復後，原有 Comment 與關聯重新可見。
- 本規格不提供留言硬刪除。
- 資源軟刪除期間，一般 API／前端不顯示該資源與其 Comment／關聯，也不得新增或解除關聯；其他資源頁不得洩漏其名稱或識別資訊。

## 11. 非目標

- 不實作 AI Workflow、角色切換、Grill、交接策略或自動化旗標。
- 不實作 Agent runtime、machine-to-machine AI API 身份模型、token expiry／rotation、集中式 Audit Log 查詢後台；另立後續議題。
- 不實作留言編輯、刪除、巢狀回覆或留言類型。
- 不開放自訂關聯類型。

## 12. 驗證條件

### Backend／API

- Comment 白名單、CRUD 邊界、權限與跨資源查詢通過測試。
- 作者、時間與 `accountType` 不可由呼叫端偽造。
- 分頁、長度上限、Markdown 清理與 URL 設定通過測試。
- 冪等重試不產生重複留言、資源或關聯。
- `BugFoundInTestRunItem` 可建立與查詢、不可解除，且不改寫 Run 歷史。
- 軟刪除與恢復符合本規格。

### Frontend

- 所有支援資源使用同一留言元件。
- 留言載入、重試與錯誤狀態不阻塞主畫面。
- 簡化分頁不直接套用清單頁分頁元件。
- Markdown 編輯／預覽、作者資訊與最新留言置頂符合規格。
- AI 帳號、唯一負責人、固定角色、Project／Workspace 範圍、AI API flag、唯一 token、Bearer-only API、token 稽核與 AI 操作顯示符合本規格。

## 13. 交付前置條件

Human PO 已確認本文件，可進入正式 Development Handoff Package 與 QA 驗證交接；產品實作仍須依 Handoff Package 的角色分工與驗證閘門執行。
