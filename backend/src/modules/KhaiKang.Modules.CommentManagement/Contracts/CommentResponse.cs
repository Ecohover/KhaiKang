namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CommentResponse
{
    public required Guid Id { get; init; }
    public required Guid AuthorAccountId { get; init; }
    public required string AuthorDisplayName { get; init; }
    public required string AccountType { get; init; }
    public string? FixedRole { get; init; }
    public Guid? OwnerAccountId { get; init; }
    public string? OwnerDisplayName { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int Version { get; init; }
}
