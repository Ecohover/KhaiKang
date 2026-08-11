using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class IssueRelationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CapturesRelationAndInitialAuditMetadata()
    {
        var creation = CreateRelationData();
        var actorId = Guid.NewGuid();
        var relation = IssueRelation.Create(
            creation,
            new ChangeContext(actorId, CreatedAt));

        Assert.Equal(creation.Id, relation.Id);
        Assert.Equal(creation.ProjectId, relation.ProjectId);
        Assert.Equal(creation.RelationTypeId, relation.RelationTypeId);
        Assert.Equal(creation.SourceIssueId, relation.SourceIssueId);
        Assert.Equal(creation.TargetIssueId, relation.TargetIssueId);
        Assert.False(relation.IsDeleted);
        Assert.Null(relation.DeletedAt);
        Assert.Null(relation.DeletedByAccountId);
        Assert.Equal(actorId, relation.CreatedByAccountId);
        Assert.Equal(actorId, relation.UpdatedByAccountId);
        Assert.Equal(CreatedAt, relation.CreatedAt);
        Assert.Equal(CreatedAt, relation.UpdatedAt);
        Assert.Equal(1, relation.Version);
    }

    [Fact]
    public void Delete_MarksRelationDeletedAndUpdatesAuditMetadata()
    {
        var relation = CreateRelation();
        var actorId = Guid.NewGuid();
        var deletedAt = CreatedAt.AddHours(1);

        relation.Delete(new ChangeContext(actorId, deletedAt));

        Assert.True(relation.IsDeleted);
        Assert.Equal(deletedAt, relation.DeletedAt);
        Assert.Equal(actorId, relation.DeletedByAccountId);
        Assert.Equal(actorId, relation.UpdatedByAccountId);
        Assert.Equal(deletedAt, relation.UpdatedAt);
        Assert.Equal(2, relation.Version);
    }

    private static IssueRelation CreateRelation()
    {
        return IssueRelation.Create(
            CreateRelationData(),
            new ChangeContext(Guid.NewGuid(), CreatedAt));
    }

    private static IssueRelationCreation CreateRelationData()
    {
        return new IssueRelationCreation
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RelationTypeId = Guid.NewGuid(),
            SourceIssueId = Guid.NewGuid(),
            TargetIssueId = Guid.NewGuid(),
        };
    }
}
