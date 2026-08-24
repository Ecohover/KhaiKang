namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record CreateAiAccountRequest
{
    public required string Username { get; init; }
    public required string FixedRole { get; init; }
}
