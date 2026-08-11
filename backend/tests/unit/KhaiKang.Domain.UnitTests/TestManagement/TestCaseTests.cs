using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestCaseTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveCaseWithInitialAuditMetadata()
    {
        var actorId = Guid.NewGuid();

        var testCase = CreateCase(actorId);

        Assert.Equal("active", testCase.Status);
        Assert.Equal("Original title", testCase.Title);
        Assert.Equal(actorId, testCase.CreatedByAccountId);
        Assert.Equal(actorId, testCase.UpdatedByAccountId);
        Assert.Equal(1, testCase.Version);
    }

    [Fact]
    public void AddStepAndClearSteps_ManageTheStepCollection()
    {
        var testCase = CreateCase();
        var step = new TestStep(
            Guid.NewGuid(), testCase.Id, 1, "Action", "Expected", Guid.NewGuid(), CreatedAt);

        testCase.AddStep(step);

        Assert.Same(step, Assert.Single(testCase.Steps));

        testCase.ClearSteps();

        Assert.Empty(testCase.Steps);
    }

    [Fact]
    public void Update_ChangesEditableContentAndAuditMetadata()
    {
        var testCase = CreateCase();
        var suiteId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddHours(1);

        testCase.Update(
            suiteId,
            "Updated title",
            "Updated description",
            "Updated preconditions",
            "Updated expected result",
            5,
            "inactive",
            actorId,
            updatedAt);

        Assert.Equal(suiteId, testCase.TestSuiteId);
        Assert.Equal("Updated title", testCase.Title);
        Assert.Equal("Updated description", testCase.Description);
        Assert.Equal("Updated preconditions", testCase.Preconditions);
        Assert.Equal("Updated expected result", testCase.OverallExpectedResult);
        Assert.Equal(5, testCase.SortOrder);
        Assert.Equal("inactive", testCase.Status);
        Assert.Equal(actorId, testCase.UpdatedByAccountId);
        Assert.Equal(2, testCase.Version);
    }

    private static TestCase CreateCase(Guid? actorId = null)
    {
        return new TestCase(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Original title",
            "Original description",
            "Original preconditions",
            "Original expected result",
            1,
            actorId ?? Guid.NewGuid(),
            CreatedAt);
    }
}
