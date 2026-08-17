using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Application;

internal static class IssueCatalog
{
    public static readonly Guid CreatedStatusId = Guid.Parse("8b211fd1-20f5-4bcb-a3f2-2bb222472c10");
    public static readonly Guid MediumPriorityId = Guid.Parse("dc8a3357-7002-46f8-98c8-ad46476d7515");

    public static readonly IssueTypeDefinition[] Types =
    [
        new(Guid.Parse("28395b03-a812-4e53-bbde-85a598166d71"), "story", "Story", "用來表示具體需求或使用者價值。", 1),
        new(Guid.Parse("d179951d-a8b4-4a37-8059-e79dc8ea25fb"), "task", "Task", "用來表示一般執行工作項目。", 2),
        new(Guid.Parse("66f5813f-a357-47de-961a-572744bc25a9"), "bug", "Bug", "用來表示缺陷或異常問題。", 3),
        new(Guid.Parse("7c80596d-325c-43c1-9e1b-757b14f975e8"), "spike", "Spike", "用來表示研究、驗證或技術探索工作。", 4),
    ];

    public static readonly IssueStatusDefinition[] Statuses =
    [
        new(CreatedStatusId, "created", "Created", "表示任務已建立，尚未正式進入處理。", IssueStatusCategory.Todo, 1),
        new(Guid.Parse("343e8e3c-4baa-41a3-bd3e-7840ae938244"), "in_progress", "In Progress", "表示任務目前正在處理中。", IssueStatusCategory.Doing, 2),
        new(Guid.Parse("b48dfc2c-1084-45ff-8c93-ac7d9613943b"), "verifying", "Verifying", "表示任務目前正在驗證中。", IssueStatusCategory.Doing, 3),
        new(Guid.Parse("62059722-9c39-4bce-b805-2490cdb6fe77"), "completed", "Completed", "表示任務已完成。", IssueStatusCategory.Done, 4),
    ];

    public static readonly IssuePriorityDefinition[] Priorities =
    [
        new(Guid.Parse("1cc8b25d-a2e8-40d2-b971-5193ffbf2fe3"), "low", "Low", "低優先級。", 1),
        new(MediumPriorityId, "medium", "Medium", "一般預設優先級。", 2),
        new(Guid.Parse("3e722449-95fd-4793-a3c1-8437acd5d5e4"), "high", "High", "高優先級。", 3),
        new(Guid.Parse("8178b599-48da-4743-a560-d3633477f1ac"), "critical", "Critical", "需要立即關注的關鍵優先級。", 4),
    ];
}

internal sealed record IssueTypeDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int SortOrder);

internal sealed record IssueStatusDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    IssueStatusCategory Category,
    int SortOrder);

internal sealed record IssuePriorityDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int SortOrder);
