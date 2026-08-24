namespace KhaiKang.Modules.Identity.Domain;

public sealed record AiAccountCreation
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string NormalizedUsername { get; init; }
    public required Guid OwnerAccountId { get; init; }
    public required AiFixedRole FixedRole { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
