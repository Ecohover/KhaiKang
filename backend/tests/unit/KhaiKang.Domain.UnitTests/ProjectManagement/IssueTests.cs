using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class IssueTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 24, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CapturesReporterAndInitialAuditMetadata()
    {
        var reporterId = Guid.NewGuid();

        var issue = CreateIssue(reporterId: reporterId);

        Assert.Equal(reporterId, issue.ReporterAccountId);
        Assert.Equal(reporterId, issue.CreatedByAccountId);
        Assert.Equal(reporterId, issue.UpdatedByAccountId);
        Assert.Null(issue.CompletedAt);
        Assert.Equal(1, issue.Version);
    }

    [Fact]
    public void Constructor_CapturesIdentityContentClassificationAndAssignment()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var priorityId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var issue = new Issue(
            id,
            projectId,
            42,
            "Initial title",
            "Initial description",
            "Initial user story",
            "Initial definition",
            typeId,
            statusId,
            priorityId,
            reporterId,
            assigneeId,
            CreatedAt);

        Assert.Equal(id, issue.Id);
        Assert.Equal(projectId, issue.ProjectId);
        Assert.Equal(42, issue.IssueNo);
        Assert.Equal("Initial title", issue.Title);
        Assert.Equal("Initial description", issue.Description);
        Assert.Equal("Initial user story", issue.UserStory);
        Assert.Equal("Initial definition", issue.DefinitionOfDone);
        Assert.Equal(typeId, issue.IssueTypeId);
        Assert.Equal(statusId, issue.IssueStatusId);
        Assert.Equal(priorityId, issue.IssuePriorityId);
        Assert.Equal(reporterId, issue.ReporterAccountId);
        Assert.Equal(assigneeId, issue.AssigneeAccountId);
        Assert.Equal(CreatedAt, issue.CreatedAt);
        Assert.Equal(CreatedAt, issue.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_ToCompleted_SetsCompletedAt()
    {
        var issue = CreateIssue();
        var actorId = Guid.NewGuid();
        var occurredAt = CreatedAt.AddHours(1);
        var completedStatusId = Guid.NewGuid();

        issue.ChangeStatus(completedStatusId, "completed", actorId, occurredAt);

        Assert.Equal(completedStatusId, issue.IssueStatusId);
        Assert.Equal(occurredAt, issue.CompletedAt);
        Assert.Equal(actorId, issue.UpdatedByAccountId);
        Assert.Equal(occurredAt, issue.UpdatedAt);
        Assert.Equal(2, issue.Version);
    }

    [Fact]
    public void ChangeStatus_AwayFromCompleted_ClearsCompletedAt()
    {
        var issue = CreateIssue();
        issue.ChangeStatus(
            Guid.NewGuid(),
            "completed",
            Guid.NewGuid(),
            CreatedAt.AddHours(1));

        issue.ChangeStatus(
            Guid.NewGuid(),
            "in_progress",
            Guid.NewGuid(),
            CreatedAt.AddHours(2));

        Assert.Null(issue.CompletedAt);
        Assert.Equal(3, issue.Version);
    }

    [Fact]
    public void UpdateDetails_ChangesContentWithoutChangingAssignee()
    {
        var assigneeId = Guid.NewGuid();
        var issue = CreateIssue(assigneeId: assigneeId);
        var actorId = Guid.NewGuid();
        var occurredAt = CreatedAt.AddHours(1);
        var typeId = Guid.NewGuid();
        var priorityId = Guid.NewGuid();
        var originalStatusId = issue.IssueStatusId;
        var originalReporterId = issue.ReporterAccountId;

        issue.UpdateDetails(
            "Updated title",
            "Updated description",
            "Updated user story",
            "Updated definition",
            "Updated result",
            typeId,
            priorityId,
            actorId,
            occurredAt);

        Assert.Equal("Updated title", issue.Title);
        Assert.Equal("Updated description", issue.Description);
        Assert.Equal("Updated user story", issue.UserStory);
        Assert.Equal("Updated definition", issue.DefinitionOfDone);
        Assert.Equal("Updated result", issue.CompletionSummary);
        Assert.Equal(typeId, issue.IssueTypeId);
        Assert.Equal(priorityId, issue.IssuePriorityId);
        Assert.Equal(assigneeId, issue.AssigneeAccountId);
        Assert.Equal(originalStatusId, issue.IssueStatusId);
        Assert.Equal(originalReporterId, issue.ReporterAccountId);
        Assert.Equal(actorId, issue.UpdatedByAccountId);
        Assert.Equal(occurredAt, issue.UpdatedAt);
        Assert.Equal(2, issue.Version);
    }

    [Fact]
    public void ChangeAssignee_SupportsAssignAndUnassign()
    {
        var issue = CreateIssue();
        var assigneeId = Guid.NewGuid();
        var unassignActorId = Guid.NewGuid();
        var unassignedAt = CreatedAt.AddMinutes(20);

        issue.ChangeAssignee(
            assigneeId,
            Guid.NewGuid(),
            CreatedAt.AddMinutes(10));
        issue.ChangeAssignee(
            null,
            unassignActorId,
            unassignedAt);

        Assert.Null(issue.AssigneeAccountId);
        Assert.Equal(unassignActorId, issue.UpdatedByAccountId);
        Assert.Equal(unassignedAt, issue.UpdatedAt);
        Assert.Equal(3, issue.Version);
    }

    private static Issue CreateIssue(
        Guid? reporterId = null,
        Guid? assigneeId = null)
    {
        return new Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Initial title",
            null,
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            reporterId ?? Guid.NewGuid(),
            assigneeId,
            CreatedAt);
    }
}
