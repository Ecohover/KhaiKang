namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestTagRequest
{
    public CreateTestTagRequest(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string? Description { get; }
}
