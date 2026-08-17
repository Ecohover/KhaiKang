using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests.ProjectManagement;

public sealed class CreateIssueCommandResultTests
{
    [Fact]
    public void Success_RequiresAndReturnsIssue()
    {
        var issue = CreateIssue();

        var result = CreateIssueCommandResult.Success(issue);

        Assert.Equal(CreateIssueCommandOutcome.Succeeded, result.Outcome);
        Assert.Same(issue, result.Issue);
    }

    [Fact]
    public void Success_RejectsNullIssue()
    {
        Assert.Throws<ArgumentNullException>(
            () => CreateIssueCommandResult.Success(null!));
    }

    [Fact]
    public void Failure_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateIssueCommandResult.Failure(CreateIssueCommandOutcome.Succeeded));
    }

    [Fact]
    public void Failure_ReturnsOutcomeWithoutIssue()
    {
        var result = CreateIssueCommandResult.Failure(CreateIssueCommandOutcome.Forbidden);

        Assert.Equal(CreateIssueCommandOutcome.Forbidden, result.Outcome);
        Assert.Null(result.Issue);
    }

    private static IssueDirectoryEntry CreateIssue()
    {
        return new IssueDirectoryEntry
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProjectCode = "TEST",
            ProjectStatus = ProjectStatus.Active,
            IssueNo = 1,
            Key = "TEST-1",
            Title = "Issue command result",
            TypeCode = "bug",
            StatusCode = "created",
        };
    }
}
