using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestWorkspaceTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveWorkspaceWithInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var workspace = new TestWorkspace(
            Guid.NewGuid(), "Quality", "QA", "Workspace", actorId, CreatedAt);

        Assert.Equal("Quality", workspace.Name);
        Assert.Equal("QA", workspace.Prefix);
        Assert.Equal(TestAssetStatus.Active, workspace.Status);
        Assert.Equal(actorId, workspace.CreatedByAccountId);
        Assert.Equal(actorId, workspace.UpdatedByAccountId);
        Assert.Equal(1, workspace.Version);
    }

    [Fact]
    public void Update_ChangesEditableFieldsWithoutChangingPrefix()
    {
        var workspace = CreateWorkspace();
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        workspace.Update(
            "Updated quality",
            "Updated",
            TestAssetStatus.Inactive,
            actorId,
            updatedAt);

        Assert.Equal("Updated quality", workspace.Name);
        Assert.Equal("Updated", workspace.Description);
        Assert.Equal(TestAssetStatus.Inactive, workspace.Status);
        Assert.Equal("QA", workspace.Prefix);
        Assert.Equal(actorId, workspace.UpdatedByAccountId);
        Assert.Equal(updatedAt, workspace.UpdatedAt);
        Assert.Equal(2, workspace.Version);
    }

    private static TestWorkspace CreateWorkspace()
    {
        return new TestWorkspace(
            Guid.NewGuid(), "Quality", "QA", null, Guid.NewGuid(), CreatedAt);
    }
}
