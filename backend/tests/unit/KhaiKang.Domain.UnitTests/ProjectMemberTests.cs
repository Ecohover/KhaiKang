using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class ProjectMemberTests
{
    private static readonly DateTimeOffset JoinedAt =
        new(2026, 7, 24, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RemoveAndRestore_PreserveLifecycleHistory()
    {
        var member = new ProjectMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            JoinedAt,
            Guid.NewGuid());
        var removeActorId = Guid.NewGuid();
        var removedAt = JoinedAt.AddDays(1);

        member.Remove(removeActorId, removedAt);

        Assert.Equal("removed", member.Status);
        Assert.Equal(removedAt, member.RemovedAt);
        Assert.Equal(removeActorId, member.UpdatedByAccountId);
        Assert.Equal(2, member.Version);

        var restoreActorId = Guid.NewGuid();
        var restoredAt = JoinedAt.AddDays(2);
        member.Restore(restoreActorId, restoredAt);

        Assert.Equal("active", member.Status);
        Assert.Null(member.RemovedAt);
        Assert.Equal(restoredAt, member.JoinedAt);
        Assert.Equal(restoreActorId, member.UpdatedByAccountId);
        Assert.Equal(3, member.Version);
    }

    [Fact]
    public void RecordRoleChange_UpdatesAuditMetadataAndVersion()
    {
        var member = new ProjectMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            JoinedAt,
            Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var occurredAt = JoinedAt.AddHours(1);

        member.RecordRoleChange(actorId, occurredAt);

        Assert.Equal(actorId, member.UpdatedByAccountId);
        Assert.Equal(occurredAt, member.UpdatedAt);
        Assert.Equal(2, member.Version);
    }
}
