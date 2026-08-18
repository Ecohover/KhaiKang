using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestTagTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveTagWithInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var tag = new TestTag(Guid.NewGuid(), "smoke", "Fast tests", actorId, CreatedAt);

        Assert.Equal("smoke", tag.Name);
        Assert.Equal(TestAssetStatus.Active, tag.Status);
        Assert.Equal(actorId, tag.CreatedByAccountId);
        Assert.Equal(actorId, tag.UpdatedByAccountId);
        Assert.Equal(1, tag.Version);
    }

    [Fact]
    public void Update_ChangesContentStatusAndAuditMetadata()
    {
        var tag = new TestTag(Guid.NewGuid(), "smoke", null, Guid.NewGuid(), CreatedAt);
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        tag.Update(
            "regression",
            "Complete tests",
            TestAssetStatus.Inactive,
            actorId,
            updatedAt);

        Assert.Equal("regression", tag.Name);
        Assert.Equal("Complete tests", tag.Description);
        Assert.Equal(TestAssetStatus.Inactive, tag.Status);
        Assert.Equal(actorId, tag.UpdatedByAccountId);
        Assert.Equal(updatedAt, tag.UpdatedAt);
        Assert.Equal(2, tag.Version);
    }
}
