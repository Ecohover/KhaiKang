using System.Security.Claims;
using KhaiKang.Modules.TestManagement.Application;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Modules.TestManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/test-workspaces/{workspaceId:guid}/runs/{runId:guid}/bugs")]
public sealed class TestRunBugsController(TestRunBugService bugService) : ControllerBase
{
    [HttpGet(Name = "ListTestRunBugs")]
    [ProducesResponseType<IReadOnlyList<TestRunBugLinkResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TestRunBugLinkResponse>>> ListAsync(
        Guid workspaceId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await bugService.ListAsync(
            workspaceId, runId, accountId, cancellationToken);
        return result.Outcome == TestManagementOutcome.Succeeded
            ? Ok(result.Value)
            : NotFound();
    }

    [HttpPost(Name = "CreateTestRunBug")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<TestRunBugLinkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TestRunBugLinkResponse>> CreateAsync(
        Guid workspaceId,
        Guid runId,
        CreateTestRunBugRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (!TryGetAccountId(out var accountId)) return Unauthorized();

        var result = await bugService.CreateAsync(
            workspaceId, runId, accountId, request, cancellationToken);
        return result.Outcome switch
        {
            TestManagementOutcome.Succeeded => StatusCode(StatusCodes.Status201Created, result.Value),
            TestManagementOutcome.Forbidden => Forbid(),
            TestManagementOutcome.NotFound => NotFound(),
            TestManagementOutcome.Invalid => BadRequestProblem(result.Code),
            TestManagementOutcome.Conflict => ConflictProblem(result.Code),
            _ => NotFound(),
        };
    }

    private static Dictionary<string, string[]> Validate(CreateTestRunBugRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ProjectId == Guid.Empty)
            errors["projectId"] = ["A target Project is required."];
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            errors["title"] = ["Bug title is required and cannot exceed 200 characters."];
        if (request.PriorityCode is not null && string.IsNullOrWhiteSpace(request.PriorityCode))
            errors["priorityCode"] = ["Bug priority cannot be empty when provided."];
        if (request.Description?.Length > 20_000)
            errors["description"] = ["description cannot exceed 20000 characters."];
        return errors;
    }

    private ObjectResult BadRequestProblem(string? code) => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        type: "https://khaikang.dev/problems/test-management/run-bug-invalid",
        detail: "The Bug Issue request is outside the Test Run trace scope.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private ObjectResult ConflictProblem(string? code) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        type: "https://khaikang.dev/problems/test-management/run-bug-conflict",
        detail: "The Bug Issue could not be created from this Test Run.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private bool TryGetAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
