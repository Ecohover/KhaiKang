# 專案管理資料模型

## 目的

這份文件整理 KhaiKang 專案管理模組目前的資料模型方向。

這裡只處理資料責任、資料表關聯與欄位方向，不重複描述功能規則。
功能定義請參考 [專案管理規格](../02-specs/02-project-management.md)。

## 設計原則

- 不用程式碼 `enum` 鎖死 Issue 類型、狀態與優先級
- 先用系統預設資料搭配 seed 初始化
- 角色、類型、狀態與優先級都保留未來自訂空間
- 先使用單一 PostgreSQL database，不拆多資料庫
- 以資料表關聯表示範圍與角色，不把範圍寫進權限字串
- 主檔 audit metadata 採一致欄位語意，詳細欄位定義另見 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)

## 核心資料表

目前建議至少包含以下資料表：

- `accounts`
- `system_roles`
- `account_system_role_mappings`
- `projects`
- `project_roles`
- `project_members`
- `project_member_roles`
- `issue_types`
- `issue_statuses`
- `issue_priorities`
- `issues`

以下資料表屬於延伸資料表，目前不列入 MVP 核心必做範圍，但先保留資料模型方向：

- `issue_comments`
- `issue_tags`
- `issue_relations`
- `issue_attachments`
- `issue_transitions`

這些表可於後續需求明確後再補。

## AI 預留資料表

以下資料表先保留設計位置，但不在目前版本定義細節欄位：

- `ai_agents`

`ai_agents` 的定位如下：

- 這是 `accounts` 的延伸設定表
- 只有 `account_type = ai_agent` 的帳號才需要對應資料
- AI 是否加入某個專案，仍然透過 `project_members` 與 `project_member_roles` 處理
- 目前先只記錄這張表未來會承載 AI 設定，不先定欄位名稱

預計承載的設定方向包含：

- AI 身分識別資料
- 模型與 provider 設定
- prompt 或 instruction 類設定
- 是否允許自動執行
- 後續整合 queue / webhook / agent runtime 的設定

## 資料表責任

---

### accounts

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `accounts` |
| 說明 | 系統中的共用身份主體。 |
| PK | `id` |
| FK | 無 |
| 備註 | 人類帳號與 AI 帳號共用主表，需支援 `human` 與 `ai_agent` 類型區分；AI 額外設定另外掛在 `ai_agents`。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 帳號主鍵（`account`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `account_type` | 帳號類型（`human_account` / `ai_agent`） | `varchar(50)` | Y | N | 用來區分人類帳號與 AI 帳號。 |
| `username` | 登入帳號 | `varchar(100)` | Y | Y | 本機登入使用。 |
| `email` | 電子郵件 | `varchar(320)` | Y | Y | 可作為聯絡與識別用途。 |
| `display_name` | 顯示名稱 | `varchar(200)` | Y | N | UI 顯示用名稱。 |
| `first_name` | 名 | `varchar(100)` | N | N | 可選的人類姓名欄位。 |
| `last_name` | 姓 | `varchar(100)` | N | N | 可選的人類姓名欄位。 |
| `password_hash` | 密碼雜湊結果 | `text` | Y | N | 只保存安全雜湊結果，不保存明文密碼。 |
| `status` | 帳號狀態 | `varchar(50)` | Y | N | 目前預計支援 `active`、`suspended`、`disabled`。 |
| `must_change_password` | 是否要求使用者在下次登入後修改密碼 | `boolean` | Y | N | 初始帳號、管理員重設密碼或發放暫時密碼後可使用。 |
| `last_login_at` | 最後登入時間 | `timestamp with time zone` | N | N | 可為空。 |
| `is_builtin` | 是否為系統內建帳號 | `boolean` | Y | N | 例如初始化 `admin`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 補充規則

- `id` 一律使用 UUID。
- `username` 與 `email` 建議為唯一欄位。
- `display_name` 為必要顯示欄位，不建議省略。
- `password_hash` 應使用包含 salt 的現代密碼雜湊機制保存。
- `first_name` 與 `last_name` 先保留，但實際顯示以 `display_name` 為主。
- AI 帳號也需要一筆 `accounts`，但其 AI 專屬設定不直接放在這張表。

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 正常可登入與可使用狀態。 | 預設狀態。 |
| `suspended` | 暫時停權。 | 不可登入。 |
| `disabled` | 停用狀態。 | 不可登入，通常代表帳號不再使用。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`account_type`、`username`、`email`、`display_name` |
| 人類姓名 | `first_name`、`last_name` |
| 驗證資訊 | `password_hash`、`must_change_password` |
| 狀態資訊 | `status`、`last_login_at`、`is_builtin` |
| 系統欄位 | `audit_info` |

#### 初始化資料規則

| 項目 | 內容 |
| --- | --- |
| 建立時機 | 系統第一次啟動且尚無管理員帳號時。 |
| 預設帳號 | `admin` |
| 預設角色 | `system_admin` |
| 初始密碼 | 系統隨機產生。 |
| 密碼取得方式 | 由系統輸出到指定檔案供安裝者查詢。 |
| 首次登入 | 必須修改密碼。 |

#### 共通欄位說明

| 標記 | 說明 |
| --- | --- |
| `Audit Metadata` | 表示此欄位屬於共通 audit metadata，詳細定義請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

---

### system_roles

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `system_roles` |
| 說明 | 保存系統層角色定義。 |
| PK | `id` |
| FK | 無 |
| 備註 | 系統層角色與專案層角色分開管理，避免權限邊界混用。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 系統角色主鍵（`system_role`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 系統角色代號 | `varchar(100)` | Y | Y | 穩定識別碼，建議全系統唯一。 |
| `name` | 系統角色名稱 | `varchar(200)` | Y | N | 顯示名稱。 |
| `description` | 系統角色說明 | `text` | N | N | 補充角色用途與範圍。 |
| `is_system` | 是否為系統預設角色 | `boolean` | Y | N | 目前預設角色由系統 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新指派。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- |
| `system_admin` | `System Admin` | 系統管理者，管理整個平台層級設定與帳號。 | Y | Y | 1 | 第一版最小必要角色。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應為唯一欄位。
- 目前建議只先建立 `system_admin`，避免一開始把全域角色設計得太重。
- 一般登入使用者不一定需要對應一個全域 `system_user` 角色，可先以是否具備專案 membership 來判斷可用範圍。

---

### account_system_role_mappings

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `account_system_role_mappings` |
| 說明 | 保存帳號與系統角色的對應關係。 |
| PK | `id` |
| FK | `account_id -> accounts.id`, `system_role_id -> system_roles.id` |
| 備註 | 這是帳號與系統角色的 mapping table，用來表示誰被指派了哪些系統角色。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 帳號系統角色對應主鍵 | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `account_id` | 帳號識別 | `uuid` | Y | N | 對應 `accounts.id`。 |
| `system_role_id` | 系統角色識別 | `uuid` | Y | N | 對應 `system_roles.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `account_id`、`system_role_id` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 同一個 `account_id` 可以對應多個 `system_role_id`。
- 同一個 `system_role_id` 也可以被多個 `account_id` 使用。
- 應限制同一組 `account_id + system_role_id` 不得重複。
- 系統層角色不應與專案層角色混用。

#### Index 建議

- 建立 `idx_account_system_role_mappings_account_id` 於 `account_id`。
- 建立 `idx_account_system_role_mappings_system_role_id` 於 `system_role_id`。

#### 唯一約束建議

- 建立 unique constraint `uq_account_system_role_mappings_account_role` 於 `account_id + system_role_id`。

---

### projects

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `projects` |
| 說明 | 保存專案基本資料。 |
| PK | `id` |
| FK | `created_by_account_id -> accounts.id` |
| 備註 | 專案是系統中的主要工作容器，第一階段不引入 workspace。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 專案主鍵（`project`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 專案代號 | `varchar(100)` | Y | Y | 穩定識別碼，同時作為 Issue 編號前綴。 |
| `name` | 專案名稱 | `varchar(200)` | Y | N | 完整顯示名稱。 |
| `description` | 專案說明 | `text` | N | N | 補充專案用途與範圍。 |
| `status` | 專案狀態 | `varchar(50)` | Y | N | 目前預計支援 `active`、`inactive`。 |
| `created_by_account_id` | 建立者帳號識別 | `uuid` | Y | N | 對應 `accounts.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 專案正常啟用中。 | 預設狀態。 |
| `inactive` | 專案已停用。 | 不表示資料被刪除。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `status` |
| 關聯欄位 | `created_by_account_id` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應為唯一欄位。
- `code` 同時作為專案穩定識別碼與 Issue 編號前綴。
- `created_by_account_id` 用來記錄專案建立者，但不直接等同目前專案 `owner`。

#### Index 建議

- 建立 `idx_projects_created_by_account_id` 於 `created_by_account_id`。

#### 唯一約束建議

- 建立 unique constraint `uq_projects_code` 於 `code`。

#### 建立交易規則

- 建立專案時，應在同一個 transaction 內完成 `projects`、`project_members`、`project_member_roles` 的初始化。
- 專案建立者應同時建立一筆有效的 `project_members`。
- 專案建立者應至少被指派一個專案角色；目前預設建議為 `owner`。
- 若其中任一步失敗，整個專案建立流程應 rollback，避免產生無 owner 的專案。

---

### project_roles

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `project_roles` |
| 說明 | 保存專案角色定義。 |
| PK | `id` |
| FK | 無 |
| 備註 | 這是專案層角色主資料，與系統層角色分開管理。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 專案角色主鍵（`project_role`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 專案角色代號 | `varchar(100)` | Y | Y | 穩定識別碼，建議全系統唯一。 |
| `name` | 專案角色名稱 | `varchar(200)` | Y | N | 顯示名稱。 |
| `description` | 專案角色說明 | `text` | N | N | 補充角色用途與範圍。 |
| `is_system` | 是否為系統預設角色 | `boolean` | Y | N | 目前預設角色由系統 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新指派。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- |
| `owner` | `Owner` | 專案擁有者，專案層級最高權限角色。 | Y | Y | 1 | 僅在該專案內有效。 |
| `manager` | `Manager` | 專案管理者，協助管理成員與日常專案運作。 | Y | Y | 2 | 不等於系統管理者。 |
| `contributor` | `Contributor` | 參與者，主要負責建立、更新與推進工作項目。 | Y | Y | 3 | 可調整處理人與狀態。 |
| `reviewer` | `Reviewer` | 審查者，協助檢視、確認與回饋工作項目。 | Y | Y | 4 | 可協助更新 Issue。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應為唯一欄位。
- 角色先由系統 seed 建立。
- `is_system` 用於標示系統預設角色。
- 之後若要開放自訂角色，可直接沿用此表。

---

### project_members

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `project_members` |
| 說明 | 保存帳號與專案的成員關係。 |
| PK | `id` |
| FK | `project_id -> projects.id`, `account_id -> accounts.id` |
| 備註 | 這是 project 與 account 的 membership mapping，但目前先保留獨立 `id`。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 專案成員主鍵（`project_member`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `project_id` | 專案識別 | `uuid` | Y | N | 對應 `projects.id`。 |
| `account_id` | 帳號識別 | `uuid` | Y | N | 對應 `accounts.id`。 |
| `status` | 專案成員狀態 | `varchar(20)` | Y | N | 目前預計支援 `active`、`removed`。 |
| `joined_at` | 加入時間 | `timestamp with time zone` | Y | N | 記錄加入專案的時間。 |
| `removed_at` | 移除時間 | `timestamp with time zone` | N | N | 被移除專案時記錄，可為空。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 目前仍為有效專案成員。 | 預設狀態。 |
| `removed` | 已被移出專案。 | 不表示帳號被刪除。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `project_id`、`account_id` |
| 狀態資訊 | `status`、`joined_at`、`removed_at` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `account_id` 對應使用者 / AI 帳號識別。
- 移除成員時，建議保留紀錄而不是硬刪除。
- 同一帳號在同一專案下只能有一筆有效 membership。
- 若 `status = removed`，則 `removed_at` 應有值。

#### Index 建議

- 建立 `idx_project_members_project_id` 於 `project_id`。
- 建立 `idx_project_members_account_id` 於 `account_id`。
- 建立 `idx_project_members_project_status` 於 `project_id + status`。

#### 唯一約束建議

- 建立 partial unique index `uq_project_members_active_member` 於 `project_id + account_id`，條件為 `status = 'active'`。

---

### project_member_roles

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `project_member_roles` |
| 說明 | 保存專案成員與專案角色的對應關係。 |
| PK | `id` |
| FK | `project_member_id -> project_members.id`, `project_role_id -> project_roles.id` |
| 備註 | 這是專案成員與專案角色的 mapping table，用來表示某個成員在專案中被指派哪些角色。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 專案成員角色對應主鍵 | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `project_member_id` | 專案成員識別 | `uuid` | Y | N | 對應 `project_members.id`。 |
| `project_role_id` | 專案角色識別 | `uuid` | Y | N | 對應 `project_roles.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `project_member_id`、`project_role_id` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 人類使用者可以在同一專案有多個角色，所以需要獨立關聯表。
- 即使畫面先做簡化，資料模型也保留多角色能力。
- 若要限制 AI 帳號只能單角色，應在業務規則處理，不必改表。
- 應限制同一組 `project_member_id + project_role_id` 不得重複。

#### Index 建議

- 建立 `idx_project_member_roles_project_member_id` 於 `project_member_id`。
- 建立 `idx_project_member_roles_project_role_id` 於 `project_role_id`。

#### 唯一約束建議

- 建立 unique constraint `uq_project_member_roles_member_role` 於 `project_member_id + project_role_id`。

---

### issue_types

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_types` |
| 說明 | 保存 Issue 類型主資料。 |
| PK | `id` |
| FK | 無 |
| 備註 | 目前先作為全域主資料，所有專案共用；後續若要限制特定專案可用類型，再另外用 mapping table 處理。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題類型主鍵（`issue_type`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 議題類型代號 | `varchar(50)` | Y | N | 穩定識別碼，系統判斷應使用 `code`。 |
| `name` | 議題類型名稱 | `varchar(200)` | Y | N | 預設建議存英文名稱。 |
| `description` | 議題類型說明 | `text` | N | N | 補充類型用途與範圍。 |
| `is_system` | 是否為系統預設資料 | `boolean` | Y | N | 系統預設資料由 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新 Issue 使用。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- |
| `story` | `Story` | 用來表示具體需求或使用者價值。 | Y | Y | 1 | 適合一般需求追蹤。 |
| `task` | `Task` | 用來表示一般執行工作項目。 | Y | Y | 2 | 適合非需求型工作。 |
| `bug` | `Bug` | 用來表示缺陷或異常問題。 | Y | Y | 3 | 適合缺陷追蹤。 |
| `spike` | `Spike` | 用來表示研究、驗證或技術探索工作。 | Y | Y | 4 | 適合不確定性高的前期工作。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應在同一範圍內唯一。
- 目前所有專案共用同一份 Issue Type 主資料。
- 後續若要限制特定專案可用哪些類型，建議另外建立 mapping table。
- `name` 目前建議先存英文名稱，其他語系後續透過 translation table 擴充。

---

### issue_statuses

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_statuses` |
| 說明 | 保存 Issue 狀態主資料。 |
| PK | `id` |
| FK | 無 |
| 備註 | 目前先作為全域主資料，所有專案共用；後續若要限制特定專案可用狀態，再另外用 mapping table 處理。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題狀態主鍵（`issue_status`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 議題狀態代號 | `varchar(50)` | Y | N | 穩定識別碼，系統判斷應使用 `code`。 |
| `name` | 議題狀態名稱 | `varchar(200)` | Y | N | 預設建議存英文名稱。 |
| `description` | 議題狀態說明 | `text` | N | N | 補充狀態用途與範圍。 |
| `category` | 狀態分類 | `varchar(20)` | Y | N | 用來區分流程大類，例如 `todo`、`doing`、`done`。 |
| `is_system` | 是否為系統預設資料 | `boolean` | Y | N | 系統預設資料由 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新 Issue 使用。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | category | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `created` | `Created` | 表示 Issue 已建立，尚未正式進入處理。 | `todo` | Y | Y | 1 | 預設初始狀態。 |
| `in_progress` | `In Progress` | 表示 Issue 目前正在處理中。 | `doing` | Y | Y | 2 | 可自由切入或切出。 |
| `verifying` | `Verifying` | 表示 Issue 目前正在驗證中。 | `doing` | Y | Y | 3 | 驗證未完成前仍不視為結束。 |
| `completed` | `Completed` | 表示 Issue 已完成。 | `done` | Y | Y | 4 | 目前視為完成狀態。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `category`、`is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應在同一範圍內唯一。
- 目前所有專案共用同一份 Issue Status 主資料。
- 後續若要限制特定專案可用哪些狀態，建議另外建立 mapping table。
- `name` 目前建議先存英文名稱，其他語系後續透過 translation table 擴充。
- 雖然目前自由流轉，但先保留 `category` 欄位，後續較容易加規則。

---

### issue_priorities

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_priorities` |
| 說明 | 保存 Issue 優先級主資料。 |
| PK | `id` |
| FK | 無 |
| 備註 | 目前先作為全域主資料，所有專案共用；後續若要限制特定專案可用優先級，再另外用 mapping table 處理。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題優先級主鍵（`issue_priority`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 議題優先級代號 | `varchar(50)` | Y | N | 穩定識別碼，系統判斷應使用 `code`。 |
| `name` | 議題優先級名稱 | `varchar(200)` | Y | N | 預設建議存英文名稱。 |
| `description` | 議題優先級說明 | `text` | N | N | 補充優先級用途與範圍。 |
| `is_system` | 是否為系統預設資料 | `boolean` | Y | N | 系統預設資料由 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新 Issue 使用。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- |
| `low` | `Low` | 低優先級，通常可排在較後面處理。 | Y | Y | 1 | 適合影響較低的工作。 |
| `medium` | `Medium` | 中優先級，一般預設工作層級。 | Y | Y | 2 | 可作為一般預設值。 |
| `high` | `High` | 高優先級，需要較快處理。 | Y | Y | 3 | 適合重要工作或問題。 |
| `critical` | `Critical` | 關鍵優先級，需要立即關注。 | Y | Y | 4 | 適合重大異常或阻塞問題。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應在同一範圍內唯一。
- 目前所有專案共用同一份 Issue Priority 主資料。
- 後續若要限制特定專案可用哪些優先級，建議另外建立 mapping table。
- `name` 目前建議先存英文名稱，其他語系後續透過 translation table 擴充。

---

### issues

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issues` |
| 說明 | 保存專案中的 Issue 主資料。 |
| PK | `id` |
| FK | `project_id -> projects.id`, `issue_type_id -> issue_types.id`, `issue_status_id -> issue_statuses.id`, `issue_priority_id -> issue_priorities.id`, `reporter_account_id -> accounts.id`, `assignee_account_id -> accounts.id` |
| 備註 | `issue_no` 為專案內流水號，對外顯示時可搭配 `projects.code` 組成易讀編號。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題主鍵（`issue`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `project_id` | 專案識別 | `uuid` | Y | N | 對應 `projects.id`。 |
| `issue_no` | 專案內議題流水號 | `integer` | Y | N | 建議由系統依專案遞增產生。 |
| `title` | 議題標題 | `varchar(200)` | Y | N | 對人可讀的主要標題。 |
| `description` | 議題內容說明 | `text` | N | N | 保存需求、缺陷或工作說明。 |
| `user_story` | 使用者故事 | `text` | N | N | 主要提供需求型 Issue 使用，可記錄 As a / I want / So that 結構。 |
| `definition_of_done` | 完成定義 | `text` | N | N | 記錄此 Issue 被視為完成前應達成的條件。 |
| `issue_type_id` | 議題類型識別 | `uuid` | Y | N | 對應 `issue_types.id`。 |
| `issue_status_id` | 議題狀態識別 | `uuid` | Y | N | 對應 `issue_statuses.id`。 |
| `issue_priority_id` | 議題優先級識別 | `uuid` | Y | N | 對應 `issue_priorities.id`。 |
| `reporter_account_id` | 建立人帳號識別 | `uuid` | Y | N | 對應 `accounts.id`。 |
| `assignee_account_id` | 指派處理人帳號識別 | `uuid` | N | N | 可為空，表示目前未指派。 |
| `completion_summary` | 完成說明 | `text` | N | N | 記錄實際完成內容、處理結果或交付摘要。 |
| `completed_at` | 完成時間 | `timestamp with time zone` | N | N | 狀態進入完成態時可由系統帶入。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`project_id`、`issue_no` |
| 基本資料 | `title`、`description`、`user_story`、`definition_of_done` |
| 流程欄位 | `issue_type_id`、`issue_status_id`、`issue_priority_id` |
| 人員欄位 | `reporter_account_id`、`assignee_account_id` |
| 狀態資訊 | `completion_summary`、`completed_at` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `issue_no` 應在同一 `project_id` 範圍內唯一。
- Issue 建立時，預設狀態應為 `created`。
- Issue 建立時，預設可為未指派，因此 `assignee_account_id` 可為空。
- `user_story` 建議主要用於 `story` 類型，但不強制限制其他類型不可填寫。
- `definition_of_done` 用來記錄完成前應達成的條件，不等同於實際完成結果。
- `reporter_account_id` 應記錄實際開立此 Issue 的帳號，可為人類或 AI 帳號。
- `assignee_account_id` 若有值，應由業務規則驗證該帳號必須是同一專案中的有效 `project_member`。
- `completion_summary` 用來記錄實際完成結果，建議在狀態切換為完成時補上。
- 對外顯示編號時，建議使用 `projects.code + "-" + issue_no` 組合，例如 `CORE-101`。
- `completed_at` 是業務狀態欄位，不屬於共用 audit metadata，因此保留在主表。
- 目前先不把 Comment、Tag、Relation、Attachment 併進主表，後續需要時再拆成獨立資料表。

#### Index 建議

- 建立 `idx_issues_project_id` 於 `project_id`。
- 建立 `idx_issues_issue_type_id` 於 `issue_type_id`。
- 建立 `idx_issues_issue_status_id` 於 `issue_status_id`。
- 建立 `idx_issues_issue_priority_id` 於 `issue_priority_id`。
- 建立 `idx_issues_reporter_account_id` 於 `reporter_account_id`。
- 建立 `idx_issues_assignee_account_id` 於 `assignee_account_id`。
- 建立 `idx_issues_project_status` 於 `project_id + issue_status_id`。
- 建立 `idx_issues_project_assignee` 於 `project_id + assignee_account_id`。
- 若後續 audit metadata 會獨立展開 `updated_at`，建議再建立 `idx_issues_project_updated_at` 於 `project_id + updated_at desc`。

#### 唯一約束建議

- 建立 unique constraint `uq_issues_project_issue_no` 於 `project_id + issue_no`。

---

#### Issue Mapping 設計原則

- `issue_type_id`、`issue_status_id`、`issue_priority_id`、`reporter_account_id`、`assignee_account_id` 都屬於單一外鍵關聯，不應另外拆成 mapping table。
- 只有在一張 Issue 同時需要對應多筆資料時，才建議使用 mapping table。
- 目前不建議建立泛用 `issue_mappings`，因為會讓資料語意模糊，後續也難做資料約束與索引優化。

目前較合理的 Issue 關聯拆表方向如下：

| 資料表 | 類型 | 是否建議目前實作 | 說明 |
| --- | --- | --- | --- |
| `issue_comments` | Entity | N | 保存 Issue 留言內容，屬於獨立主體，不是單純 mapping。 |
| `issue_relations` | Mapping | N | 保存 Issue 與 Issue 的關聯，例如 blocked by / relates to。 |
| `issue_tags` | Mapping | N | 若未來支援標籤，建議再搭配 `tags` 主表使用。 |
| `issue_watchers` | Mapping | N | 若未來支援關注者通知，再建立此表。 |
| `issue_attachments` | Entity | N | 保存附件中繼資料，屬於獨立主體，不是單純 mapping。 |

---

### issue_comments

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_comments` |
| 說明 | 保存 Issue 留言內容。 |
| PK | `id` |
| FK | `issue_id -> issues.id`, `author_account_id -> accounts.id` |
| 備註 | 這是獨立 Entity，用來記錄 Issue 底下的人類或 AI 留言。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題留言主鍵（`issue_comment`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `issue_id` | 議題識別 | `uuid` | Y | N | 對應 `issues.id`。 |
| `author_account_id` | 留言作者帳號識別 | `uuid` | Y | N | 對應 `accounts.id`，可為人類或 AI 帳號。 |
| `content` | 留言內容 | `text` | Y | N | 保存留言本文。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`issue_id` |
| 人員欄位 | `author_account_id` |
| 內容欄位 | `content` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `author_account_id` 應記錄實際留言者，可為人類或 AI 帳號。
- `content` 建議保存純文字或標準化富文字格式，避免一開始就綁定前端特定編輯器結構。
- MVP 階段可先不支援巢狀回覆；若後續需要，再增加 `parent_comment_id`。
- 不建議硬刪除留言；若後續需要刪除功能，建議以業務狀態欄位或刪除標記處理。

#### Index 建議

- 建立 `idx_issue_comments_issue_id` 於 `issue_id`。
- 建立 `idx_issue_comments_author_account_id` 於 `author_account_id`。
- 若後續 audit metadata 會獨立展開 `created_at`，建議再建立 `idx_issue_comments_issue_created_at` 於 `issue_id + created_at`。

---

### issue_relations

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_relations` |
| 說明 | 保存 Issue 與 Issue 之間的關聯。 |
| PK | `id` |
| FK | `source_issue_id -> issues.id`, `target_issue_id -> issues.id`, `issue_relation_type_id -> issue_relation_types.id` |
| 備註 | 這是 mapping table，用來表示 Issue 之間的依賴或關聯語意。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 議題關聯主鍵（`issue_relation`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `source_issue_id` | 起點議題識別 | `uuid` | Y | N | 對應 `issues.id`。 |
| `target_issue_id` | 目標議題識別 | `uuid` | Y | N | 對應 `issues.id`。 |
| `issue_relation_type_id` | 關聯類型識別 | `uuid` | Y | N | 對應 `issue_relation_types.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `source_issue_id`、`target_issue_id` |
| 類型欄位 | `issue_relation_type_id` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 不可建立 `source_issue_id = target_issue_id` 的自我關聯。
- 應限制同一組 `source_issue_id + target_issue_id + issue_relation_type_id` 不得重複。
- 主從方向由 `source_issue_id` 與 `target_issue_id` 決定，不能只看關聯名稱。
- 對等語意的關聯類型仍建議只保存一筆，避免雙向重複資料。
- 目前建議限制為同一專案內的 Issue 關聯，不先支援跨專案關聯。

#### Index 建議

- 建立 `idx_issue_relations_source_issue_id` 於 `source_issue_id`。
- 建立 `idx_issue_relations_target_issue_id` 於 `target_issue_id`。
- 建立 `idx_issue_relations_issue_relation_type_id` 於 `issue_relation_type_id`。

#### 唯一約束建議

- 建立 unique constraint `uq_issue_relations_source_target_type` 於 `source_issue_id + target_issue_id + issue_relation_type_id`。

---

### issue_relation_types

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `issue_relation_types` |
| 說明 | 保存 Issue 關係類型主資料。 |
| PK | `id` |
| FK | 無 |
| 備註 | 用來定義 Issue 與 Issue 之間的關聯語意，以及是否具有主從方向。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 關聯類型主鍵（`issue_relation_type`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `code` | 關聯類型代號 | `varchar(50)` | Y | N | 穩定識別碼，系統判斷應使用 `code`。 |
| `name` | 關聯類型名稱 | `varchar(200)` | Y | N | 預設建議存英文名稱。 |
| `description` | 關聯類型說明 | `text` | N | N | 補充關聯語意與使用時機。 |
| `is_directional` | 是否具有方向性 | `boolean` | Y | N | `Y` 表示需明確區分 source 與 target。 |
| `is_system` | 是否為系統預設資料 | `boolean` | Y | N | 系統預設資料由 seed 建立。 |
| `is_active` | 是否啟用 | `boolean` | Y | N | 停用後不可再被新關聯使用。 |
| `sort_order` | 排序值 | `integer` | Y | N | 用於顯示排序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Metadata 欄位表](./03-audit-metadata-fields.md)。 |

#### 建議預設資料

| code | name | description | is_directional | is_system | is_active | sort_order | 備註 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `relates_to` | `Relates To` | 表示兩張 Issue 有一般關聯。 | N | Y | Y | 1 | 屬於對等語意。 |
| `depends_on` | `Depends On` | 表示起點 Issue 依賴目標 Issue。 | Y | Y | Y | 2 | `source_issue_id` 依賴 `target_issue_id`。 |
| `blocks` | `Blocks` | 表示起點 Issue 阻塞目標 Issue。 | Y | Y | Y | 3 | `source_issue_id` 阻塞 `target_issue_id`。 |
| `duplicates` | `Duplicates` | 表示起點 Issue 與目標 Issue 內容重複。 | N | Y | Y | 4 | 建議保留單向一筆即可。 |
| `caused_by` | `Caused By` | 表示起點 Issue 的發生原因來自目標 Issue。 | Y | Y | Y | 5 | 適合問題追因。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id`、`code` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `is_directional`、`is_system`、`is_active`、`sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `code` 應在同一範圍內唯一。
- `name` 目前建議先存英文名稱，其他語系後續透過 translation table 擴充。
- 具有方向性的關聯類型，必須明確解釋 `source_issue_id` 與 `target_issue_id` 的語意。
- 對等語意的關聯類型，UI 顯示可不分正反向，但資料層仍建議只保存一筆。

---

## 主要關聯

- `projects` 1 對多 `project_members`
- `project_roles` 多對多 `project_members`，透過 `project_member_roles`
- `projects` 1 對多 `issues`
- `issue_types` 1 對多 `issues`
- `issue_statuses` 1 對多 `issues`
- `issue_priorities` 1 對多 `issues`

## 資料約束建議

- `projects.code` 應唯一
- `project_roles.code` 在系統預設角色範圍內應唯一
- `project_members` 應限制同一 `project_id + account_id` 不得重複有效 membership
- `project_member_roles` 應限制同一 `project_member_id + project_role_id` 不得重複
- `issue_types`、`issue_statuses`、`issue_priorities` 應限制同一範圍下的 `code` 唯一
- `issues.issue_no` 應在同一 `project_id` 下唯一

## FK 與 Index 原則

- 每個 FK 欄位原則上都應建立對應 index，避免 join、刪除檢查與更新檢查成本過高。
- 每個 mapping table 除了個別 FK index 外，還應建立對應的 composite unique constraint。
- 不能只依賴 FK index；實際清單查詢路徑仍應補上查詢用 composite index。
- partial unique index 可用於處理「同範圍內只能有一筆有效資料」這類軟刪除 / 狀態型規則。
