namespace KhaiKang.Modules.Identity.Contracts;

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Username,
    IReadOnlyList<string> SystemRoles,
    IReadOnlyList<string> SystemPermissions,
    bool MustChangePassword);
