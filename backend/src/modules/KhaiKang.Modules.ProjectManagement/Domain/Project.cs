namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class Project
{
    private Project()
    {
    }

    private Project(ProjectCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        Code = creation.Code;
        Name = creation.Name;
        Description = creation.Description;
        CreatedByAccountId = context.ActorAccountId;
        CreatedAt = context.OccurredAt;
        UpdatedByAccountId = context.ActorAccountId;
        UpdatedAt = context.OccurredAt;
    }

    public static Project Create(ProjectCreation creation, ChangeContext context)
    {
        return new Project(creation, context);
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

    public void Update(ProjectDetailsChange change, ChangeContext context)
    {
        Name = change.Name;
        Description = change.Description;
        Status = change.Status;
        UpdatedByAccountId = context.ActorAccountId;
        UpdatedAt = context.OccurredAt;
        Version++;
    }
}
