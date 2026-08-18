namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record LinkTestCaseRequirementIssueRequest
{
    public LinkTestCaseRequirementIssueRequest(Guid requirementIssueId)
    {
        RequirementIssueId = requirementIssueId;
    }

    public Guid RequirementIssueId { get; }
}
