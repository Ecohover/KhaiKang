using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestSuiteTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveSuiteWithHierarchyAndSortOrder()
    {
        var parentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var suite = new TestSuite(
            Guid.NewGuid(),
            Guid.NewGuid(),
            parentId,
            "Regression",
            "Scope",
            3,
            actorId,
            CreatedAt);

        Assert.Equal(parentId, suite.ParentId);
        Assert.Equal("Regression", suite.Name);
        Assert.Equal(3, suite.SortOrder);
        Assert.Equal(TestAssetStatus.Active, suite.Status);
        Assert.Equal(actorId, suite.CreatedByAccountId);
        Assert.Equal(1, suite.Version);
    }

    [Fact]
    public void Update_ChangesHierarchyContentOrderAndStatus()
    {
        var suite = CreateSuite();
        var parentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        suite.Update(
            parentId,
            "Updated suite",
            "Updated scope",
            8,
            TestAssetStatus.Inactive,
            actorId,
            updatedAt);

        Assert.Equal(parentId, suite.ParentId);
        Assert.Equal("Updated suite", suite.Name);
        Assert.Equal("Updated scope", suite.Description);
        Assert.Equal(8, suite.SortOrder);
        Assert.Equal(TestAssetStatus.Inactive, suite.Status);
        Assert.Equal(actorId, suite.UpdatedByAccountId);
        Assert.Equal(2, suite.Version);
    }

    private static TestSuite CreateSuite()
    {
        return new TestSuite(
            Guid.NewGuid(), Guid.NewGuid(), null, "Regression", null, 1, Guid.NewGuid(), CreatedAt);
    }
}
