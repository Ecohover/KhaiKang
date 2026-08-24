namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record SetAiApiEnabledRequest
{
    public required bool Enabled { get; init; }
    public required int Version { get; init; }
}
