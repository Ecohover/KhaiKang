namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record SetAiApiEnabledCommand
{
    public required Guid ActorAccountId { get; init; }
    public required Guid AiAccountId { get; init; }
    public required bool Enabled { get; init; }
    public required int Version { get; init; }
}
