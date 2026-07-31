using KhaiKang.Modules.Identity.Contracts;

namespace KhaiKang.Modules.Identity.Application;

public sealed record CreateAccountResult(
    CreateAccountOutcome Outcome,
    CreateAccountResponse? Response = null);

public enum CreateAccountOutcome
{
    Succeeded,
    UsernameConflict,
    UserRoleNotConfigured,
}

public sealed record UpdateAccountResult(
    UpdateAccountOutcome Outcome,
    AccountResponse? Account = null);

public enum UpdateAccountOutcome
{
    Succeeded,
    NotFound,
    UsernameConflict,
    VersionConflict,
    CannotUpdateOwnAccount,
}

public sealed record UpdateAccountStatusResult(
    UpdateAccountStatusOutcome Outcome,
    AccountResponse? Account = null);

public enum UpdateAccountStatusOutcome
{
    Succeeded,
    NotFound,
    VersionConflict,
    CannotChangeOwnStatus,
}
