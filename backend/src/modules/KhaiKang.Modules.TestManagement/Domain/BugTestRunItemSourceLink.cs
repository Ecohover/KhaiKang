namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class BugTestRunItemSourceLink
{
    private BugTestRunItemSourceLink() { }
    internal BugTestRunItemSourceLink(Guid id, Guid workspaceId, Guid projectId, Guid bugIssueId, Guid runId, Guid runItemId, Guid actorId, DateTimeOffset now)
    {
        Id = id; TestWorkspaceId = workspaceId; ProjectId = projectId; BugIssueId = bugIssueId;
        TestRunId = runId; TestRunItemId = runItemId; CreatedAt = now; CreatedByAccountId = actorId;
    }
    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BugIssueId { get; private set; }
    public Guid TestRunId { get; private set; }
    public Guid TestRunItemId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
}
