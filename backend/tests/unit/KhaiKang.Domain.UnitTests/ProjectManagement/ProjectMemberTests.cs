using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectMemberTests
{
    private static readonly DateTimeOffset JoinedAt =
        new(2026, 7, 24, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CapturesMembershipAndInitialAuditMetadata()
    {
        var creation = new ProjectMemberCreation
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
        };
        var actorId = Guid.NewGuid();

        var member = ProjectMember.Create(
            creation,
            new ChangeContext(actorId, JoinedAt));

        Assert.Equal(creation.Id, member.Id);
        Assert.Equal(creation.ProjectId, member.ProjectId);
        Assert.Equal(creation.AccountId, member.AccountId);
        Assert.Equal(ProjectMemberStatus.Active, member.Status);
        Assert.Equal(JoinedAt, member.JoinedAt);
        Assert.Equal(JoinedAt, member.CreatedAt);
        Assert.Equal(actorId, member.CreatedByAccountId);
        Assert.Equal(actorId, member.UpdatedByAccountId);
        Assert.Equal(1, member.Version);
    }

    [Fact]
    public void RemoveAndRestore_PreserveLifecycleHistory()
    {
        var member = CreateMember();
        var removeActorId = Guid.NewGuid();
        var removedAt = JoinedAt.AddDays(1);

        member.Remove(new ChangeContext(removeActorId, removedAt));

        Assert.Equal(ProjectMemberStatus.Removed, member.Status);
        Assert.Equal(removedAt, member.RemovedAt);
        Assert.Equal(removeActorId, member.UpdatedByAccountId);
        Assert.Equal(2, member.Version);

        var restoreActorId = Guid.NewGuid();
        var restoredAt = JoinedAt.AddDays(2);
        member.Restore(new ChangeContext(restoreActorId, restoredAt));

        Assert.Equal(ProjectMemberStatus.Active, member.Status);
        Assert.Null(member.RemovedAt);
        Assert.Equal(restoredAt, member.JoinedAt);
        Assert.Equal(restoreActorId, member.UpdatedByAccountId);
        Assert.Equal(3, member.Version);
    }

    [Fact]
    public void RecordRoleChange_UpdatesAuditMetadataAndVersion()
    {
        var member = CreateMember();
        var actorId = Guid.NewGuid();
        var occurredAt = JoinedAt.AddHours(1);

        member.RecordRoleChange(new ChangeContext(actorId, occurredAt));

        Assert.Equal(actorId, member.UpdatedByAccountId);
        Assert.Equal(occurredAt, member.UpdatedAt);
        Assert.Equal(2, member.Version);
    }

    private static ProjectMember CreateMember()
    {
        return ProjectMember.Create(
            new ProjectMemberCreation
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
            },
            new ChangeContext(Guid.NewGuid(), JoinedAt));
    }
}
