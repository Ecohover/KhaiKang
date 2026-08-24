namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record AiTokenPlaintextResponse
{
    public required string Token { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int Version { get; init; }
}
