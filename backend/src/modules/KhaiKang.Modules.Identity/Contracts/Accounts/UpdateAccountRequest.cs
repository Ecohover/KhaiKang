namespace KhaiKang.Modules.Identity.Contracts;

public sealed record UpdateAccountRequest
{
    public required string Username { get; init; }

    public required int Version { get; init; }
}
