using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class IssueRelationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesRelationAndInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();
        var relation = new IssueRelation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorId,
            CreatedAt);

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

        relation.Delete(actorId, deletedAt);

        Assert.True(relation.IsDeleted);
        Assert.Equal(deletedAt, relation.DeletedAt);
        Assert.Equal(actorId, relation.DeletedByAccountId);
        Assert.Equal(actorId, relation.UpdatedByAccountId);
        Assert.Equal(deletedAt, relation.UpdatedAt);
        Assert.Equal(2, relation.Version);
    }

    private static IssueRelation CreateRelation()
    {
        return new IssueRelation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt);
    }
}
