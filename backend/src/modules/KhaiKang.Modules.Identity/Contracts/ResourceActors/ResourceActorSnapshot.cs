namespace KhaiKang.Modules.Identity.Contracts;

public sealed record ResourceActorSnapshot
{
    public required Guid AccountId { get; init; }
    public required Guid ScopeAccountId { get; init; }
    public required string AccountName { get; init; }
    public required string AccountType { get; init; }
    public string? FixedRole { get; init; }
    public Guid? OwnerAccountId { get; init; }
    public string? OwnerDisplayName { get; init; }

    public bool IsAi => AccountType == "ai_agent";
}
