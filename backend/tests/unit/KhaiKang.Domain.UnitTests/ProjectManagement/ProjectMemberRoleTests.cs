using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectMemberRoleTests
{
    [Fact]
    public void Create_CapturesRoleMappingAndInitialAuditMetadata()
    {
        var creation = new ProjectMemberRoleCreation
        {
            Id = Guid.NewGuid(),
            ProjectMemberId = Guid.NewGuid(),
            ProjectRoleId = Guid.NewGuid(),
        };
        var actorId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 11, 9, 0, 0, TimeSpan.Zero);

        var mapping = ProjectMemberRole.Create(
            creation,
            new ChangeContext(actorId, createdAt));

        Assert.Equal(creation.Id, mapping.Id);
        Assert.Equal(creation.ProjectMemberId, mapping.ProjectMemberId);
        Assert.Equal(creation.ProjectRoleId, mapping.ProjectRoleId);
        Assert.Equal(createdAt, mapping.CreatedAt);
        Assert.Equal(actorId, mapping.CreatedByAccountId);
        Assert.Equal(actorId, mapping.UpdatedByAccountId);
        Assert.Equal(1, mapping.Version);
    }
}
