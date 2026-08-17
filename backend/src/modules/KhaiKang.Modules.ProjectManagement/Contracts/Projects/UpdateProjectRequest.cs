namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateProjectRequest
{
    public UpdateProjectRequest(string name, string status, int version)
    {
        Name = name;
        Status = status;
        Version = version;
    }

    public string Name { get; }

    public string? Description { get; init; }

    public string Status { get; }

    public int Version { get; }
}
