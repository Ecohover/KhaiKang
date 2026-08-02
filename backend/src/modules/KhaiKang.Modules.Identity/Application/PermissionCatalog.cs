namespace KhaiKang.Modules.Identity.Application;

public static class PermissionCatalog
{
    public const string ClaimType = "permission";

    public static readonly PermissionDefinition[] All =
    [
        new("2d19d818-0214-4be2-b080-621e0cf0c526", "account.read", "Account Read", "查看使用者帳號資料。", "system"),
        new("da7ff46b-3349-4a09-a4e9-5d60542bc2b2", "account.create", "Account Create", "建立本機使用者帳號。", "system"),
        new("3cda75d2-995b-4e94-bdab-9307429352c5", "account.update", "Account Update", "修改使用者基本資料或重設密碼。", "system"),
        new("ae2a3092-bd64-42bc-88f1-708e7abdac8a", "account.suspend", "Account Suspend", "停權、停用或恢復使用者帳號。", "system"),
        new("297e6b10-207a-47f5-b604-47c40f1e6bc1", "project.create", "Project Create", "建立新專案。", "system"),
        new("1a8f54ab-c19f-4356-a40b-fe4fcbeda0fb", "project.deactivate", "Project Deactivate", "停用或恢復專案。", "system"),
        new("1e42b09d-9839-4e8c-951c-38c941f9e4ca", "project.read", "Project Read", "查看專案基本內容。", "project"),
        new("f622c88d-d0f0-40ed-86c0-bba2c3ff44c9", "project.update", "Project Update", "修改專案基本資料。", "project"),
        new("23cffaf6-9d01-4116-9a79-a4970bc01eae", "project.member.add", "Project Member Add", "新增專案成員。", "project"),
        new("bb8afb36-8753-4383-bce7-83065a92c0d3", "project.member.remove", "Project Member Remove", "移除專案成員。", "project"),
        new("0cc976a0-4f5a-4ce4-9309-6f4476522aa6", "project.role.assign", "Project Role Assign", "指派或調整專案角色。", "project"),
        new("3c2bdd52-7445-4a16-9750-1b28d97bf109", "issue.create", "Issue Create", "建立 Issue。", "project"),
        new("3ed83136-9532-4e6e-8adb-b73ae9863a2c", "issue.read", "Issue Read", "查看 Issue。", "project"),
        new("a5fa36de-f8eb-491a-981c-f2f17244fa2b", "issue.update", "Issue Update", "編輯 Issue。", "project"),
        new("084b565e-e59c-4b48-9a4d-2de2a58e0a9d", "issue.status.change", "Issue Status Change", "變更 Issue 狀態。", "project"),
        new("60098112-f880-40d8-97d6-bed5784f83a0", "issue.assignee.change", "Issue Assignee Change", "變更 Issue 處理人。", "project"),
        new("2f36b9f4-b7f1-4cd2-af25-46393d560b13", "issue.comment.create", "Issue Comment Create", "新增 Issue 留言。", "project"),
        new("081eced1-97b4-4f3e-bb05-56cc3053de6f", "issue.relation.create", "Issue Relation Create", "建立 Issue 關聯。", "project"),
        new("bce4a59e-47d6-4664-bda4-c1ea66b50ec1", "issue.attachment.upload", "Issue Attachment Upload", "上傳 Issue 附件。", "project"),
        new("811e7203-b5ab-4b2c-83aa-8e071f68b36f", "issue.attachment.delete", "Issue Attachment Delete", "刪除 Issue 附件。", "project"),
    ];

    public static readonly string[] SystemPermissionCodes = All
        .Where(permission => permission.ScopeType == "system")
        .Select(permission => permission.Code)
        .ToArray();
}

public sealed record PermissionDefinition(
    string Id,
    string Code,
    string Name,
    string Description,
    string ScopeType);
