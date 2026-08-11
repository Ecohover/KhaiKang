namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestPlan
{
    private TestPlan() { }

    public TestPlan(
        Guid id,
        Guid workspaceId,
        int planNo,
        string name,
        string? description,
        Guid actorId,
        DateTimeOffset now,
        Guid? testIssueProjectId = null,
        Guid? testIssueId = null)
    {
        Id = id;
        TestWorkspaceId = workspaceId;
        PlanNo = planNo;
        Name = name;
        Description = description;
        TestIssueProjectId = testIssueProjectId;
        TestIssueId = testIssueId;
        Status = "draft";
        CreatedAt = UpdatedAt = now;
        CreatedByAccountId = UpdatedByAccountId = actorId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestWorkspaceId { get; private set; }
    public int PlanNo { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? TestIssueProjectId { get; private set; }
    public Guid? TestIssueId { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public ICollection<TestPlanItem> Items { get; } = [];
    public TestWorkspace Workspace { get; private set; } = null!;

    public void Update(
        string name,
        string? description,
        string status,
        Guid actorId,
        DateTimeOffset now,
        Guid? testIssueProjectId = null,
        Guid? testIssueId = null)
    {
        Name = name; Description = description; Status = status;
        TestIssueProjectId = testIssueProjectId; TestIssueId = testIssueId;
        UpdatedAt = now; UpdatedByAccountId = actorId; Version++;
    }
}
