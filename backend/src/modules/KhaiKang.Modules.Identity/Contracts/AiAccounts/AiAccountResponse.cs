namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record AiAccountResponse
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string AccountType { get; init; }
    public required string Status { get; init; }
    public required Guid OwnerAccountId { get; init; }
    public required string OwnerDisplayName { get; init; }
    public required string FixedRole { get; init; }
    public required bool AiApiEnabled { get; init; }
    public required bool HasActiveToken { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
