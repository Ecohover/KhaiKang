using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Application;

public sealed record LoginResult
{
    private LoginResult(
        LoginOutcome outcome,
        LoginSession? session,
        AuthenticatedUserResponse? user)
    {
        Outcome = outcome;
        Session = session;
        User = user;
    }

    public LoginOutcome Outcome { get; }

    public LoginSession? Session { get; }

    public AuthenticatedUserResponse? User { get; }

    public static LoginResult Success(
        LoginSession session,
        AuthenticatedUserResponse user)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(user);

        return new LoginResult(LoginOutcome.Succeeded, session, user);
    }

    public static LoginResult InvalidCredentials()
    {
        return new LoginResult(LoginOutcome.InvalidCredentials, null, null);
    }
}
