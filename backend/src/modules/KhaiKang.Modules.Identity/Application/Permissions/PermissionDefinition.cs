namespace KhaiKang.Modules.Identity.Application;

public sealed record PermissionDefinition
{
    public required string Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string ScopeType { get; init; }
}
