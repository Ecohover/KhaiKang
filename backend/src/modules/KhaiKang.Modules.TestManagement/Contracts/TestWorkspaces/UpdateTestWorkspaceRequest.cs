namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record UpdateTestWorkspaceRequest
{
    public UpdateTestWorkspaceRequest(string name, string status, int version)
    {
        Name = name;
        Status = status;
        Version = version;
    }

    public string Name { get; }

    public string Status { get; }

    public int Version { get; }

    public string? Description { get; init; }
}
