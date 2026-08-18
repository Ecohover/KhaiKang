using KhaiKang.Modules.Identity.Contracts;

namespace KhaiKang.Modules.Identity.Application;

public sealed record UpdateAccountStatusResult
{
    private UpdateAccountStatusResult(
        UpdateAccountStatusOutcome outcome,
        AccountResponse? account)
    {
        Outcome = outcome;
        Account = account;
    }

    public UpdateAccountStatusOutcome Outcome { get; }

    public AccountResponse? Account { get; }

    public static UpdateAccountStatusResult Success(AccountResponse account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new UpdateAccountStatusResult(UpdateAccountStatusOutcome.Succeeded, account);
    }

    public static UpdateAccountStatusResult Failure(UpdateAccountStatusOutcome outcome)
    {
        if (outcome == UpdateAccountStatusOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A successful account status update requires an account.");
        }

        return new UpdateAccountStatusResult(outcome, null);
    }
}
