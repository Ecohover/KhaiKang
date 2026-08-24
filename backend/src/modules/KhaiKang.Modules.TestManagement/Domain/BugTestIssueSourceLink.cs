namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class BugTestIssueSourceLink
{
    private BugTestIssueSourceLink() { }
    internal BugTestIssueSourceLink(Guid id, Guid workspaceId, Guid projectId, Guid bugIssueId, Guid testIssueId, Guid actorId, DateTimeOffset now)
    {
        Id = id; TestWorkspaceId = workspaceId; ProjectId = projectId; BugIssueId = bugIssueId;
        TestIssueId = testIssueId; CreatedAt = now; CreatedByAccountId = actorId;
    }
    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BugIssueId { get; private set; }
    public Guid TestIssueId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
}
