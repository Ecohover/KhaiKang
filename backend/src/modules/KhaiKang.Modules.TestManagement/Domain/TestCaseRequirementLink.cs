namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCaseRequirementLink
{
    private TestCaseRequirementLink() { }

    public TestCaseRequirementLink(
        Guid id,
        Guid workspaceId,
        Guid testCaseId,
        Guid projectId,
        Guid requirementIssueId,
        Guid actorId,
        DateTimeOffset now)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        TestCaseId = testCaseId;
        ProjectId = projectId;
        RequirementIssueId = requirementIssueId;
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public Guid TestCaseId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid RequirementIssueId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedByAccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public TestWorkspace Workspace { get; private set; } = null!;
    public TestCase TestCase { get; private set; } = null!;

    public void Delete(Guid actorId, DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        DeletedByAccountId = actorId;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
        Version++;
    }
}
