namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestRunStepResponse
{
    public required Guid Id { get; init; }

    public required int StepNo { get; init; }

    public required string Action { get; init; }

    public required string ExpectedResult { get; init; }

    public required string ResultStatus { get; init; }

    public required string? ActualResult { get; init; }

    public required Guid? ExecutedByAccountId { get; init; }

    public required DateTimeOffset? ExecutedAt { get; init; }

    public required int Version { get; init; }
}
