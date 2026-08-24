namespace KhaiKang.Modules.Identity.Domain;

public sealed class AiAccountToken : AuditableEntity
{
    private AiAccountToken()
    {
    }

    public AiAccountToken(AiAccountTokenCreation creation)
    {
        Id = creation.Id;
        AiAccountId = creation.AiAccountId;
        Apply(creation.Material);
        InitializeAudit(creation.ActorAccountId, creation.OccurredAt);
    }

    public Guid Id { get; private set; }
    public Guid AiAccountId { get; private set; }
    public Account AiAccount { get; private set; } = null!;
    public string ProtectedSecret { get; private set; } = null!;
    public string LookupVerifier { get; private set; } = null!;
    public string ProtectionVersion { get; private set; } = null!;
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByAccountId { get; private set; }

    public bool IsActive => RevokedAt is null;

    public void Recreate(
        AiTokenMaterial material,
        Guid actorAccountId,
        DateTimeOffset occurredAt)
    {
        Apply(material);
        RecordChange(actorAccountId, occurredAt);
    }

    public void Revoke(Guid actorAccountId, DateTimeOffset occurredAt)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = occurredAt;
        RevokedByAccountId = actorAccountId;
        RecordChange(actorAccountId, occurredAt);
    }

    private void Apply(AiTokenMaterial material)
    {
        ProtectedSecret = material.ProtectedSecret;
        LookupVerifier = material.LookupVerifier;
        ProtectionVersion = material.ProtectionVersion;
        RevokedAt = null;
        RevokedByAccountId = null;
    }
}
