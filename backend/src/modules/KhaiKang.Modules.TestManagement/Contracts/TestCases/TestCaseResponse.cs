namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestCaseResponse
{
    public required Guid Id { get; init; }

    public required Guid SuiteId { get; init; }

    public required int CaseNo { get; init; }

    public required IReadOnlyList<TestTagResponse> Tags { get; init; }

    public required string Title { get; init; }

    public required string? Description { get; init; }

    public required string? Preconditions { get; init; }

    public required string? OverallExpectedResult { get; init; }

    public required int SortOrder { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<TestCaseStepResponse> Steps { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int Version { get; init; }
}
