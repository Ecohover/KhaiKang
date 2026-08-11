namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelationType : AuditableEntity
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
}
