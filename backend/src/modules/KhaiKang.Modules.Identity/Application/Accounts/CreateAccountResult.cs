using KhaiKang.Modules.Identity.Contracts;

namespace KhaiKang.Modules.Identity.Application;

public sealed record CreateAccountResult
{
    private CreateAccountResult(
        CreateAccountOutcome outcome,
        CreateAccountResponse? response)
    {
        Outcome = outcome;
        Response = response;
    }

    public CreateAccountOutcome Outcome { get; }

    public CreateAccountResponse? Response { get; }

    public static CreateAccountResult Success(CreateAccountResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new CreateAccountResult(CreateAccountOutcome.Succeeded, response);
    }

    public static CreateAccountResult Failure(CreateAccountOutcome outcome)
    {
        if (outcome == CreateAccountOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A successful account creation requires a response.");
        }

        return new CreateAccountResult(outcome, null);
    }
}
