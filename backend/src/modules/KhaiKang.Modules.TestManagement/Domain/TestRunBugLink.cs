namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestRunBugLink
{
    private TestRunBugLink() { }

    public TestRunBugLink(
        Guid id,
        Guid workspaceId,
        Guid testRunId,
        Guid projectId,
        Guid bugIssueId,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        TestRunId = testRunId;
        ProjectId = projectId;
        BugIssueId = bugIssueId;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid TestRunId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BugIssueId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public TestWorkspace Workspace { get; private set; } = null!;
    public TestRun TestRun { get; private set; } = null!;
}
