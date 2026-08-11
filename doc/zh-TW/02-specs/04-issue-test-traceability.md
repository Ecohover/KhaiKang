# Issue 與測試資產追溯規格

狀態：已完成並通過整合、前端與全新 PostgreSQL／Docker smoke 驗證（2026-08-11）。

## 目的

本功能將「需求、測試任務、測試案例、測試計畫、測試執行與缺陷」串成可查詢的追溯鏈，同時維持 Project 與 Test Workspace 原有的存取邊界。

核心追溯方向為：

```text
需求 Issue <- tests - 測試 Issue <- Test Plan <- Test Run -> Bug Issue
     ^
     |
 Test Case
```

## 相關文件

- [專案管理規格](./02-project-management.md)
- [測試案例管理規格](./03-test-case-management.md)
- [Issue 與測試資產追溯資料模型](../03-data-model/05-issue-test-traceability-data-model.md)
- [Issue 與測試資產追溯實作計畫](../04-planning/09-issue-test-traceability-implementation-plan.md)

## 範圍

第一版包含：

- Issue 之間的基本關聯，以及方向與從屬規則。
- Test Case 與一或多張需求 Issue 的直接關聯。
- Test Plan 與一張測試 Issue 的直接關聯。
- 建立 Test Run 時保存 Plan 當時的測試 Issue 快照。
- Test Run 與該次執行開立之 Bug Issue 的來源關聯。
- 從 Issue 或測試資產查看有權存取的追溯資料。
- 使用目前的 Workspace–Project 關聯、Workspace 固定角色及 Project 權限判斷存取。

## 不處理範圍

第一版不包含：

- 自訂 Issue 關聯類型或關聯流程編輯器。
- 自動依 Issue 關聯產生 Case、Plan 或 Run。
- Run Item 或 Step 層級的 Bug 證據關聯。
- 覆蓋率權重、簽核或自動完成判定。
- CI、Playwright、外部結果匯入或 AI 自動建立關聯。

## Issue 基本關聯

Issue 關聯由 Project Management 擁有。第一版提供固定關聯類型：

| 關聯 code | 正向顯示 | 反向顯示 | 方向 | 規則 |
| --- | --- | --- | --- | --- |
| `related` | 關聯於 | 關聯於 | 對稱 | 只保存一筆標準化關聯。 |
| `parent_of` | 父層於 | 子層於 | 從屬 | 來源為父 Issue、目標為子 Issue；一張 Issue 最多一個有效父層，且不得形成循環。 |
| `blocks` | 阻擋 | 被阻擋於 | 有向 | 來源 Issue 阻擋目標 Issue。 |
| `duplicates` | 重複於 | 被重複於 | 有向 | 來源為重複 Issue，目標為保留處理的主要 Issue。 |
| `tests` | 測試／驗證 | 被測試／驗證於 | 有向 | 來源為測試 Issue，目標為其涵蓋的需求 Issue。 |

第一版只允許同一 Project 內建立 Issue 關聯。所有類型都禁止自己關聯自己，也不得重複建立相同有效關聯。`related` 不區分來源與目標；其他類型必須保留方向。

測試 Issue 第一版沿用 `task` 類型；是否新增專用 Issue Type 留待實際使用後決定。`tests` 的關聯語意不依顯示名稱判斷，系統以關聯類型 code 為準。

## 測試追溯關係

### Case 與需求 Issue

- 一張 Case 可以驗證多張需求 Issue。
- 一張需求 Issue 可以由多張 Case 驗證。
- 關聯只表示「此案例用於驗證該需求」，不代表需求已通過測試。
- 需求 Issue 一般為 `story` 或 `task`，第一版不以型別硬性阻擋其他 Issue。

### Plan 與測試 Issue

- 一份 Plan 最多直接關聯一張測試 Issue。
- 一張測試 Issue 可以在不同版本或週期建立多份 Plan。
- 測試 Issue 透過 `tests` Issue 關聯涵蓋一或多張需求 Issue；Plan 不重複保存需求 Issue 清單。
- 為相容既有資料，Plan 的測試 Issue 可為空；新建或編輯時應提供選擇入口。

### Run 與測試 Issue 快照

- 建立 Run 時，將 Plan 當時的測試 Issue 複製到 Run。
- Plan 後續更換或移除測試 Issue，不得回寫既有 Run。
- Run 的快照可為空，以相容沒有測試 Issue 的既有 Plan。
- Run 頁面顯示快照內容，不以目前 Plan 值取代歷史值。

### Run 與 Bug Issue

- 一次 Run 可以開立多張 Bug Issue。
- 從 Run 建立的 Bug 必須記錄該 Run 為來源。
- 一張新建 Bug 第一版只記錄一個來源 Run；後續若需要記錄在其他 Run 重現，另擴充 `reproduced_in` 關聯。
- Bug 狀態改變不移除來源關聯。

## 流程

1. 專案成員先建立需求 Issue 與一張測試 Issue。
2. 使用 `tests` 將測試 Issue 關聯到一或多張需求 Issue。
3. 測試人員在 Case 上選擇此 Case 驗證的需求 Issue。
4. 建立或編輯 Plan 時選擇一張測試 Issue。
5. 建立 Run 時保存 Plan 的測試 Issue 快照。
6. 執行失敗時可以從 Run 開立 Bug，並保存來源 Run。
7. 各頁面只顯示使用者同時有權讀取的 Project 與 Workspace 資料。

## 狀態與一致性規則

- Issue 與測試資產必須位於有效 Workspace–Project 關聯範圍。
- 有效的 Case／Plan／Run 追溯存在時，不得解除對應的 Workspace–Project 關聯。
- Workspace 或 Project 停用後保留歷史關聯供查詢，但不得新增或移除關聯。
- Case 停用、Plan 封存、Run 完成或取消都不刪除追溯資料。
- Issue 狀態、類型或處理人改變不自動刪除既有追溯。
- Run 快照不隨來源 Plan 或 Issue 關聯變更。
- 建立操作由唯一鍵防止重複；移除或取代關聯必須使用樂觀版本控制。

## 權限

### Issue 關聯

- 讀取需要兩張 Issue 所屬 Project 的 `issue.read`；第一版兩者必須位於同一 Project。
- 建立需要 `issue.relation.create`。
- 移除需要 `issue.update`，且操作人必須是有效 Project 成員。

### 測試追溯

讀取時，使用者必須同時為有效 Workspace 成員，並具備 Issue 所屬 Project 的 `issue.read`。

建立、取代或移除 Case／Plan 關聯時，使用者必須：

- 是 Workspace 的 `owner` 或 `manager`。
- 對 Issue 所屬 Project 具備 `issue.update`。
- Workspace 與 Project 均為使用中，且兩者存在有效關聯。

建立 Run 時的快照由系統依已授權 Plan 產生。從 Run 建立 Bug 另需 `issue.create`；Bug 來源關聯隨建立流程一併保存。

`tester` 與 `viewer` 第一版只能讀取追溯關聯；系統管理者不因全域角色自動取得 Project 或 Workspace 內部權限。

## 安全與稽核

- 每次查詢與異動都必須重新驗證 Project 與 Workspace 雙邊授權。
- 不信任前端傳入的 Project、Workspace、Issue 類型或關聯方向描述。
- Project Management 擁有 Issue 關聯；Test Management 不直接讀取其 DbContext，而透過明確的應用層合約查詢 Issue。
- 建立、取代及移除至少記錄操作帳號、時間及資源版本。
- API 不得向只具備單邊權限的使用者洩漏另一側名稱、編號、數量或其他識別資訊。

## 驗收條件

- 使用者可建立五種固定 Issue 關聯，並正確看到正向與反向語意。
- `parent_of` 不允許第二個父層、自我關聯或循環。
- `related` 的 A–B 與 B–A 視為同一筆關聯。
- 測試 Issue 可透過 `tests` 關聯多張需求 Issue。
- Case 可關聯多張需求 Issue。
- Plan 最多關聯一張測試 Issue，Run 建立時保存其快照。
- Plan 後續異動不影響既有 Run。
- Run 可保留其建立之多張 Bug 來源關聯。
- 未關聯 Project、不同 Workspace、不同 Project或缺少任一側權限的請求會被拒絕。
- 併發異動會以版本衝突回應，不會靜默覆寫。

## 後續擴充

- Bug 在其他 Run 的重現紀錄。
- Run Item 或 Step 細部證據關聯。
- 關聯圖、需求覆蓋率與未驗證需求報表。
- 可設定的 Issue 關聯類型與跨 Project 關聯。
