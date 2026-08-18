namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record UpdateTestTagRequest
{
    public required string Name { get; init; }

    public required string? Description { get; init; }

    public required string Status { get; init; }

    public required int Version { get; init; }
}
