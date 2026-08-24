namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record AiBearerIdentityResponse
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string AccountType { get; init; }
    public required string FixedRole { get; init; }
    public required Guid OwnerAccountId { get; init; }
    public required string OwnerDisplayName { get; init; }
}
