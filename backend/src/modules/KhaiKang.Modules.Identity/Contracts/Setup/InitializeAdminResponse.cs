namespace KhaiKang.Modules.Identity.Contracts;

public sealed record InitializeAdminResponse
{
    public required string Username { get; init; }

    public required string InitialPassword { get; init; }
}
