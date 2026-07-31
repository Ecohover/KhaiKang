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
            Guid workspaceId, Guid? suiteId, ClaimsPrincipal principal,
            TestManagementService service, CancellationToken token) =>
        {
            if (AccountId(principal) is not { } accountId) return Results.Unauthorized();
            return Map(await service.ListCasesAsync(workspaceId, accountId, suiteId, token));
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

    private static bool ValidRole(string role) =>
        role is "owner" or "manager" or "tester" or "viewer";
}
