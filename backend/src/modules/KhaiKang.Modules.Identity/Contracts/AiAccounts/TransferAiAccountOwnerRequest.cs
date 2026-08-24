namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record TransferAiAccountOwnerRequest
{
    public required Guid OwnerAccountId { get; init; }
    public required int Version { get; init; }
}
