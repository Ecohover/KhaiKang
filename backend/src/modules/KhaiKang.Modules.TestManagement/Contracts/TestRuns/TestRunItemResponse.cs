namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestRunItemResponse
{
    public required Guid Id { get; init; }

    public required Guid CaseId { get; init; }

    public required int SortOrder { get; init; }

    public required string CaseTitle { get; init; }

    public required string? CaseDescription { get; init; }

    public required string? Preconditions { get; init; }

    public required string? OverallExpectedResult { get; init; }

    public required string ResultStatus { get; init; }

    public required string? ActualResult { get; init; }

    public required Guid? ExecutedByAccountId { get; init; }

    public required DateTimeOffset? ExecutedAt { get; init; }

    public required IReadOnlyList<TestRunStepResponse> Steps { get; init; }

    public required int Version { get; init; }
}
