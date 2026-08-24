namespace KhaiKang.Modules.Identity.Domain;

public sealed class Account
{
    private Account()
    {
    }

    public Account(
        Guid id,
        string username,
        string normalizedUsername,
        DateTimeOffset createdAt,
        Guid? createdByAccountId = null)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        CreatedAt = createdAt;
        CreatedByAccountId = createdByAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = createdByAccountId;
    }

    private Account(AiAccountCreation creation)
    {
        Id = creation.Id;
        Username = creation.Username;
        NormalizedUsername = creation.NormalizedUsername;
        AccountType = AccountType.AiAgent;
        OwnerAccountId = creation.OwnerAccountId;
        FixedRole = creation.FixedRole;
        CreatedAt = creation.CreatedAt;
        CreatedByAccountId = creation.OwnerAccountId;
        UpdatedAt = creation.CreatedAt;
        UpdatedByAccountId = creation.OwnerAccountId;
    }

    public static Account CreateAi(AiAccountCreation creation) => new(creation);

    public Guid Id { get; private set; }

    public string Username { get; private set; } = null!;

    public string NormalizedUsername { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public AccountType AccountType { get; private set; } = AccountType.Human;

    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public Guid? OwnerAccountId { get; private set; }

    public Account? OwnerAccount { get; private set; }

    public AiFixedRole? FixedRole { get; private set; }

    public bool AiApiEnabled { get; private set; }

    public bool MustChangePassword { get; private set; } = true;

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<AccountSystemRole> SystemRoles { get; } = [];

    public ICollection<LoginSession> Sessions { get; } = [];

    public ICollection<Account> OwnedAiAccounts { get; } = [];

    public AiAccountToken? AiToken { get; private set; }

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

    public void ChangeStatus(AccountStatus status, Guid actorId, DateTimeOffset occurredAt)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedByAccountId = actorId;
        UpdatedAt = occurredAt;
        Version++;
    }

    public void Rename(
        string username,
        string normalizedUsername,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (NormalizedUsername == normalizedUsername && Username == username)
        {
            return;
        }

        Username = username;
        NormalizedUsername = normalizedUsername;
        UpdatedByAccountId = actorId;
        UpdatedAt = occurredAt;
        Version++;
    }

    public void SetAiApiEnabled(bool enabled, Guid actorId, DateTimeOffset occurredAt)
    {
        if (AccountType != AccountType.AiAgent)
        {
            throw new InvalidOperationException("Only AI accounts can enable the AI API.");
        }

        if (AiApiEnabled == enabled)
        {
            return;
        }

        AiApiEnabled = enabled;
        UpdatedByAccountId = actorId;
        UpdatedAt = occurredAt;
        Version++;
    }

    public void TransferOwner(Guid ownerAccountId, Guid actorId, DateTimeOffset occurredAt)
    {
        if (AccountType != AccountType.AiAgent)
        {
            throw new InvalidOperationException("Only AI accounts have an owner.");
        }

        if (OwnerAccountId == ownerAccountId)
        {
            return;
        }

        OwnerAccountId = ownerAccountId;
        UpdatedByAccountId = actorId;
        UpdatedAt = occurredAt;
        Version++;
    }
}
