namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestTraceIssueResponse
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string ProjectCode { get; init; }

    public required int IssueNo { get; init; }

    public required string Key { get; init; }

    public required string Title { get; init; }

    public required string TypeCode { get; init; }

    public required string StatusCode { get; init; }
}
