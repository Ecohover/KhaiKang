namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueRelationIssueResponse
{
    public required Guid Id { get; init; }

    public required int IssueNo { get; init; }

    public required string Key { get; init; }

    public required string Title { get; init; }

    public required string TypeCode { get; init; }

    public required string StatusCode { get; init; }
}
