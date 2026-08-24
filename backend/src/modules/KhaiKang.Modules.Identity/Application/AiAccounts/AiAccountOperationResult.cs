using KhaiKang.Modules.Identity.Contracts.AiAccounts;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record AiAccountOperationResult
{
    private AiAccountOperationResult()
    {
    }

    public required AiAccountOperationOutcome Outcome { get; init; }
    public AiAccountResponse? Account { get; init; }
    public string? InitialPassword { get; init; }

    public static AiAccountOperationResult Success(
        AiAccountResponse account,
        string? initialPassword = null) => new()
        {
            Outcome = AiAccountOperationOutcome.Succeeded,
            Account = account,
            InitialPassword = initialPassword,
        };

    public static AiAccountOperationResult Failure(AiAccountOperationOutcome outcome) => new()
    {
        Outcome = outcome,
    };
}
