namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueOptionResponse
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Category { get; init; }
}
