namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public enum AiAccountOperationOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    VersionConflict,
    UsernameConflict,
    OwnerNotActiveHuman,
}
