namespace KhaiKang.Modules.Identity.Application;

public enum UpdateAccountStatusOutcome
{
    Succeeded,
    NotFound,
    VersionConflict,
    CannotChangeOwnStatus,
}
