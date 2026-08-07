# UI Guideline

## 目的

這份文件定義 KhaiKang 前端的共用 UI 基準。目標是讓不同功能頁面的控制項、表單、狀態與回饋具有一致外觀與互動，同時避免把有領域語意的流程過早抽成泛用元件。

實作來源如下：

- 視覺 token：`frontend/packages/ui/src/tokens.css`
- domain-neutral UI 元件：`frontend/packages/ui/src/`
- 應用程式共用組合元件：`frontend/apps/web/src/components/`

## 元件放置原則

| 類型 | 放置位置 | 例子 |
| --- | --- | --- |
| 無領域語意、至少兩處使用且 API 穩定 | `frontend/packages/ui/src/` | `UiButton`、`UiSelect`、`UiPagination` |
| 跨頁共用但具有 KhaiKang 資源頁語意 | `frontend/apps/web/src/components/` | `ResourcePageHeader`、`SharedBreadcrumb` |
| 只屬於單一功能或工作流程 | 所屬 feature 的 view/component | Test Run 結果填寫、Plan 案例樹 |

不可為了減少檔案數量，把 API 呼叫、權限、資料轉換或特定工作流程塞進 UI 套件。UI 套件只處理呈現、可及性與通用互動。

## 設計 token

- 顏色、圓角、陰影與字色使用 `--kk-*` token，不在頁面直接重複定義基礎色。
- 目前基準圓角為 `var(--kk-radius)`；特殊膠囊狀態可使用 `999px`。
- 新增 token 時，先確認至少兩個元件需要相同語意；不得只為一個頁面新增 token。

## 控制項尺寸

一般工具列與列表內的互動控制項使用下列預設：

| 控制項 | 預設元件 | 高度 | 規則 |
| --- | --- | --- | --- |
| 按鈕 | `UiButton` | 32px | 主要、次要與 ghost variant 高度一致。 |
| 單選下拉 | `UiSelect` | 32px | 不自行覆寫 padding 或 min-height。 |
| 搜尋欄 | 共用搜尋欄 | 32px | 與同一工具列的按鈕、下拉對齊。 |
| 分頁控制項 | `UiPagination` | 32px | 每頁筆數下拉與前後頁按鈕一致。 |

以下情境可使用不同尺寸，但元件本身要明確定義 variant，而非由頁面任意覆寫：多選標籤、狀態膠囊、極密集表格、圖示按鈕與測試步驟結果控制項。

## 表單

- 以 label、控制項、說明與驗證訊息為一組欄位；新表單優先使用 `UiField`。
- 反覆出現的區塊標題、說明、邊框與內距應使用 `UiFormSection`；在此元件完成前，新增頁面應沿用既有 `form-section` 結構，不自行創造另一套樣式。
- 單行文字與多行文字完成共用化後，使用 `UiInput`、`UiTextarea`；選單使用 `UiSelect`。
- 必填、disabled、error、focus 與小螢幕單欄排列是表單完成條件。

## 清單、表格與空狀態

- 資料列表應使用一致的外框、表頭、列間距、hover 與橫向捲動規則。
- 先建立 `UiTableContainer` 與 `UiTable` 等呈現基元；不建立 schema-driven 的萬用資料表，欄位、排序、拖拉與操作權限仍由功能頁面負責。
- 查無資料時使用 `UiEmptyState`；載入與頁面級錯誤使用 `SharedStateBanner`。
- 分頁一律使用 `UiPagination`，不得在頁面複製前後頁按鈕與 page-size 選單。

## 狀態與訊息

- 純呈現狀態使用 `UiStatusBadge`，以語意 variant 表示 `neutral`、`success`、`warning`、`danger`、`inactive`。
- 可編輯狀態仍使用 `UiSelect` 或功能專屬的明確 variant；不得把 badge 偽裝成不具可及性的可操作控制項。
- 成功寫入一律使用 `useSaveNotice`；需要使用者閱讀或確認的內容使用 `UiActionDialog`。
- 頁面內警示與錯誤訊息使用 `UiAlert`；不得於各頁重複建立相同 error banner CSS。

## 可及性與響應式

- 每個可操作控制項都必須有可理解的文字、label 或 `aria-label`。
- 鍵盤 focus 必須可見；disabled 不得只改變顏色。
- 工具列在小螢幕可換行；資料表必須可水平捲動或提供可替代的窄版呈現。
- 拖拉操作必須有非拖拉替代方式；例如以 `UiSelect` 變更狀態。

## 共用化清單與順序

| 優先級 | 元件 | 來源 | 範圍 |
| --- | --- | --- | --- |
| P0 | `UiSelect`、共用搜尋欄、`UiPagination` | 已建立或調整 | 保持 32px 基準並逐頁替換一般選單。 |
| P0 | `UiInput`、`UiTextarea`、`UiFormSection` | 表單頁重複樣式 | Project Issue、Test Case、Test Suite、Workspace。 |
| P0 | `UiAlert`、`UiEmptyState`、`UiStatusBadge` | error banner、empty state、status pill 重複 | Project 與 Test Management。 |
| P1 | `UiTableContainer`、`UiTable` | Issue、Case、Plan、Run、Member 列表 | 只共用結構與視覺，不共用資料行為。 |
| P1 | 成員管理整併 | `SharedMemberManagement`、`ResourceMemberManager` | 保留角色、權限與 API 由呼叫端注入。 |

## 實作與驗證

每次抽取共用元件時：

1. 先確認至少兩個真實呼叫端有相同需求。
2. 先加入或更新 UI 套件元件，再替換最少兩個呼叫端驗證 API。
3. 保留 feature 的權限、API、i18n 與 workflow 邏輯在 feature 端。
4. 執行 `pnpm type-check`、`pnpm test`、`pnpm build`。
5. 以繁體中文與英文各檢查一次受影響流程，並確認鍵盤與小螢幕狀態。

