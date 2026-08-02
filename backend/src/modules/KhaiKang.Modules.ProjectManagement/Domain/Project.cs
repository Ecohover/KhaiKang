namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class Project
{
    private Project()
    {
    }

    public Project(
        Guid id,
        string code,
        string name,
        string? description,
        Guid creatorAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        CreatedByAccountId = creatorAccountId;
        CreatedAt = createdAt;
        UpdatedByAccountId = creatorAccountId;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public ProjectStatus Status { get; private set; } = ProjectStatus.Active;

    public Guid CreatedByAccountId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int Version { get; private set; } = 1;

    public ICollection<ProjectMember> Members { get; } = [];

    public ICollection<Issue> Issues { get; } = [];

    public void Update(
        string name,
        string? description,
        ProjectStatus status,
        Guid actorAccountId,
        DateTimeOffset updatedAt)
    {
        Name = name;
        Description = description;
        Status = status;
        UpdatedByAccountId = actorAccountId;
        UpdatedAt = updatedAt;
        Version++;
    }
}
