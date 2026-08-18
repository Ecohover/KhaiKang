namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestPlanRequest
{
    public CreateTestPlanRequest(string? description, IReadOnlyList<Guid> caseIds)
    {
        Description = description;
        CaseIds = caseIds;
    }

    public string? Description { get; }

    public IReadOnlyList<Guid> CaseIds { get; }

    public string? Name { get; init; }

    public Guid? TestIssueId { get; init; }
}
