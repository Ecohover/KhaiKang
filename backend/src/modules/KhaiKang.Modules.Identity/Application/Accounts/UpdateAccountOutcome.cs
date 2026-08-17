namespace KhaiKang.Modules.Identity.Application;

public enum UpdateAccountOutcome
{
    Succeeded,
    NotFound,
    UsernameConflict,
    VersionConflict,
    CannotUpdateOwnAccount,
}
