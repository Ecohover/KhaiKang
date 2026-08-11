namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelation
{
    private IssueRelation() { }

    public IssueRelation(
        Guid id,
        Guid projectId,
        Guid relationTypeId,
        Guid sourceIssueId,
        Guid targetIssueId,
        Guid actorAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        RelationTypeId = relationTypeId;
        SourceIssueId = sourceIssueId;
        TargetIssueId = targetIssueId;
        CreatedAt = createdAt;
        CreatedByAccountId = actorAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = actorAccountId;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public Guid RelationTypeId { get; private set; }
    public IssueRelationType RelationType { get; private set; } = null!;
    public Guid SourceIssueId { get; private set; }
    public Issue SourceIssue { get; private set; } = null!;
    public Guid TargetIssueId { get; private set; }
    public Issue TargetIssue { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedByAccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;

    public void Delete(Guid actorAccountId, DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedByAccountId = actorAccountId;
        UpdatedAt = deletedAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }
}
