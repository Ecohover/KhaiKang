# 規格索引

## 目的

這份文件是 KhaiKang 功能規格的總導覽頁。
它列出主要產品區塊、子功能，以及目前狀態。

## 狀態說明

- 開發狀態：
  - `Not Started`
  - `In Progress`
  - `Blocked`
- 文件狀態：
  - `Ready`
  - `Draft`
  - `Not Started`

## 第一階段

| 功能區塊 | 子功能 | 開發狀態 | 文件狀態 | 規格 |
| --- | --- | --- | --- | --- |
| 使用者與管理者 | 初始化管理員 | Not Started | Ready | [使用者與登入規格](./user-authentication.md) |
| 使用者與管理者 | 本機使用者登入 | Not Started | Ready | [使用者與登入規格](./user-authentication.md) |
| 使用者與管理者 | JWT / cookie 驗證機制 | Not Started | Ready | [使用者與登入規格](./user-authentication.md) |
| 使用者與管理者 | 權限模型 | Not Started | Draft | [使用者與登入規格](./user-authentication.md) |
| 使用者與管理者 | 密碼政策 | Not Started | Draft | [使用者與登入規格](./user-authentication.md) |
| 使用者與管理者 | 帳號生命週期 | Not Started | Draft | [使用者與登入規格](./user-authentication.md) |
| 專案管理 | 專案基礎模型 | Not Started | Not Started | TBD |
| 專案管理 | Workspace 基礎模型 | Not Started | Not Started | TBD |
| 專案管理 | 專案成員 | Not Started | Not Started | TBD |
| 專案管理 | 專案設定 | Not Started | Not Started | TBD |
| 專案管理 | 任務 / work item 清單 | Not Started | Not Started | TBD |
| 測試管理 | 測試案例基礎模型 | Not Started | Not Started | TBD |
| 測試管理 | 測試套件 | Not Started | Not Started | TBD |
| 測試管理 | 測試計畫 | Not Started | Not Started | TBD |
| 測試管理 | 測試執行 | Not Started | Not Started | TBD |
| 測試管理 | 測試結果 | Not Started | Not Started | TBD |

## 第二階段

| 功能區塊 | 子功能 | 開發狀態 | 文件狀態 | 規格 |
| --- | --- | --- | --- | --- |
| CI 整合 | Jenkins 連線 | Not Started | Not Started | TBD |
| CI 整合 | Job 對應 | Not Started | Not Started | TBD |
| CI 整合 | Build 結果匯入 | Not Started | Not Started | TBD |
| CI 整合 | JUnit XML 匯入 | Not Started | Not Started | TBD |
| 稽核記錄 | Work item 稽核 | Not Started | Not Started | TBD |
| 稽核記錄 | 測試稽核 | Not Started | Not Started | TBD |
| 稽核記錄 | 設定稽核 | Not Started | Not Started | TBD |
| AI 輔助 | AI 提案建立 | Not Started | Not Started | TBD |
| AI 輔助 | 人工審核流程 | Not Started | Not Started | TBD |
| AI 輔助 | AI 產生測試案例草稿 | Not Started | Not Started | TBD |

## 備註

- 狀態更新要簡短且具體。
- 每個主要功能區塊都應該連到對應的規格文件。
- 當某份規格變大時，應切成較小的規格文件，並同步更新這份索引。
