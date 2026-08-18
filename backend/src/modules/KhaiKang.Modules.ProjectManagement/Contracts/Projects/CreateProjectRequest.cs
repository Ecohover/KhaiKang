namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record CreateProjectRequest
{
    public CreateProjectRequest(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; }

    public string Name { get; }

    public string? Description { get; init; }
}
