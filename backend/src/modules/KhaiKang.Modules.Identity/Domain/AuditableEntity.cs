namespace KhaiKang.Modules.Identity.Domain;

public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;

    protected void InitializeAudit(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        CreatedAt = occurredAt;
        CreatedByAccountId = actorAccountId;
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version = 1;
    }

    protected void RecordChange(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        UpdatedAt = occurredAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }
}
