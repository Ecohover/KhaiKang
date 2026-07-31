namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestPlanItem
{
    private TestPlanItem() { }

    public TestPlanItem(Guid id, Guid planId, Guid caseId, int sortOrder, Guid actorId, DateTimeOffset now)
    {
        Id = id; TestPlanId = planId; TestCaseId = caseId; SortOrder = sortOrder;
        CreatedAt = UpdatedAt = now; CreatedByAccountId = UpdatedByAccountId = actorId; Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid TestPlanId { get; private set; }
    public Guid TestCaseId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
    public TestCase TestCase { get; private set; } = null!;
}
