using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestWorkspaceMemberTests
{
    private static readonly DateTimeOffset JoinedAt =
        new(2026, 8, 11, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveMemberWithInitialRoleAndAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var member = new TestWorkspaceMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestWorkspaceRole.Tester,
            actorId,
            JoinedAt);

        Assert.Equal(TestWorkspaceRole.Tester, member.Role);
        Assert.Equal(TestWorkspaceMemberStatus.Active, member.Status);
        Assert.Equal(JoinedAt, member.JoinedAt);
        Assert.Null(member.RemovedAt);
        Assert.Equal(actorId, member.CreatedByAccountId);
        Assert.Equal(1, member.Version);
    }

    [Fact]
    public void ChangeRole_UpdatesRoleAndAuditMetadata()
    {
        var member = CreateMember();
        var actorId = Guid.NewGuid();
        var changedAt = JoinedAt.AddHours(1);

        member.ChangeRole(TestWorkspaceRole.Manager, actorId, changedAt);

        Assert.Equal(TestWorkspaceRole.Manager, member.Role);
        Assert.Equal(actorId, member.UpdatedByAccountId);
        Assert.Equal(changedAt, member.UpdatedAt);
        Assert.Equal(2, member.Version);
    }

    [Fact]
    public void RemoveAndRestore_PreservesMembershipLifecycle()
    {
        var member = CreateMember();
        var removeActorId = Guid.NewGuid();
        var removedAt = JoinedAt.AddHours(1);

        member.Remove(removeActorId, removedAt);

        Assert.Equal(TestWorkspaceMemberStatus.Removed, member.Status);
        Assert.Equal(removedAt, member.RemovedAt);
        Assert.Equal(removeActorId, member.UpdatedByAccountId);
        Assert.Equal(2, member.Version);

        var restoreActorId = Guid.NewGuid();
        var restoredAt = JoinedAt.AddHours(2);
        member.Restore(TestWorkspaceRole.Viewer, restoreActorId, restoredAt);

        Assert.Equal(TestWorkspaceMemberStatus.Active, member.Status);
        Assert.Equal(TestWorkspaceRole.Viewer, member.Role);
        Assert.Null(member.RemovedAt);
        Assert.Equal(restoreActorId, member.UpdatedByAccountId);
        Assert.Equal(restoredAt, member.UpdatedAt);
        Assert.Equal(3, member.Version);
    }

    private static TestWorkspaceMember CreateMember()
    {
        return new TestWorkspaceMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestWorkspaceRole.Tester,
            Guid.NewGuid(),
            JoinedAt);
    }
}
