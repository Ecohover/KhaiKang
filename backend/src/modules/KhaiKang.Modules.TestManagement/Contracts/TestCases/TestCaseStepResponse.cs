namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestCaseStepResponse
{
    public required Guid Id { get; init; }

    public required int StepNo { get; init; }

    public required string Action { get; init; }

    public required string ExpectedResult { get; init; }
}
