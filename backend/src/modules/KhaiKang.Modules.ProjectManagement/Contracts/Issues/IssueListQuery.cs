namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed class IssueListQuery
{
    public string? Search { get; init; }

    public string? TypeCode { get; init; }

    public string? StatusCode { get; init; }

    public string? PriorityCode { get; init; }

    public Guid? AssigneeAccountId { get; init; }

    public bool? Unassigned { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
