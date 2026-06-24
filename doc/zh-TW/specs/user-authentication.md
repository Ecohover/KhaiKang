# 使用者與登入規格

## 目的

這份文件定義 KhaiKang 第一階段的使用者與登入模型。
初版只做 Web，並使用本機帳號。LDAP、OIDC、SSO 與手機 App 登入都不在 v0.1 範圍內。

## 相關文件

- [文件規範](../../documentation-guidelines.md)
- [架構總覽](../../zh-TW/architecture/overview.md)
- [Roadmap](../roadmap/)
- [英文 README](../../../README.md)
- [繁體中文 README](../../../README.zh-TW.md)

## 範圍

這份規格涵蓋：

- 第一次啟動時的管理員初始化
- 本機使用者登入
- 登出
- authentication ticket 到期
- refresh state 到期
- 密碼政策
- 最小權限邊界
- 帳號狀態管理

## 非目標

這一階段不包含：

- LDAP 整合
- OIDC / SSO 整合
- 手機 App 登入
- 社群登入
- Email 密碼重設
- 多因素驗證
- 委派式管理者建立流程

## 使用者模型

系統至少要支援以下使用者狀態：

- `active`
- `suspended`
- `disabled`

系統至少要支援以下角色：

- `admin`
- `user`

初版可以先採簡單權限模型，但角色檢查的設計要保留未來擴充空間。

## 權限模型

KhaiKang 的授權模型必須區分三件事：

1. 角色
2. 權限字串
3. 授權範圍

### 全域角色

第一階段先支援以下全域角色：

- `admin`
- `user`

其中：

- `admin` 代表系統管理者
- `user` 代表一般登入使用者

### 專案角色

專案內部可以另外定義角色，例如：

- `project_admin`
- `project_member`
- `project_viewer`

專案角色只對指定專案有效，不等於系統管理員權限。

### 權限字串格式

權限字串一律使用 `.` 分隔，例如：

- `user.manage`
- `project.read`
- `project.write`
- `project.manage`
- `testcase.read`
- `testcase.write`

權限字串只描述「可以做什麼」，不描述「在哪裡做」。

### 授權範圍

授權範圍用使用者與資源的指派關係表示，不直接寫進權限字串。

例如：

- 使用者 A 在 Project X 是 `project_admin`
- 使用者 A 在 Project Y 是 `project_member`
- 使用者 B 只在特定專案有存取權

這表示同一個使用者在不同專案可以擁有不同角色與不同權限範圍。

### 授權判斷原則

系統在判斷授權時，應依序考慮：

1. 是否為系統 `admin`
2. 是否屬於目標資源的成員
3. 使用者在該資源上的角色
4. 角色對應的權限字串

### 設計原則

- 權限字串只代表能力，不代表範圍
- 專案範圍使用 membership 或 assignment 表示
- `admin` 與專案角色要分開
- 之後若擴充 workspace scope，也要沿用同樣的設計方式

## 第一次啟動管理員初始化

當系統第一次啟動，且尚未存在管理員帳號時，系統必須進入初始化模式。

在初始化模式下：

- 系統必須建立一組預設管理員帳號，帳號名稱固定為 `admin`
- 系統必須產生一組高強度的隨機初始密碼
- 密碼必須顯示在第一次啟動頁面給操作人員看
- 密碼也可以寫入啟動日誌或初始設定輸出位置
- 初始化完成後，密碼不得再以明文保存
- 管理員第一次成功登入後，必須強制更改初始密碼

初始化狀態會在第一個管理員建立完成後結束。

## 登入流程

### 登入請求

使用者以以下資訊登入：

- 帳號名稱
- 密碼

### 登入驗證

系統必須：

- 驗證本機使用者帳密
- 拒絕停用或停權帳號
- 驗證成功後才建立有效登入狀態

### 登入 session

Web 端使用 cookie-based authentication。

需求如下：

- authentication cookie 必須是 `HttpOnly`
- authentication cookie 必須是 `Secure`
- authentication cookie 必須明確使用 `SameSite=Lax`
- 前端 JavaScript 不得讀取 refresh credential

實作上應支援：

- 短效期的登入狀態
- 較長效期的續期機制

## Session 與 Token 政策

系統必須支援：

- 正常 API 使用的短效期 authentication ticket
- server-side 的登入狀態撤銷
- 登出時讓目前登入狀態失效

v0.1 建議政策：

- authentication ticket 狀態應自動到期
- persistent login 只有在使用者明確選擇 `Remember Me` 時才啟用
- server-side refresh state 必須可撤銷

實際有效時間長度屬於實作細節，但必須可設定。

## 登出

登出必須：

- 清除瀏覽器上的 authentication cookie
- 讓 server-side 的 refresh state 失效
- 登出後目前登入狀態不得再被重用

## 密碼政策

初版密碼政策至少要包含：

- 最小長度限制
- 存入資料庫前要先雜湊
- 不可保存明文密碼

v0.1 可選擇性支援：

- 密碼複雜度檢查
- 第一次登入後強制改密碼

## 權限邊界

第一階段必須支援最小權限模型：

- `admin` 可以建立與管理使用者
- `admin` 可以存取系統層級設定
- `user` 在專案模組完成後可以使用一般專案功能

專案層級與 workspace 層級的細部權限之後可以再擴充，但目前的模型不能阻礙未來做更細粒度的權限檢查。

## 帳號生命週期

系統必須支援以下帳號操作：

- 建立使用者
- 更新使用者資料
- 停權使用者
- 停用使用者
- 恢復使用者

當使用者被停權或停用時：

- 現有 session 必須失效，或在下一次驗證時被拒絕

## 安全要求

- 密碼必須使用現代單向雜湊演算法保存
- authentication cookie 必須使用 `HttpOnly`、`Secure` 與明確的 `SameSite` 設定
- 敏感資訊不得以明文寫入 log
- 登入流程必須適合瀏覽器使用
- 任何會改變狀態的 cookie-authenticated request 都要考慮 CSRF 防護

## 稽核要求

系統必須記錄以下稽核事件：

- 使用者建立
- 使用者更新
- 密碼變更
- 使用者停權
- 使用者停用
- 使用者恢復
- 登入成功
- 登入失敗
- 登出
- 管理員初始化完成

## 驗收條件

- 全新安裝且沒有管理員時，系統會進入初始化模式
- 初始化頁面會顯示 `admin` 帳號與隨機初始密碼
- 第一個管理員可以成功登入
- 第一個管理員第一次登入後必須強制修改初始密碼
- 一般使用者可以用本機帳密登入
- 停權或停用的使用者無法登入
- 登出後目前的 authentication 狀態會失效
- 瀏覽器登入使用 `HttpOnly` cookie

## 未決問題

- 初始密碼要只顯示在 Web 初始化頁面，還是也要寫到 container log
- 第一次登入後強制改密碼要不要做成可設定
