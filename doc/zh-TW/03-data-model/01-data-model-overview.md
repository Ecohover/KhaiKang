# 資料模型總覽

## 目的

這份文件用來說明 KhaiKang 中文資料模型文件的組織方式。

資料模型文件與功能規格分開維護，目的是讓：

- 功能規格專注在業務規則與流程
- 資料模型文件專注在資料表責任、欄位方向與關聯

## 目前文件

- [專案管理資料模型](./02-project-management-data-model.md)
- [Audit Metadata 欄位表](./03-audit-metadata-fields.md)

## 使用原則

- 先閱讀功能規格，再閱讀對應資料模型
- 資料模型文件可以討論欄位與關聯
- 階段規劃與優先順序不放在資料模型文件中
- 若語詞有歧義，先回到 [語詞總表](../01-overview/04-domain-glossary.md)

## 欄位型別規範

目前為了維持一致性，字串欄位先統一優先使用以下幾種長度：

- `varchar(20)`：極短代碼、前綴、受控短識別
- `varchar(50)`：狀態、類型、短代碼
- `varchar(100)`：一般代號、帳號、短名稱
- `varchar(200)`：顯示名稱
- `text`：長說明

其他常用型別規範：

- `uuid`：Entity 主鍵與主要識別欄位
- `boolean`：布林值
- `integer`：排序值或整數型輔助欄位
- `bigint`：檔案大小或較大整數數值
- `timestamp with time zone`：時間欄位

這份規範是目前版本的預設原則。
若特定欄位有明確需求，再個別例外說明。

## 目前範圍

目前資料模型整理重點放在：

- `accounts`
- `system_roles`
- `permissions`
- `account_system_role_mappings`
- `system_role_permissions`
- `projects`
- `project_roles`
- `project_members`
- `project_member_roles`
- `project_role_permissions`
- `issue_types`
- `issue_statuses`
- `issue_priorities`
- `issues`
- `issue_relation_types`
- `issue_comments`
- `issue_attachments`
- `issue_relations`

另外：

- `ai_agents` 先保留設計位置
- `audit log` 完整策略另行討論
- `tags / watchers / transitions` 暫不納入目前 MVP 核心表
