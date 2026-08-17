namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestRunRequest
{
    public CreateTestRunRequest(Guid planId, string name)
    {
        PlanId = planId;
        Name = name;
    }

    public Guid PlanId { get; }

    public string Name { get; }
}
