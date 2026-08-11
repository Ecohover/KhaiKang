# 測試案例管理規格

## 目的

這份文件定義 KhaiKang 第一階段的測試案例管理功能規格。

目標是讓團隊在 Test Workspace 中建立可重複使用的測試案例，組織測試範圍，執行測試並保留每次結果；同時保留未來與 Project、Issue、CI 與 AI 整合的空間。

## 範圍狀態

第一期 MVP 已確認只提供手動測試管理：Test Suite、Test Case、Test Plan、Test Run 與執行結果。

自動化執行仍是未來方向，尚未確認操作流程、Repository 管理、測試環境、執行 Agent、結果匯入與憑證管理方式。因此不納入目前實作、資料表欄位或驗收條件；確認後再另行展開規格。

## 相關文件

- [文件規範](../../documentation-guidelines.md)
- [文件導覽](../01-overview/01-documentation-map.md)
- [語詞總表](../01-overview/04-domain-glossary.md)
- [開發規範](../01-overview/03-development-guidelines.md)
- [專案管理規格](./02-project-management.md)

## 範圍

目前規格涵蓋以下功能：

- 測試工作區建立、停用、成員與固定角色管理
- 測試案例建立、修改、停用與查詢
- 樹狀測試套件建立與案例分類
- 測試標籤建立與案例標記
- 測試計畫建立、手動編排案例範圍與排序
- 從測試管理頁面依測試計畫建立手動測試執行
- 逐一記錄測試案例的執行結果
- 保存測試執行當下的案例內容快照

## 非目標

目前不包含：

- CI 結果匯入
- 前端或後端單元測試與 Test Case 的對應
- Vitest、xUnit 或其他單元測試結果的 Test Run Item 匯入
- 測試案例自動產生
- AI 自動執行或判定結果
- 測試案例版本管理 UI
- 複雜的測試排程、簽核或報表
- 由 Issue 觸發 Test Run 或顯示測試結果
- Playwright 或其他自動化測試的觸發、執行與結果匯入
- Test Repository、Test Environment、Test Execution Agent 與測試憑證管理

## 核心模型

### Test Workspace

`Test Workspace` 是測試資產與存取權限的根容器。MVP 可透過多對多關聯連結多個 Project，但 Workspace 仍是測試資產的唯一存取與共同維護邊界；Project 關聯不取代 Workspace 成員與固定角色。

建立 Test Workspace 的帳號自動成為 `owner`。MVP 使用固定角色：

- `owner`：管理 Workspace、成員及全部測試資產；每個 Workspace 至少保留一位 owner。
- `manager`：管理成員、Suite、Case、Plan 與 Run，但不能停用 Workspace 或移除最後一位 owner。
- `tester`：讀取測試資產、建立及執行 Test Run，不能管理成員或修改案例目錄。
- `viewer`：只能讀取測試資產與既有執行結果。

同一帳號在一個 Workspace 中只能有一個有效成員關係及一個固定角色。資產不建立個人擁有者 ACL；具備對應角色的成員共同維護。自訂角色不屬於 MVP。

### Test Case

`Test Case` 是可重複使用的測試規格，必須屬於一個 Test Workspace。

一份測試案例至少應描述：

- 測試目標
- 前置準備（可選）
- 多個可排序的測試步驟
- 每個測試步驟的預期結果
- 整體預期結果（可選）

測試案例是規格本體，不等同某一次實際執行紀錄。MVP 中每份測試案例必須屬於一個測試套件。

每個測試案例都可手動執行。未來若加入自動化測試，應以可選的綁定方式擴充，不得改變既有 Test Plan 或手動測試流程。

### Test Suite

`Test Suite` 是測試案例的功能或模組分類集合，例如登入、專案管理或 Issue 管理。

- 一個測試套件可包含多個測試案例。
- 一個測試套件可包含子套件，形成樹狀結構。
- MVP 中一個測試案例只屬於一個測試套件。
- 測試套件與測試案例都屬於同一個 Test Workspace。

### Test Step

`Test Step` 是測試案例中一個可排序的操作單位。

- 每個步驟至少包含操作說明與預期結果。
- 測試執行時，使用者可以依步驟逐一驗證並記錄實際結果。

### Test Tag

`Test Tag` 是系統共用的標籤，用於標記測試案例的跨分類特性或測試目的，概念類似 Robot Framework 的 tag。

- 一份測試案例可以有多個標籤。
- 一個標籤可以套用到多份測試案例。
- 標籤適合表示 `smoke`、`regression`、`api`、`critical` 等特性，不用來取代功能分類。

### Test Plan

`Test Plan` 定義一次測試工作的預定範圍與目的，例如某個版本驗證、某張需求的驗收或回歸測試。

使用者以測試套件樹狀結構、標籤與搜尋條件查找測試案例後，手動加入、移除及排序 `Test Plan Item`。

測試套件與標籤只作為瀏覽、搜尋與批次加入工具，不直接成為 Test Plan 的固定篩選條件。`Test Plan Item` 才是該計畫要涵蓋的明確案例範圍，與實際執行結果分開管理。

建立 Test Plan 時，應支援先加入一整個 Test Suite 的案例，再手動移除不需執行的案例。

### Test Run

#### 已實作的執行規則

- 建立 Test Plan 時，案例選取器使用與 Test Suite 相同的樹狀結構；使用者可收合套件、勾選一個 Suite 批次加入其本身及子套件的啟用案例，也可逐一勾選或取消單一案例。計畫實際保存的仍是固定排序的 Test Plan Item，不保存動態 Suite 篩選條件。
- 建立 Test Run 時，系統依 Test Plan Item 建立案例與步驟快照。後續修改案例或套件，不得改變既有 Run 的範圍或內容。
- 使用者必須為每一個有步驟的 Test Run Item 的所有 Test Run Item Step Result 記錄非 `not_run` 的結果，才能將 Test Run 設為 `completed`；沒有步驟的案例則以案例結果為準。前端應停用完成操作；後端也必須拒絕未完成項目的完成請求，避免直接呼叫 API 繞過畫面限制。
- `cancelled` 不要求所有項目完成，且可由使用者重新開始並回到 `in_progress`，保留既有案例與步驟結果以便續測；只有 `completed` 的 Run、案例結果與步驟結果一律唯讀。
- Test Run 建立後為 `not_started`，使用者必須明確觸發「開始執行」才能進入 `in_progress` 並編輯案例或步驟結果。執行中畫面以焦點底色標示目前處理的案例；使用者可切換焦點案例，但不得同時將多個案例標示為目前處理中。
- Test Run 進入 `in_progress` 後即鎖定建立當下的案例與步驟快照，不允許變更執行範圍或快照結構。鎖定不綁定單一執行人；任何具備 Run 執行權限的有效 Workspace 成員都可共同填寫結果，系統仍逐筆保存實際操作人與時間。
- 執行畫面必須以一致的結果視覺呈現：`not_run` 與 `skipped` 為中性灰、`passed` 為綠、`failed`／`blocked` 為紅；任一步驟不通過或受阻時，案例摘要即為不通過，不提供「部分通過」結果。
- 結果色彩不只用於標籤與下拉欄位，也必須套用至整個步驟列與案例卡片的淡色底；案例卡的顏色依其步驟結果彙總計算。
- 案例結果與步驟結果採自動儲存：選擇結果狀態時立即送出；步驟的實際結果在輸入框失去焦點時送出。執行畫面不提供額外的案例結果或步驟結果儲存按鈕。
- 案例與步驟結果雙向連動：使用者設定案例結果時，系統應同步套用至全部步驟；使用者更新任一步驟結果時，系統應重新計算並儲存案例結果，讓 Test Run 進度、案例卡顏色與清單統計保持一致。

`Test Run` 是依據一份測試計畫進行的一次實際測試活動。

同一份測試計畫可以有多次測試執行，例如第一次測試失敗後，修正完成再進行回歸測試。

每次 Test Run 都支援手動填寫結果。

`Test Run` 本身就是 MVP 的測試報告，顯示執行範圍、案例與步驟結果、狀態統計、執行時間與執行人。MVP 不另建立 Test Report。

### Test Run Item

`Test Run Item` 是一次 `Test Run` 中一個測試案例的個別執行項目。

每個執行項目必須保存：

- 當次執行的結果狀態
- 實際結果與備註
- 執行者與執行時間
- 當次案例內容快照

保存快照的原因是測試案例之後可能被修改；既有執行結果必須仍能對應當時被執行的內容。

### 核心關係

```text
Test Workspace
  |- Test Suites -> Test Cases <-> Test Tags
  |- Test Plans -> Test Plan Items -> Test Cases
  `- Test Runs -> Test Run Items -> Test Case Snapshot
```

## 流程

### 測試案例維護

具備 Test Workspace 存取權的成員建立測試案例時，必須指定一個測試套件與至少一個測試步驟；每個步驟都填寫預期結果。前置準備可省略，案例可加入零個或多個測試標籤。

已不再使用的案例應停用，而非直接刪除；既有測試執行紀錄不得因此失去可追溯性。

### 建立測試計畫

使用者建立測試計畫時，應設定測試目的，並從測試套件樹、標籤或搜尋結果手動加入需要執行的測試案例。

系統應支援整個 Suite 批次加入、個別案例加入、移除與排序。建立後，計畫案例清單固定，後續 Suite 或 Tag 異動不應自動改變既有計畫範圍。

### 執行測試

使用者從測試管理頁面的測試計畫建立 `Test Run` 後，系統為每個計畫案例建立一筆 `Test Run Item`。

手動執行時，執行者逐一記錄每個項目的結果與實際說明。

手動執行完成後，使用者應可快速查看整體結果與失敗、阻塞項目。

MVP 不從 Project Issue 直接觸發 Test Run；Project 關聯只提供 Workspace 與 Project 的導覽，不改變既有 Test Plan 與 Test Run 流程。

### 未來方向：自動化執行（待確認）

未來可評估以 Playwright 支援 API 與 Browser E2E 測試，但以下流程尚未確認，不屬於 MVP：

- `Test Environment`：被測系統的目標環境，例如網站 URL、測試帳號或 API Base URL。
- `Test Repository`：自動化測試程式所在的程式庫與要執行的 branch 或 commit。
- `Test Execution Agent`：實際執行 Playwright 的服務、VM 或容器。

是否由 KhaiKang、CI 工具或外部服務觸發，以及是否採用 Agent、結果格式與回寫規則，均待後續規格確認。

Playwright 用於 API 與 Browser E2E 測試。Vitest、xUnit 與其他前後端單元測試不與 Test Case 綁定，仍可由各服務自己的開發與 CI 流程執行。

## 狀態與規則

### 測試結果狀態

MVP 建議每個 `Test Run Item` 支援：

- `not_run`：尚未執行
- `passed`：執行結果符合預期
- `failed`：執行結果不符合預期
- `blocked`：因環境、權限、相依服務或其他阻礙而無法執行
- `skipped`：本次刻意不執行

### 基本規則

- `Test Case`、`Test Suite`、`Test Plan` 與 `Test Run` 都必須屬於同一個 Test Workspace。
- 每個 `Test Case` 在 MVP 中必須且只能屬於一個 `Test Suite`。
- `Test Suite` 可以形成最多五層的樹狀結構；移動套件或案例不得改變既有 Test Plan 的項目範圍。
- 只有具備該 Test Workspace 存取權的成員可以查看或操作測試管理資料。
- Test Workspace 可關聯多個 Project，Project 也可關聯多個 Workspace；Workspace–Project 關聯本身只提供導覽與範圍前置條件。Case 需求關聯、Plan 測試 Issue、Run 快照與 Bug 來源由獨立的 [Issue 與測試資產追溯規格](./04-issue-test-traceability.md) 定義。
- 有效 Workspace 成員皆可查看關聯專案的最小導覽資訊；只有有效的 `owner` 或 `manager` 可新增或移除關聯。
- 建立關聯時，操作人必須同時具備該 Project 的 `project.read`，且 Project 必須為使用中；關聯本身不會授與 Project 或 Workspace 存取權。
- Test Case 以 UUID 作為內部主鍵，並提供穩定的人類可讀編號。Workspace 使用唯一 `prefix`，案例在 Workspace 內使用遞增 `case_no`，對外顯示為 `{PREFIX}-TC{case_no}`；移動 Suite 或修改標題不得改變案例編號。
- `Test Run` 必須來自一份 `Test Plan`；MVP 不先提供無計畫的臨時執行流程。
- `Test Run` 只能由使用者從測試管理頁面手動觸發；不支援 Issue、CI 或 AI 觸發。
- 每個 Test Run Item 都可由使用者手動填寫結果。
- Vitest、xUnit 與其他單元測試不與 Test Case 綁定，其結果不建立 Test Run Item。
- Test Plan 至少必須有一個 Test Plan Item；不得建立空的 Test Plan。
- Test Suite 與 Tag 可用於分類與找出案例；MVP 以 Suite 與樹狀案例選取加入 Plan，不提供 Tag 或搜尋結果批次加入，亦不提供 AND、OR、NOT 或巢狀條件編輯。
- `Test Plan` 建立後的案例清單應固定，後續套件異動不應自動改變既有計畫。
- `Test Run Item` 建立時必須保存案例內容快照。
- `Test Plan` 在 MVP 中只要求名稱與測試目的，不先加入目標版本、環境或負責人欄位。
- `completed` 的 `Test Run` 與其結果完全唯讀。`cancelled` 的 Run 可重新開始並回到 `in_progress`，保留既有案例與步驟結果；若需新的獨立測試紀錄，使用者仍可建立新的 `Test Run`。
- `in_progress` 的 Test Run 鎖定案例與步驟快照，但不建立單一執行人鎖；具執行權限的成員可共同更新結果。

## 安全與稽核

測試管理相關操作必須可稽核，至少包含：

- 測試案例建立、修改與停用
- 測試套件與案例關聯異動
- 測試案例步驟與預期結果異動
- 測試計畫建立與範圍調整
- 測試執行建立與完成
- 測試執行項目結果變更

所有稽核紀錄都必須能對應到 `actor_id` 與 `actor_type`。

## 驗收條件

- 使用者可在已授權的 Test Workspace 內建立與查詢測試案例。
- 使用者可建立樹狀測試套件，並將案例放入指定套件。
- 每個測試案例可包含前置準備，並包含可排序步驟與每個步驟的預期結果，且可套用多個測試標籤。
- 使用者可建立測試計畫，從 Suite 與樹狀案例選取加入、移除及排序案例。
- 使用者可先批次加入一個 Suite 的全部案例，再手動移除部分案例。
- 使用者可依測試計畫建立多次測試執行。
- 使用者可從測試管理頁面手動開始、持續執行與完成一份手動測試執行。
- Test Run 可作為完整手動測試報告，顯示結果統計、執行時間與執行人。
- 每次測試執行都可記錄每個案例的獨立結果。
- 案例更新後，既有測試執行仍可檢視當次案例內容快照。
- 測試結果可區分未執行、通過、失敗、阻塞與略過。

## 未決問題

- 初期是否只允許一個預設 Test Environment、Test Repository 與 Test Execution Agent。
- 未來是否支援由 Test Issue 觸發 Test Run，並回寫測試摘要到 Issue。
- 測試結果失敗時，是否需要提供一鍵建立 Bug Issue。
- 未來 CI 匯入結果時，是否建立新的 `Test Run` 或寫入既有執行。
