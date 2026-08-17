namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record CreateIssueCommand
{
    public CreateIssueCommand(string title, string typeCode)
    {
        Title = title;
        TypeCode = typeCode;
    }

    public string Title { get; }

    public string TypeCode { get; }

    public string? PriorityCode { get; init; }

    public string? Description { get; init; }

    public Guid? AssigneeAccountId { get; init; }
}
