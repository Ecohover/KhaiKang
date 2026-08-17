using KhaiKang.Modules.Identity.Contracts;

namespace KhaiKang.Modules.Identity.Application;

public sealed record UpdateAccountResult
{
    private UpdateAccountResult(
        UpdateAccountOutcome outcome,
        AccountResponse? account)
    {
        Outcome = outcome;
        Account = account;
    }

    public UpdateAccountOutcome Outcome { get; }

    public AccountResponse? Account { get; }

    public static UpdateAccountResult Success(AccountResponse account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new UpdateAccountResult(UpdateAccountOutcome.Succeeded, account);
    }

    public static UpdateAccountResult Failure(UpdateAccountOutcome outcome)
    {
        if (outcome == UpdateAccountOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A successful account update requires an account.");
        }

        return new UpdateAccountResult(outcome, null);
    }
}
