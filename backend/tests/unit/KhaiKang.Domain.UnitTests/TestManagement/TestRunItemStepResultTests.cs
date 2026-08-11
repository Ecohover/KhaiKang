using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestRunItemStepResultTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesStepContentAsAnExecutionSnapshot()
    {
        var step = CreateStep();

        var result = new TestRunItemStepResult(
            Guid.NewGuid(), Guid.NewGuid(), step, Guid.NewGuid(), CreatedAt);

        Assert.Equal(2, result.StepNo);
        Assert.Equal("Perform action", result.Action);
        Assert.Equal("Expected outcome", result.ExpectedResult);
        Assert.Equal("not_run", result.ResultStatus);
        Assert.Null(result.ExecutedByAccountId);
        Assert.Null(result.ExecutedAt);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void Record_ResultCapturesExecutorAndAuditMetadata()
    {
        var result = CreateResult();
        var actorId = Guid.NewGuid();
        var executedAt = CreatedAt.AddMinutes(5);

        result.Record("passed", "Observed outcome", actorId, executedAt);

        Assert.Equal("passed", result.ResultStatus);
        Assert.Equal("Observed outcome", result.ActualResult);
        Assert.Equal(actorId, result.ExecutedByAccountId);
        Assert.Equal(executedAt, result.ExecutedAt);
        Assert.Equal(actorId, result.UpdatedByAccountId);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public void Record_NotRunClearsExecutionMetadata()
    {
        var result = CreateResult();
        result.Record("failed", "Failure", Guid.NewGuid(), CreatedAt.AddMinutes(5));

        result.Record("not_run", null, Guid.NewGuid(), CreatedAt.AddMinutes(10));

        Assert.Equal("not_run", result.ResultStatus);
        Assert.Null(result.ActualResult);
        Assert.Null(result.ExecutedByAccountId);
        Assert.Null(result.ExecutedAt);
        Assert.Equal(3, result.Version);
    }

    private static TestRunItemStepResult CreateResult()
    {
        return new TestRunItemStepResult(
            Guid.NewGuid(), Guid.NewGuid(), CreateStep(), Guid.NewGuid(), CreatedAt);
    }

    private static TestStep CreateStep()
    {
        return new TestStep(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            "Perform action",
            "Expected outcome",
            Guid.NewGuid(),
            CreatedAt);
    }
}
