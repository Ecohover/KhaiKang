namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record TransferAiAccountOwnerCommand
{
    public required Guid ActorAccountId { get; init; }
    public required Guid AiAccountId { get; init; }
    public required Guid OwnerAccountId { get; init; }
    public required int Version { get; init; }
}
