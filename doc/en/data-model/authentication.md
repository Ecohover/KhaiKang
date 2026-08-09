# Authentication Data Model

Status: synchronized with `InitialIdentity` on 2026-08-09. Traditional Chinese counterpart: [登入與使用者資料模型](../../zh-TW/03-data-model/04-authentication-data-model.md).

| Table | Responsibility |
| --- | --- |
| `accounts` | Local username, password hash, account type/status, one-time-password flag, last login, and audit metadata. Username normalization is unique. |
| `system_roles` | Global Admin/User role definitions with unique normalized names. |
| `account_system_role_mappings` | Many-to-many account/global-role membership. |
| `permissions` | Unique capability codes and scope type. |
| `system_role_permissions` | Unique global-role/permission mappings. |
| `login_sessions` | Revocable refresh-session state with expiry and persistence flag. |
| `audit_events` | Identity and authentication event records. |

Passwords are stored only as hashes. Refresh credentials are protected in cookies; the database stores session identity and lifecycle state, not plaintext credentials. Project and Workspace roles belong to their modules rather than these global-role tables.
