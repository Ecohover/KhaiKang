namespace KhaiKang.Modules.Identity.Contracts;

public sealed record AccountDirectoryEntry
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }
}
