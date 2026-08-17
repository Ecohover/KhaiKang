namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestWorkspaceRequest
{
    public CreateTestWorkspaceRequest(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string? Prefix { get; init; }

    public string? Description { get; init; }
}
