namespace KhaiKang.Modules.Identity.Contracts;

public sealed record LoginRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }

    public required bool RememberMe { get; init; }
}
