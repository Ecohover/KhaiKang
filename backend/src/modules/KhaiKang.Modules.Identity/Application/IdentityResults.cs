using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Application;

public sealed record LoginResult(
    LoginOutcome Outcome,
    LoginSession? Session = null,
    AuthenticatedUserResponse? User = null);

public enum LoginOutcome
{
    Succeeded,
    InvalidCredentials,
}

public enum ChangePasswordOutcome
{
    Succeeded,
    InvalidCurrentPassword,
    PasswordTooShort,
    SessionNotFound,
}
