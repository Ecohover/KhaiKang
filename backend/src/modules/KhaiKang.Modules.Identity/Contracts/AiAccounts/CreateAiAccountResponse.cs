namespace KhaiKang.Modules.Identity.Contracts.AiAccounts;

public sealed record CreateAiAccountResponse
{
    public required AiAccountResponse Account { get; init; }
    public required string InitialPassword { get; init; }
}
