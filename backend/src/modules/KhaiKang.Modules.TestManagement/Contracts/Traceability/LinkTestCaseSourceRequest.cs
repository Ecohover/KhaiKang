namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record LinkTestCaseSourceRequest
{
    public LinkTestCaseSourceRequest(Guid testIssueId)
    {
        TestIssueId = testIssueId;
    }

    public Guid TestIssueId { get; init; }
}
