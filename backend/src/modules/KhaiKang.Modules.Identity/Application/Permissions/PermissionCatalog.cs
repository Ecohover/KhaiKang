namespace KhaiKang.Modules.Identity.Application;

public static class PermissionCatalog
{
    public const string ClaimType = "permission";

    public static readonly PermissionDefinition[] All =
    [
        Define(
            id: "2d19d818-0214-4be2-b080-621e0cf0c526",
            code: "account.read",
            name: "Account Read",
            description: "查看使用者帳號資料。",
            scopeType: "system"),
        Define(
            id: "da7ff46b-3349-4a09-a4e9-5d60542bc2b2",
            code: "account.create",
            name: "Account Create",
            description: "建立本機使用者帳號。",
            scopeType: "system"),
        Define(
            id: "3cda75d2-995b-4e94-bdab-9307429352c5",
            code: "account.update",
            name: "Account Update",
            description: "修改使用者基本資料或重設密碼。",
            scopeType: "system"),
        Define(
            id: "ae2a3092-bd64-42bc-88f1-708e7abdac8a",
            code: "account.suspend",
            name: "Account Suspend",
            description: "停權、停用或恢復使用者帳號。",
            scopeType: "system"),
        Define(
            id: "297e6b10-207a-47f5-b604-47c40f1e6bc1",
            code: "project.create",
            name: "Project Create",
            description: "建立新專案。",
            scopeType: "system"),
        Define(
            id: "1a8f54ab-c19f-4356-a40b-fe4fcbeda0fb",
            code: "project.deactivate",
            name: "Project Deactivate",
            description: "停用或恢復專案。",
            scopeType: "system"),
        Define(
            id: "1e42b09d-9839-4e8c-951c-38c941f9e4ca",
            code: "project.read",
            name: "Project Read",
            description: "查看專案基本內容。",
            scopeType: "project"),
        Define(
            id: "f622c88d-d0f0-40ed-86c0-bba2c3ff44c9",
            code: "project.update",
            name: "Project Update",
            description: "修改專案基本資料。",
            scopeType: "project"),
        Define(
            id: "23cffaf6-9d01-4116-9a79-a4970bc01eae",
            code: "project.member.add",
            name: "Project Member Add",
            description: "新增專案成員。",
            scopeType: "project"),
        Define(
            id: "bb8afb36-8753-4383-bce7-83065a92c0d3",
            code: "project.member.remove",
            name: "Project Member Remove",
            description: "移除專案成員。",
            scopeType: "project"),
        Define(
            id: "0cc976a0-4f5a-4ce4-9309-6f4476522aa6",
            code: "project.role.assign",
            name: "Project Role Assign",
            description: "指派或調整專案角色。",
            scopeType: "project"),
        Define(
            id: "3c2bdd52-7445-4a16-9750-1b28d97bf109",
            code: "issue.create",
            name: "Issue Create",
            description: "建立 Issue。",
            scopeType: "project"),
        Define(
            id: "3ed83136-9532-4e6e-8adb-b73ae9863a2c",
            code: "issue.read",
            name: "Issue Read",
            description: "查看 Issue。",
            scopeType: "project"),
        Define(
            id: "a5fa36de-f8eb-491a-981c-f2f17244fa2b",
            code: "issue.update",
            name: "Issue Update",
            description: "編輯 Issue。",
            scopeType: "project"),
        Define(
            id: "084b565e-e59c-4b48-9a4d-2de2a58e0a9d",
            code: "issue.status.change",
            name: "Issue Status Change",
            description: "變更 Issue 狀態。",
            scopeType: "project"),
        Define(
            id: "60098112-f880-40d8-97d6-bed5784f83a0",
            code: "issue.assignee.change",
            name: "Issue Assignee Change",
            description: "變更 Issue 處理人。",
            scopeType: "project"),
        Define(
            id: "2f36b9f4-b7f1-4cd2-af25-46393d560b13",
            code: "issue.comment.create",
            name: "Issue Comment Create",
            description: "新增 Issue 留言。",
            scopeType: "project"),
        Define(
            id: "081eced1-97b4-4f3e-bb05-56cc3053de6f",
            code: "issue.relation.create",
            name: "Issue Relation Create",
            description: "建立 Issue 關聯。",
            scopeType: "project"),
        Define(
            id: "bce4a59e-47d6-4664-bda4-c1ea66b50ec1",
            code: "issue.attachment.upload",
            name: "Issue Attachment Upload",
            description: "上傳 Issue 附件。",
            scopeType: "project"),
        Define(
            id: "811e7203-b5ab-4b2c-83aa-8e071f68b36f",
            code: "issue.attachment.delete",
            name: "Issue Attachment Delete",
            description: "刪除 Issue 附件。",
            scopeType: "project"),
    ];

    public static readonly string[] SystemPermissionCodes = All
        .Where(permission => permission.ScopeType == "system")
        .Select(permission => permission.Code)
        .ToArray();

    private static PermissionDefinition Define(
        string id,
        string code,
        string name,
        string description,
        string scopeType)
    {
        return new PermissionDefinition
        {
            Id = id,
            Code = code,
            Name = name,
            Description = description,
            ScopeType = scopeType,
        };
    }
}
