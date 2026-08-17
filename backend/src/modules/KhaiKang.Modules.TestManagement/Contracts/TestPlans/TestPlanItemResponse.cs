namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestPlanItemResponse
{
    public required Guid Id { get; init; }

    public required Guid CaseId { get; init; }

    public required int SortOrder { get; init; }

    public required string CaseTitle { get; init; }
}
