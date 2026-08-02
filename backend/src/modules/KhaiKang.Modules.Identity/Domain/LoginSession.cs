namespace KhaiKang.Modules.Identity.Domain;

public sealed class LoginSession
{
    private LoginSession()
    {
    }

    public LoginSession(
        Guid id,
        Guid accountId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        bool isPersistent)
    {
        Id = id;
        AccountId = accountId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        IsPersistent = isPersistent;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Account Account { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsPersistent { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsValidAt(DateTimeOffset occurredAt)
    {
        return RevokedAt is null && ExpiresAt > occurredAt;
    }

    public void Revoke(DateTimeOffset occurredAt)
    {
        RevokedAt ??= occurredAt;
    }
}
