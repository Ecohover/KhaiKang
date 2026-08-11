namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelationType
{
    private IssueRelationType() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string ForwardLabel { get; private set; } = null!;
    public string ReverseLabel { get; private set; } = null!;
    public string DirectionKind { get; private set; } = null!;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
}
