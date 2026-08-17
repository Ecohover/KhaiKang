namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestCaseRequest
{
    public CreateTestCaseRequest(
        Guid suiteId,
        string title,
        IReadOnlyList<CreateTestCaseStepRequest> steps)
    {
        SuiteId = suiteId;
        Title = title;
        Steps = steps;
    }

    public Guid SuiteId { get; }

    public string Title { get; }

    public IReadOnlyList<CreateTestCaseStepRequest> Steps { get; }

    public required int SortOrder { get; init; }

    public string? Description { get; init; }

    public string? Preconditions { get; init; }

    public string? OverallExpectedResult { get; init; }

    public IReadOnlyList<Guid>? TagIds { get; init; }
}
