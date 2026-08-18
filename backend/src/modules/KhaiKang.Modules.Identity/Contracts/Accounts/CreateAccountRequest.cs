namespace KhaiKang.Modules.Identity.Contracts;

public sealed record CreateAccountRequest
{
    public required string Username { get; init; }
}
