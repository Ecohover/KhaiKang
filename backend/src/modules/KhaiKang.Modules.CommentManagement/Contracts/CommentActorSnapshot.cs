namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CommentActorSnapshot
{
    public required Guid AccountId { get; init; }
    public required string DisplayName { get; init; }
    public required string AccountType { get; init; }
    public string? FixedRole { get; init; }
    public Guid? OwnerAccountId { get; init; }
    public string? OwnerDisplayName { get; init; }
}
