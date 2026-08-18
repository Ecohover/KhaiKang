namespace KhaiKang.Modules.Identity.Application;

public enum ChangePasswordOutcome
{
    Succeeded,
    InvalidCurrentPassword,
    PasswordTooShort,
    SessionNotFound,
}
