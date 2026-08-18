using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestRunItemTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesCaseContentAsAnExecutionSnapshot()
    {
        var source = CreateCase();

        var item = new TestRunItem(
            Guid.NewGuid(), Guid.NewGuid(), source, 3, Guid.NewGuid(), CreatedAt);

        Assert.Equal(source.Id, item.TestCaseId);
        Assert.Equal(3, item.SortOrder);
        Assert.Equal("Original title", item.CaseTitle);
        Assert.Equal("Original description", item.CaseDescription);
        Assert.Equal("Original preconditions", item.Preconditions);
        Assert.Equal("Original expected result", item.OverallExpectedResult);
        Assert.Equal(TestResultStatus.NotRun, item.ResultStatus);
        Assert.Null(item.ExecutedAt);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void SourceCaseUpdate_DoesNotChangeExistingExecutionSnapshot()
    {
        var source = CreateCase();
        var item = new TestRunItem(
            Guid.NewGuid(), Guid.NewGuid(), source, 1, Guid.NewGuid(), CreatedAt);

        source.Update(
            Guid.NewGuid(),
            "Changed title",
            "Changed description",
            "Changed preconditions",
            "Changed expected result",
            2,
            TestAssetStatus.Active,
            Guid.NewGuid(),
            CreatedAt.AddHours(1));

        Assert.Equal("Original title", item.CaseTitle);
        Assert.Equal("Original description", item.CaseDescription);
        Assert.Equal("Original preconditions", item.Preconditions);
        Assert.Equal("Original expected result", item.OverallExpectedResult);
    }

    [Fact]
    public void Record_ResultCapturesExecutorAndAuditMetadata()
    {
        var item = CreateRunItem();
        var actorId = Guid.NewGuid();
        var executedAt = CreatedAt.AddMinutes(15);

        item.Record(TestResultStatus.Failed, "Observed error", actorId, executedAt);

        Assert.Equal(TestResultStatus.Failed, item.ResultStatus);
        Assert.Equal("Observed error", item.ActualResult);
        Assert.Equal(actorId, item.ExecutedByAccountId);
        Assert.Equal(executedAt, item.ExecutedAt);
        Assert.Equal(actorId, item.UpdatedByAccountId);
        Assert.Equal(2, item.Version);
    }

    [Fact]
    public void Record_NotRunClearsExecutionMetadata()
    {
        var item = CreateRunItem();
        item.Record(
            TestResultStatus.Passed,
            "Passed",
            Guid.NewGuid(),
            CreatedAt.AddMinutes(10));
        var actorId = Guid.NewGuid();

        item.Record(TestResultStatus.NotRun, null, actorId, CreatedAt.AddMinutes(20));

        Assert.Equal(TestResultStatus.NotRun, item.ResultStatus);
        Assert.Null(item.ActualResult);
        Assert.Null(item.ExecutedByAccountId);
        Assert.Null(item.ExecutedAt);
        Assert.Equal(actorId, item.UpdatedByAccountId);
        Assert.Equal(3, item.Version);
    }

    private static TestRunItem CreateRunItem()
    {
        return new TestRunItem(
            Guid.NewGuid(), Guid.NewGuid(), CreateCase(), 1, Guid.NewGuid(), CreatedAt);
    }

    private static TestCase CreateCase()
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
            Guid.NewGuid(),
            CreatedAt);
    }
}
