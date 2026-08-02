# 使用者與登入資料模型

## 目的

本文件記錄第一個登入垂直切片已採用的 PostgreSQL 資料表。權限定義與專案角色會在對應功能切片加入，不提前建立未使用的資料表。

## `accounts`

保存本機帳號、密碼雜湊、狀態與首次改密碼要求。

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| `id` | `uuid` | 帳號識別。 |
| `username` | `varchar(100)` | 顯示與登入帳號。 |
| `normalized_username` | `varchar(100)` | 唯一、正規化後的查詢值。 |
| `password_hash` | `varchar(500)` | 單向密碼雜湊，不保存明文。 |
| `account_type` | `varchar(50)` | `human` 或 `ai_agent`。 |
| `status` | `varchar(50)` | `active`、`suspended` 或 `disabled`。 |
| `must_change_password` | `boolean` | 是否必須先變更密碼。 |
| `last_login_at` | `timestamp with time zone` | 最後成功登入時間。 |
| `audit_info` | `-` | 依共用 Audit Info 結構保存。 |

## `system_roles`

保存系統層角色定義。登入初始化建立 `System Admin`；帳號管理切片以固定 seed data 建立 `User` 角色。MVP 不提供自訂系統角色或調整角色權限的 UI。

## `account_system_role_mappings`

以複合主鍵保存帳號與系統角色的多對多關係。系統角色不得取代未來的專案 membership 與專案角色判斷。

## `login_sessions`

保存可撤銷的 server-side refresh state。

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| `id` | `uuid` | Session 識別，經保護後寫入 refresh cookie。 |
| `account_id` | `uuid` | 所屬帳號。 |
| `created_at` | `timestamp with time zone` | 建立時間。 |
| `expires_at` | `timestamp with time zone` | 最長有效時間。 |
| `is_persistent` | `boolean` | 是否由使用者選擇 Remember Me。 |
| `revoked_at` | `timestamp with time zone` | 登出或撤銷時間。 |

## `audit_events`

保存登入切片的安全事件，包括初始化、登入成功、登入失敗、密碼變更與登出。事件不保存密碼、cookie、token 或完整敏感 payload。

## 後續範圍

以下資料表已由授權與帳號管理切片加入：

- `permissions`
- `system_role_permissions`

帳號生命週期沿用 `accounts.status`、`login_sessions.revoked_at` 與 `audit_events`，不另增帳號狀態資料表。

以下仍屬後續功能：

- 管理員重設密碼
- 自訂系統角色與系統角色權限管理
