namespace KhaiKang.Modules.ProjectManagement.Domain;

public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    protected void InitializeAudit(ChangeContext context)
    {
        CreatedAt = context.OccurredAt;
        CreatedByAccountId = context.ActorAccountId;
        UpdatedAt = context.OccurredAt;
        UpdatedByAccountId = context.ActorAccountId;
        Version = 1;
    }

    protected void RecordChange(ChangeContext context)
    {
        UpdatedAt = context.OccurredAt;
        UpdatedByAccountId = context.ActorAccountId;
        Version++;
    }
}
