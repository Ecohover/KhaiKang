namespace KhaiKang.Modules.Identity.Domain;

public sealed class Account
{
    private Account()
    {
    }

    public Account(Guid id, string username, string normalizedUsername, DateTimeOffset createdAt)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; } = null!;

    public string NormalizedUsername { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public AccountType AccountType { get; private set; } = AccountType.Human;

    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public bool MustChangePassword { get; private set; } = true;

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<AccountSystemRole> SystemRoles { get; } = [];

    public ICollection<LoginSession> Sessions { get; } = [];

    public void SetInitialPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void RecordSuccessfulLogin(DateTimeOffset occurredAt)
    {
        LastLoginAt = occurredAt;
        UpdatedAt = occurredAt;
        Version++;
    }

    public void ChangePassword(string passwordHash, Guid actorId, DateTimeOffset occurredAt)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        UpdatedByAccountId = actorId;
        UpdatedAt = occurredAt;
        Version++;
    }
}
