namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record AiTokenCommand
{
    public required Guid ActorAccountId { get; init; }
    public required Guid AiAccountId { get; init; }
}
