using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Domain.UnitTests.ProjectManagement;

public sealed class ProjectManagementApplicationResultTests
{
    [Fact]
    public void ProjectResults_KeepSuccessfulPayloadsAndRejectSucceededFailures()
    {
        var project = CreateProject();

        var created = CreateProjectResult.Success(project);
        var updated = UpdateProjectResult.Success(project);

        Assert.Equal(CreateProjectOutcome.Succeeded, created.Outcome);
        Assert.Same(project, created.Project);
        Assert.Equal(UpdateProjectOutcome.Succeeded, updated.Outcome);
        Assert.Same(project, updated.Project);
        Assert.Throws<ArgumentNullException>(() => CreateProjectResult.Success(null!));
        Assert.Throws<ArgumentNullException>(() => UpdateProjectResult.Success(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateProjectResult.Failure(CreateProjectOutcome.Succeeded));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => UpdateProjectResult.Failure(UpdateProjectOutcome.Succeeded));
    }

    [Fact]
    public void IssueMutationResult_RequiresIssueOnlyForSuccess()
    {
        var issue = CreateIssue();

        var success = IssueMutationResult.Success(issue);
        var failure = IssueMutationResult.Failure(IssueMutationOutcome.Forbidden);

        Assert.Equal(IssueMutationOutcome.Succeeded, success.Outcome);
        Assert.Same(issue, success.Issue);
        Assert.Equal(IssueMutationOutcome.Forbidden, failure.Outcome);
        Assert.Null(failure.Issue);
        Assert.Throws<ArgumentNullException>(() => IssueMutationResult.Success(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IssueMutationResult.Failure(IssueMutationOutcome.Succeeded));
    }

    [Fact]
    public void RelationAndProjectMemberResults_EnforceTheirPayloadInvariants()
    {
        var relation = CreateRelation();
        var member = CreateMember();

        var created = IssueRelationMutationResult.Created(relation);
        var deleted = IssueRelationMutationResult.Deleted();
        var memberAdded = AddProjectMemberResult.Success(member);
        var memberUpdated = UpdateProjectMemberRolesResult.Success(member);

        Assert.Same(relation, created.Relation);
        Assert.Null(deleted.Relation);
        Assert.Same(member, memberAdded.Member);
        Assert.Same(member, memberUpdated.Member);
        Assert.All(
            new[] { created.Outcome, deleted.Outcome },
            outcome => Assert.Equal(IssueRelationMutationOutcome.Succeeded, outcome));
        Assert.Equal(AddProjectMemberOutcome.Succeeded, memberAdded.Outcome);
        Assert.Equal(UpdateProjectMemberRolesOutcome.Succeeded, memberUpdated.Outcome);
        Assert.Throws<ArgumentNullException>(() => IssueRelationMutationResult.Created(null!));
        Assert.Throws<ArgumentNullException>(() => AddProjectMemberResult.Success(null!));
        Assert.Throws<ArgumentNullException>(() => UpdateProjectMemberRolesResult.Success(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IssueRelationMutationResult.Failure(IssueRelationMutationOutcome.Succeeded));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AddProjectMemberResult.Failure(AddProjectMemberOutcome.Succeeded));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => UpdateProjectMemberRolesResult.Failure(
                UpdateProjectMemberRolesOutcome.Succeeded));
    }

    [Theory]
    [InlineData(AddProjectMemberOutcome.NotFound)]
    [InlineData(AddProjectMemberOutcome.AccountNotFound)]
    [InlineData(AddProjectMemberOutcome.Forbidden)]
    [InlineData(AddProjectMemberOutcome.AlreadyMember)]
    [InlineData(AddProjectMemberOutcome.InvalidRoles)]
    public void AddProjectMemberFailure_HasNoMemberPayload(AddProjectMemberOutcome outcome)
    {
        var result = AddProjectMemberResult.Failure(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Member);
    }

    [Theory]
    [InlineData(UpdateProjectMemberRolesOutcome.NotFound)]
    [InlineData(UpdateProjectMemberRolesOutcome.Forbidden)]
    [InlineData(UpdateProjectMemberRolesOutcome.InvalidRoles)]
    [InlineData(UpdateProjectMemberRolesOutcome.LastOwner)]
    [InlineData(UpdateProjectMemberRolesOutcome.VersionConflict)]
    public void UpdateProjectMemberRolesFailure_HasNoMemberPayload(
        UpdateProjectMemberRolesOutcome outcome)
    {
        var result = UpdateProjectMemberRolesResult.Failure(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Member);
    }

    [Fact]
    public void AttachmentMutationResult_DistinguishesUploadFromDelete()
    {
        var attachment = CreateAttachment();

        var uploaded = IssueAttachmentMutationResult.Uploaded(attachment);
        var deleted = IssueAttachmentMutationResult.Deleted();
        var failure = IssueAttachmentMutationResult.Failure(IssueAttachmentOutcome.InvalidFile);

        Assert.Same(attachment, uploaded.Attachment);
        Assert.Null(deleted.Attachment);
        Assert.Null(failure.Attachment);
        Assert.Equal(IssueAttachmentOutcome.Succeeded, uploaded.Outcome);
        Assert.Equal(IssueAttachmentOutcome.Succeeded, deleted.Outcome);
        Assert.Equal(IssueAttachmentOutcome.InvalidFile, failure.Outcome);
        Assert.Throws<ArgumentNullException>(() => IssueAttachmentMutationResult.Uploaded(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IssueAttachmentMutationResult.Failure(IssueAttachmentOutcome.Succeeded));
    }

    [Fact]
    public void AttachmentContentResult_RequiresCompleteContentMetadataForSuccess()
    {
        using var stream = new MemoryStream([1, 2, 3]);

        var success = IssueAttachmentContentResult.Success(
            stream,
            "image/png",
            "evidence.png");
        var failure = IssueAttachmentContentResult.Failure(
            IssueAttachmentOutcome.StorageUnavailable);

        Assert.Same(stream, success.Content);
        Assert.Equal("image/png", success.ContentType);
        Assert.Equal("evidence.png", success.FileName);
        Assert.Equal(IssueAttachmentOutcome.Succeeded, success.Outcome);
        Assert.Null(failure.Content);
        Assert.Null(failure.ContentType);
        Assert.Null(failure.FileName);
        Assert.Throws<ArgumentNullException>(
            () => IssueAttachmentContentResult.Success(null!, "image/png", "evidence.png"));
        Assert.Throws<ArgumentNullException>(
            () => IssueAttachmentContentResult.Success(stream, null!, "evidence.png"));
        Assert.Throws<ArgumentNullException>(
            () => IssueAttachmentContentResult.Success(stream, "image/png", null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IssueAttachmentContentResult.Failure(IssueAttachmentOutcome.Succeeded));
    }

    private static ProjectResponse CreateProject()
    {
        return new ProjectResponse
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Name = "Test Project",
            Description = null,
            Status = "active",
            CurrentUserRoles = ["Owner"],
            CurrentUserPermissions = ["project.read"],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1,
        };
    }

    private static IssueResponse CreateIssue()
    {
        return new IssueResponse
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            IssueNo = 1,
            Key = "TEST-1",
            Title = "Test issue",
            TypeCode = "task",
            TypeName = "Task",
            StatusCode = "created",
            StatusName = "Created",
            PriorityCode = "medium",
            PriorityName = "Medium",
            ReporterAccountId = Guid.NewGuid(),
            ReporterUsername = "owner",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1,
        };
    }

    private static IssueRelationResponse CreateRelation()
    {
        var issue = new IssueRelationIssueResponse
        {
            Id = Guid.NewGuid(),
            IssueNo = 1,
            Key = "TEST-1",
            Title = "Test issue",
            TypeCode = "task",
            StatusCode = "created",
        };
        return new IssueRelationResponse
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RelationTypeCode = "related",
            ForwardLabel = "Relates to",
            ReverseLabel = "Relates to",
            DirectionKind = "symmetric",
            SourceIssue = issue,
            TargetIssue = issue,
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 1,
        };
    }

    private static ProjectMemberResponse CreateMember()
    {
        return new ProjectMemberResponse
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Username = "reviewer",
            Status = "active",
            RoleCodes = ["reviewer"],
            JoinedAt = DateTimeOffset.UtcNow,
            Version = 1,
        };
    }

    private static IssueAttachmentResponse CreateAttachment()
    {
        return new IssueAttachmentResponse
        {
            Id = Guid.NewGuid(),
            IssueId = Guid.NewGuid(),
            OriginalFileName = "evidence.png",
            ContentType = "image/png",
            FileSize = 3,
            FileHash = new string('a', 64),
            UploadedByAccountId = Guid.NewGuid(),
            UploadedByUsername = "owner",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
