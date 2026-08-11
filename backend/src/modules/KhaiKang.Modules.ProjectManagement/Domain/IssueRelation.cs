namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueRelation : AuditableEntity
{
    private IssueRelation() { }

    private IssueRelation(IssueRelationCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        ProjectId = creation.ProjectId;
        RelationTypeId = creation.RelationTypeId;
        SourceIssueId = creation.SourceIssueId;
        TargetIssueId = creation.TargetIssueId;
        InitializeAudit(context);
    }

    public static IssueRelation Create(IssueRelationCreation creation, ChangeContext context)
    {
        return new IssueRelation(creation, context);
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
    public void Delete(ChangeContext context)
    {
        IsDeleted = true;
        DeletedAt = context.OccurredAt;
        DeletedByAccountId = context.ActorAccountId;
        RecordChange(context);
    }
}
