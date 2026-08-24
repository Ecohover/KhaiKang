using KhaiKang.Modules.Identity.Contracts.AiAccounts;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record AiTokenOperationResult
{
    private AiTokenOperationResult()
    {
    }

    public required AiTokenOperationOutcome Outcome { get; init; }
    public AiTokenPlaintextResponse? Token { get; init; }

    public static AiTokenOperationResult Success(AiTokenPlaintextResponse? token = null) => new()
    {
        Outcome = AiTokenOperationOutcome.Succeeded,
        Token = token,
    };

    public static AiTokenOperationResult Failure(AiTokenOperationOutcome outcome) => new()
    {
        Outcome = outcome,
    };
}
