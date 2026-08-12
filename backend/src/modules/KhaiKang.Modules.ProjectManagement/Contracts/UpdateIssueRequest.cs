namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateIssueRequest
{
    public UpdateIssueRequest(
        string title,
        string typeCode,
        string priorityCode,
        int version)
    {
        Title = title;
        TypeCode = typeCode;
        PriorityCode = priorityCode;
        Version = version;
    }

    public string Title { get; }

    public string TypeCode { get; }

    public string PriorityCode { get; }

    public string? Description { get; init; }

    public string? UserStory { get; init; }

    public string? DefinitionOfDone { get; init; }

    public string? CompletionSummary { get; init; }

    public int Version { get; }
}
