namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCaseSourceLink
{
    private TestCaseSourceLink() { }

    internal TestCaseSourceLink(Guid id, Guid workspaceId, Guid caseId, Guid projectId, Guid testIssueId, Guid actorId, DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        TestCaseId = caseId;
        ProjectId = projectId;
        TestIssueId = testIssueId;
        CreatedAt = now;
        CreatedByAccountId = actorId;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid TestCaseId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TestIssueId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
}
