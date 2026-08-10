using System.Security.Claims;
using System.Text.RegularExpressions;
using KhaiKang.Modules.TestManagement.Application;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KhaiKang.Modules.TestManagement.Endpoints;

public static class TestManagementEndpointExtensions
{
    public static IEndpointRouteBuilder MapTestManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var workspaces = endpoints.MapGroup("/api/v1/test-workspaces")
            .WithTags("Test Management").RequireAuthorization();

        workspaces.MapGet("/", async (
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
            AccountId(principal) is { } accountId
                ? Results.Ok(await service.ListWorkspacesAsync(accountId, token))
                : Results.Unauthorized())
            .WithName("ListTestWorkspaces")
            .Produces<IReadOnlyList<TestWorkspaceResponse>>();

        workspaces.MapGet("/{workspaceId:guid}", async (
            Guid workspaceId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var value = await service.GetWorkspaceAsync(workspaceId, accountId, token);
            return value is null ? Results.NotFound() : Results.Ok(value);
        }).WithName("GetTestWorkspace").Produces<TestWorkspaceResponse>();

        workspaces.MapPost("/", async (
            CreateTestWorkspaceRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateName(request.Name, request.Description) is { } invalid) return invalid;
            if (!string.IsNullOrWhiteSpace(request.Prefix) &&
                !Regex.IsMatch(request.Prefix.Trim(), "^[A-Za-z][A-Za-z0-9]{1,9}$"))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["prefix"] = ["Prefix must be 2-10 letters or numbers and start with a letter."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreateWorkspaceAsync(accountId, request, token);
            return result.Outcome switch
            {
                TestManagementOutcome.Succeeded =>
                    Results.Created($"/api/v1/test-workspaces/{result.Value!.Id}", result.Value),
                TestManagementOutcome.Invalid => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["prefix"] = [result.Code ?? "Invalid prefix."] }),
                _ => Problem(result.Code ?? "workspace_conflict"),
            };
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestWorkspace").Produces<TestWorkspaceResponse>(201);

        workspaces.MapPut("/{workspaceId:guid}", async (
            Guid workspaceId, UpdateTestWorkspaceRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateName(request.Name, request.Description) is { } invalid) return invalid;
            if (request.Status is not ("active" or "inactive") || request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Status or version is invalid."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateWorkspaceAsync(workspaceId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestWorkspace").Produces<TestWorkspaceResponse>();

        workspaces.MapGet("/{workspaceId:guid}/projects", async (
            Guid workspaceId,
            ClaimsPrincipal principal,
            TestManagementService service,
            CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListWorkspaceProjectsAsync(workspaceId, accountId, token));
        }).WithName("ListTestWorkspaceProjects")
          .Produces<IReadOnlyList<TestWorkspaceProjectResponse>>();

        workspaces.MapPost("/{workspaceId:guid}/projects", async (
            Guid workspaceId,
            LinkTestWorkspaceProjectRequest request,
            ClaimsPrincipal principal,
            TestManagementService service,
            CancellationToken token) =>
        {
            if (request.ProjectId == Guid.Empty)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["projectId"] = ["A valid project id is required."],
                });
            }
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.LinkWorkspaceProjectAsync(
                workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created(
                    $"/api/v1/test-workspaces/{workspaceId}/projects/{request.ProjectId}",
                    result.Value)
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("LinkTestWorkspaceProject")
          .Produces<TestWorkspaceProjectResponse>(201);

        workspaces.MapDelete("/{workspaceId:guid}/projects/{projectId:guid}", async (
            Guid workspaceId,
            Guid projectId,
            int version,
            ClaimsPrincipal principal,
            TestManagementService service,
            CancellationToken token) =>
        {
            if (version < 1)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["version"] = ["Version must be greater than zero."],
                });
            }
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.UnlinkWorkspaceProjectAsync(
                workspaceId, projectId, accountId, version, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.NoContent()
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UnlinkTestWorkspaceProject")
          .Produces(204);

        workspaces.MapGet("/{workspaceId:guid}/members", async (
            Guid workspaceId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListMembersAsync(workspaceId, accountId, token));
        }).WithName("ListTestWorkspaceMembers")
          .Produces<IReadOnlyList<TestWorkspaceMemberResponse>>();

        workspaces.MapPost("/{workspaceId:guid}/members", async (
            Guid workspaceId, AddTestWorkspaceMemberRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || !ValidRole(request.Role))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["member"] = ["Username and a valid role are required."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.AddMemberAsync(workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Json(result.Value, statusCode: 201) : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("AddTestWorkspaceMember").Produces<TestWorkspaceMemberResponse>(201);

        workspaces.MapPut("/{workspaceId:guid}/members/{memberId:guid}", async (
            Guid workspaceId, Guid memberId, UpdateTestWorkspaceMemberRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (!ValidRole(request.Role) || request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["member"] = ["Role or version is invalid."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateMemberAsync(
                workspaceId, memberId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestWorkspaceMember").Produces<TestWorkspaceMemberResponse>();

        workspaces.MapDelete("/{workspaceId:guid}/members/{memberId:guid}", async (
            Guid workspaceId, Guid memberId, int version, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (version < 1) return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["version"] = ["Version must be greater than zero."],
            });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.RemoveMemberAsync(
                workspaceId, memberId, accountId, version, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.NoContent() : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("RemoveTestWorkspaceMember").Produces(204);

        workspaces.MapGet("/{workspaceId:guid}/suites", async (
            Guid workspaceId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListSuitesAsync(workspaceId, accountId, token));
        }).WithName("ListTestSuites").Produces<IReadOnlyList<TestSuiteResponse>>();

        workspaces.MapPost("/{workspaceId:guid}/suites", async (
            Guid workspaceId, CreateTestSuiteRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateSuite(request.Name, request.Description, request.SortOrder) is { } invalid)
                return invalid;
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreateSuiteAsync(workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Json(result.Value, statusCode: 201) : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestSuite").Produces<TestSuiteResponse>(201);

        workspaces.MapPut("/{workspaceId:guid}/suites/{suiteId:guid}", async (
            Guid workspaceId, Guid suiteId, UpdateTestSuiteRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (ValidateSuite(request.Name, request.Description, request.SortOrder) is { } invalid)
                return invalid;
            if (request.Status is not ("active" or "inactive") || request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Status or version is invalid."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateSuiteAsync(
                workspaceId, suiteId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestSuite").Produces<TestSuiteResponse>();

        workspaces.MapGet("/{workspaceId:guid}/cases", async (
            Guid workspaceId, Guid? suiteId, string? search, string? status, Guid? tagId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListCasesAsync(workspaceId, accountId, suiteId, search, status, tagId, token));
        }).WithName("ListTestCases").Produces<IReadOnlyList<TestCaseResponse>>();

        workspaces.MapPost("/{workspaceId:guid}/cases", async (
            Guid workspaceId, CreateTestCaseRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateCase(request.Title, request.Description, request.Preconditions,
                request.OverallExpectedResult, request.SortOrder, request.Steps) is { } invalid)
                return invalid;
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreateCaseAsync(workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created(
                    $"/api/v1/test-workspaces/{workspaceId}/cases/{result.Value!.Id}",
                    result.Value)
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestCase").Produces<TestCaseResponse>(201);

        workspaces.MapGet("/{workspaceId:guid}/cases/{caseId:guid}", async (
            Guid workspaceId, Guid caseId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.GetCaseAsync(workspaceId, caseId, accountId, token));
        }).WithName("GetTestCase").Produces<TestCaseResponse>();

        workspaces.MapPut("/{workspaceId:guid}/cases/{caseId:guid}", async (
            Guid workspaceId, Guid caseId, UpdateTestCaseRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateCase(request.Title, request.Description, request.Preconditions,
                request.OverallExpectedResult, request.SortOrder, request.Steps) is { } invalid)
                return invalid;
            if (request.Status is not ("active" or "inactive") || request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["testCase"] = ["Status or version is invalid."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateCaseAsync(workspaceId, caseId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestCase").Produces<TestCaseResponse>();

        workspaces.MapGet("/{workspaceId:guid}/plans", async (
            Guid workspaceId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListPlansAsync(workspaceId, accountId, token));
        }).WithName("ListTestPlans").Produces<IReadOnlyList<TestPlanResponse>>();

        workspaces.MapGet("/{workspaceId:guid}/plans/{planId:guid}", async (
            Guid workspaceId, Guid planId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.GetPlanAsync(workspaceId, planId, accountId, token));
        }).WithName("GetTestPlan").Produces<TestPlanResponse>();

        workspaces.MapPost("/{workspaceId:guid}/plans", async (
            Guid workspaceId, CreateTestPlanRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidatePlan(request.Name, request.Description, request.CaseIds) is { } invalid)
                return invalid;
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreatePlanAsync(workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created(
                    $"/api/v1/test-workspaces/{workspaceId}/plans/{result.Value!.Id}",
                    result.Value)
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestPlan").Produces<TestPlanResponse>(201);

        workspaces.MapPut("/{workspaceId:guid}/plans/{planId:guid}", async (
            Guid workspaceId, Guid planId, UpdateTestPlanRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (ValidatePlan(request.Name, request.Description, request.CaseIds) is { } invalid)
                return invalid;
            if (request.Status is not ("draft" or "active" or "archived") ||
                request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["plan"] = ["Plan status or version is invalid."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdatePlanAsync(
                workspaceId, planId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestPlan").Produces<TestPlanResponse>();

        workspaces.MapGet("/{workspaceId:guid}/runs", async (
            Guid workspaceId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListRunsAsync(workspaceId, accountId, token));
        }).WithName("ListTestRuns").Produces<IReadOnlyList<TestRunResponse>>();

        workspaces.MapGet("/{workspaceId:guid}/runs/{runId:guid}", async (
            Guid workspaceId, Guid runId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.GetRunAsync(workspaceId, runId, accountId, token));
        }).WithName("GetTestRun").Produces<TestRunResponse>();

        workspaces.MapPost("/{workspaceId:guid}/runs", async (
            Guid workspaceId, CreateTestRunRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["run"] = ["Run name is required and cannot exceed 200 characters."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreateRunAsync(workspaceId, accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created(
                    $"/api/v1/test-workspaces/{workspaceId}/runs/{result.Value!.Id}",
                    result.Value)
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestRun").Produces<TestRunResponse>(201);

        workspaces.MapPost("/{workspaceId:guid}/runs/{runId:guid}/rerun", async (
            Guid workspaceId, Guid runId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.RerunAsync(workspaceId, runId, accountId, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created($"/api/v1/test-workspaces/{workspaceId}/runs/{result.Value!.Id}", result.Value)
                : Map(result);
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("RerunTestRun").Produces<TestRunResponse>(201);

        workspaces.MapPut("/{workspaceId:guid}/runs/{runId:guid}/items/{itemId:guid}", async (
            Guid workspaceId, Guid runId, Guid itemId, RecordTestResultRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (ValidateResult(request) is { } invalid) return invalid;
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.RecordRunItemAsync(
                workspaceId, runId, itemId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("RecordTestRunItem").Produces<TestRunResponse>();

        workspaces.MapPut(
            "/{workspaceId:guid}/runs/{runId:guid}/items/{itemId:guid}/steps/{stepId:guid}",
            async (
                Guid workspaceId, Guid runId, Guid itemId, Guid stepId,
                RecordTestResultRequest request, ClaimsPrincipal principal,
                TestManagementService service, CancellationToken token) =>
            {
                if (ValidateResult(request) is { } invalid) return invalid;
                if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
                return Map(await service.RecordRunStepAsync(
                    workspaceId, runId, itemId, stepId, accountId, request, token));
            }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
              .WithName("RecordTestRunStep").Produces<TestRunResponse>();

        workspaces.MapPut("/{workspaceId:guid}/runs/{runId:guid}/status", async (
            Guid workspaceId, Guid runId, UpdateTestRunStatusRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (request.Status is not ("in_progress" or "completed" or "cancelled") ||
                request.Version < 1 || request.Summary?.Length > 4000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["run"] = ["A valid run status, version, and summary are required."],
                });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateRunStatusAsync(
                workspaceId, runId, accountId, request, token));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestRunStatus").Produces<TestRunResponse>();

        var tags = endpoints.MapGroup("/api/v1/test-tags")
            .WithTags("Test Management")
            .RequireAuthorization();

        tags.MapGet("/", async (TestManagementService service, CancellationToken token) =>
            Results.Ok(await service.ListTagsAsync(token)))
            .WithName("ListTestTags").Produces<IReadOnlyList<TestTagResponse>>();

        tags.MapPost("/", async (CreateTestTagRequest request, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (ValidateTag(request.Name, request.Description) is { } invalid) return invalid;
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            var result = await service.CreateTagAsync(accountId, request, token);
            return result.Outcome == TestManagementOutcome.Succeeded
                ? Results.Created($"/api/v1/test-tags/{result.Value!.Id}", result.Value) : Map(result);
        }).RequireAuthorization("account.create")
          .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("CreateTestTag").Produces<TestTagResponse>(201);

        tags.MapPut("/{tagId:guid}", async (Guid tagId, UpdateTestTagRequest request,
            ClaimsPrincipal principal, TestManagementService service, CancellationToken token) =>
        {
            if (ValidateTag(request.Name, request.Description) is { } invalid) return invalid;
            if (request.Status is not ("active" or "inactive") || request.Version < 1)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["tag"] = ["Status or version is invalid."] });
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.UpdateTagAsync(tagId, accountId, request, token));
        }).RequireAuthorization("account.create")
          .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
          .WithName("UpdateTestTag").Produces<TestTagResponse>();

        return endpoints;
    }

    private static Guid? AccountId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;

    private static IResult Map<T>(TestManagementResult<T> result) => result.Outcome switch
    {
        TestManagementOutcome.Succeeded => Results.Ok(result.Value),
        TestManagementOutcome.Forbidden => Results.Forbid(),
        TestManagementOutcome.NotFound => Results.NotFound(),
        TestManagementOutcome.Invalid => Results.ValidationProblem(
            new Dictionary<string, string[]> { ["request"] = [result.Code ?? "Invalid request."] }),
        _ => Problem(result.Code ?? "test_management_conflict"),
    };

    private static IResult Problem(string code) => Results.Problem(
        statusCode: 409,
        type: $"https://khaikang.dev/problems/test-management/{code.Replace('_', '-')}",
        detail: code,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static IResult? ValidateName(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || description?.Length > 4000)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["workspace"] = ["Name is required and description cannot exceed 4000 characters."],
            });
        return null;
    }

    private static IResult? ValidateSuite(string name, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
            description?.Length > 4000 || sortOrder < 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["suite"] = ["Suite name, description, or sort order is invalid."],
            });
        return null;
    }

    private static IResult? ValidateCase(
        string title,
        string? description,
        string? preconditions,
        string? overallExpectedResult,
        int sortOrder,
        IReadOnlyList<CreateTestCaseStepRequest>? steps)
    {
        var textIsInvalid =
            string.IsNullOrWhiteSpace(title) ||
            title.Length > 200 ||
            description?.Length > 4000 ||
            preconditions?.Length > 4000 ||
            overallExpectedResult?.Length > 4000 ||
            sortOrder < 0;
        var stepsAreInvalid =
            steps is null ||
            steps.Count is < 1 or > 100 ||
            steps.Any(step =>
                string.IsNullOrWhiteSpace(step.Action) ||
                step.Action.Length > 4000 ||
                string.IsNullOrWhiteSpace(step.ExpectedResult) ||
                step.ExpectedResult.Length > 4000);
        if (!textIsInvalid && !stepsAreInvalid)
        {
            return null;
        }

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["testCase"] =
            [
                "A title and at least one valid step with an expected result are required.",
            ],
        });
    }

    private static IResult? ValidateTag(string name, string? description) =>
        string.IsNullOrWhiteSpace(name) || name.Length > 50 || description?.Length > 4000
            ? Results.ValidationProblem(new Dictionary<string, string[]> { ["tag"] = ["Tag name or description is invalid."] })
            : null;

    private static bool ValidRole(string role) =>
        role is "owner" or "manager" or "tester" or "viewer";

    private static IResult? ValidatePlan(
        string? name, string? description, IReadOnlyList<Guid>? caseIds)
    {
        if (name is { Length: > 200 } ||
            description?.Length > 4000 || caseIds is null || caseIds.Count > 1000)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["plan"] = ["Plan name, description, or case scope is invalid."],
            });
        return null;
    }

    private static IResult? ValidateResult(RecordTestResultRequest request)
    {
        if (request.Status is not ("not_run" or "passed" or "failed" or "blocked" or "skipped") ||
            request.Version < 1 || request.ActualResult?.Length > 4000)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["result"] = ["Result status, version, or actual result is invalid."],
            });
        return null;
    }
}
