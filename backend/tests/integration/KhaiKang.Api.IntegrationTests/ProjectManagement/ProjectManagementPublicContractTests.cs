using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ProjectManagementPublicContractTests
{
    [Fact]
    public void ProjectRequests_UseCanonicalJsonShapes()
    {
        var create = new CreateProjectRequest(
            code: "WEB",
            name: "Web Project")
        {
            Description = null,
        };
        var update = new UpdateProjectRequest(
            name: "Web Project",
            status: "active",
            version: 3)
        {
            Description = "Customer portal",
        };

        AssertPropertyNames(create, "code", "description", "name");
        AssertPropertyNames(update, "description", "name", "status", "version");
    }

    [Fact]
    public void IssueActionRequests_UseCanonicalJsonShapes()
    {
        var status = new UpdateIssueStatusRequest(
            statusCode: "in_progress",
            version: 4);
        var assignee = new UpdateIssueAssigneeRequest
        {
            AssigneeAccountId = null,
            Version = 5,
        };
        var relation = new CreateIssueRelationRequest(
            relationTypeCode: "tests",
            relatedIssueId: Guid.Parse("3be31a85-3a75-42fb-950c-45181e9f8732"),
            direction: "forward");

        AssertPropertyNames(status, "statusCode", "version");
        AssertPropertyNames(assignee, "assigneeAccountId", "version");
        AssertPropertyNames(relation, "direction", "relatedIssueId", "relationTypeCode");
    }

    [Fact]
    public void UpdateIssueAssignee_RequiresNullableAssigneePropertyToBePresent()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<UpdateIssueAssigneeRequest>(
                """{"version":5}""",
                JsonSerializerOptions.Web));
    }

    [Fact]
    public void ProjectResponses_UseCanonicalJsonShapes()
    {
        var project = new ProjectResponse
        {
            Id = Guid.Parse("f0b15346-43a5-4cf1-8935-1b1126eb6023"),
            Code = "WEB",
            Name = "Web Project",
            Description = null,
            Status = "active",
            CurrentUserRoles = ["owner"],
            CurrentUserPermissions = ["project.read"],
            CreatedAt = DateTimeOffset.Parse("2026-08-12T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-08-12T01:00:00Z"),
            Version = 2,
        };
        var member = new ProjectMemberResponse
        {
            Id = Guid.Parse("32a1db62-f98b-4f75-8e47-f09f9064e586"),
            AccountId = Guid.Parse("3e489257-6fd5-4465-aef1-55e49d3019a8"),
            Username = "owner",
            Status = "active",
            RoleCodes = ["owner"],
            JoinedAt = DateTimeOffset.Parse("2026-08-12T00:00:00Z"),
            Version = 1,
        };
        var role = new ProjectRoleResponse
        {
            Code = "owner",
            Name = "Owner",
            Description = "Project owner",
        };

        AssertPropertyNames(
            project,
            "code",
            "createdAt",
            "currentUserPermissions",
            "currentUserRoles",
            "description",
            "id",
            "name",
            "status",
            "updatedAt",
            "version");
        AssertPropertyNames(
            member,
            "accountId",
            "id",
            "joinedAt",
            "roleCodes",
            "status",
            "username",
            "version");
        AssertPropertyNames(role, "code", "description", "name");
    }

    [Fact]
    public void IssueResponses_UseCanonicalJsonShapes()
    {
        var issue = CreateIssueResponse();
        var option = new IssueOptionResponse
        {
            Code = "created",
            Name = "Created",
            Description = null,
            Category = "todo",
        };
        var metadata = new IssueMetadataResponse
        {
            Types = [option],
            Statuses = [option],
            Priorities = [option],
        };

        AssertPropertyNames(
            issue,
            "assigneeAccountId",
            "assigneeUsername",
            "completedAt",
            "completionSummary",
            "createdAt",
            "definitionOfDone",
            "description",
            "id",
            "issueNo",
            "key",
            "priorityCode",
            "priorityName",
            "projectId",
            "reporterAccountId",
            "reporterUsername",
            "statusCode",
            "statusName",
            "title",
            "typeCode",
            "typeName",
            "updatedAt",
            "userStory",
            "version");
        AssertPropertyNames(option, "category", "code", "description", "name");
        AssertPropertyNames(metadata, "priorities", "statuses", "types");
    }

    [Fact]
    public void RelationAndAttachmentResponses_UseCanonicalJsonShapes()
    {
        var source = new IssueRelationIssueResponse
        {
            Id = Guid.Parse("46559bfd-b2c7-4df4-91dc-b68580db0506"),
            IssueNo = 1,
            Key = "WEB-1",
            Title = "Requirement",
            TypeCode = "story",
            StatusCode = "created",
        };
        var relationType = new IssueRelationTypeResponse
        {
            Id = Guid.Parse("3a6df9ad-c0cf-4e4b-a25f-b5bafee27877"),
            Code = "tests",
            ForwardLabel = "Tests",
            ReverseLabel = "Tested by",
            DirectionKind = "directed",
        };
        var relation = new IssueRelationResponse
        {
            Id = Guid.Parse("e7223c26-1057-470e-9844-5dadf7c5546e"),
            ProjectId = Guid.Parse("f0b15346-43a5-4cf1-8935-1b1126eb6023"),
            RelationTypeCode = "tests",
            ForwardLabel = "Tests",
            ReverseLabel = "Tested by",
            DirectionKind = "directed",
            SourceIssue = source,
            TargetIssue = source,
            CreatedAt = DateTimeOffset.Parse("2026-08-12T00:00:00Z"),
            Version = 1,
        };
        var attachment = new IssueAttachmentResponse
        {
            Id = Guid.Parse("390a699a-8f82-4ce2-af64-53b23ce53f54"),
            IssueId = source.Id,
            OriginalFileName = "evidence.png",
            ContentType = "image/png",
            FileSize = 1024,
            FileHash = new string('a', 64),
            UploadedByAccountId = Guid.Parse("3e489257-6fd5-4465-aef1-55e49d3019a8"),
            UploadedByUsername = "owner",
            CreatedAt = DateTimeOffset.Parse("2026-08-12T00:00:00Z"),
        };

        AssertPropertyNames(
            relationType,
            "code",
            "directionKind",
            "forwardLabel",
            "id",
            "reverseLabel");
        AssertPropertyNames(
            source,
            "id",
            "issueNo",
            "key",
            "statusCode",
            "title",
            "typeCode");
        AssertPropertyNames(
            relation,
            "createdAt",
            "directionKind",
            "forwardLabel",
            "id",
            "projectId",
            "relationTypeCode",
            "reverseLabel",
            "sourceIssue",
            "targetIssue",
            "version");
        AssertPropertyNames(
            attachment,
            "contentType",
            "createdAt",
            "fileHash",
            "fileSize",
            "id",
            "issueId",
            "originalFileName",
            "uploadedByAccountId",
            "uploadedByUsername");
    }

    private static IssueResponse CreateIssueResponse()
    {
        return new IssueResponse
        {
            Id = Guid.Parse("46559bfd-b2c7-4df4-91dc-b68580db0506"),
            ProjectId = Guid.Parse("f0b15346-43a5-4cf1-8935-1b1126eb6023"),
            IssueNo = 1,
            Key = "WEB-1",
            Title = "Requirement",
            Description = null,
            UserStory = null,
            DefinitionOfDone = null,
            CompletionSummary = null,
            TypeCode = "story",
            TypeName = "Story",
            StatusCode = "created",
            StatusName = "Created",
            PriorityCode = "medium",
            PriorityName = "Medium",
            ReporterAccountId = Guid.Parse("3e489257-6fd5-4465-aef1-55e49d3019a8"),
            ReporterUsername = "owner",
            AssigneeAccountId = null,
            AssigneeUsername = null,
            CompletedAt = null,
            CreatedAt = DateTimeOffset.Parse("2026-08-12T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-08-12T01:00:00Z"),
            Version = 2,
        };
    }

    private static void AssertPropertyNames<T>(T value, params string[] expectedNames)
    {
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(value, JsonSerializerOptions.Web));

        Assert.Equal(
            expectedNames.Order(StringComparer.Ordinal),
            document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }
}
