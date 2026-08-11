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
[Route("api/v1/test-workspaces/{workspaceId:guid}/cases/{caseId:guid}/requirement-issues")]
public sealed class TestCaseRequirementLinksController(
    TestCaseRequirementLinkService linkService) : ControllerBase
{
    [HttpGet(Name = "ListTestCaseRequirementIssues")]
    [ProducesResponseType<IReadOnlyList<TestCaseRequirementLinkResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TestCaseRequirementLinkResponse>>> ListAsync(
        Guid workspaceId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await linkService.ListAsync(
            workspaceId, caseId, accountId, cancellationToken);
        return result.Outcome == TestManagementOutcome.Succeeded
            ? Ok(result.Value)
            : NotFound();
    }

    [HttpPost(Name = "LinkTestCaseRequirementIssue")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<TestCaseRequirementLinkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TestCaseRequirementLinkResponse>> CreateAsync(
        Guid workspaceId,
        Guid caseId,
        LinkTestCaseRequirementIssueRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequirementIssueId == Guid.Empty)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["requirementIssueId"] = ["A requirement Issue is required."],
                }));
        }

        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await linkService.CreateAsync(
            workspaceId, caseId, accountId, request, cancellationToken);
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

    [HttpDelete("{linkId:guid}", Name = "UnlinkTestCaseRequirementIssue")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid caseId,
        Guid linkId,
        [FromQuery] int version,
        CancellationToken cancellationToken)
    {
        if (version < 1)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["version"] = ["Version must be greater than zero."] }));
        }

        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await linkService.DeleteAsync(
            workspaceId, caseId, linkId, accountId, version, cancellationToken);
        return result.Outcome switch
        {
            TestManagementOutcome.Succeeded => NoContent(),
            TestManagementOutcome.Forbidden => Forbid(),
            TestManagementOutcome.NotFound => NotFound(),
            TestManagementOutcome.Conflict => ConflictProblem(result.Code),
            _ => NotFound(),
        };
    }

    private ObjectResult BadRequestProblem(string? code) => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        type: "https://khaikang.dev/problems/test-management/trace-invalid",
        detail: "The Issue is outside the Test Workspace trace scope.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private ObjectResult ConflictProblem(string? code) => Problem(
        statusCode: StatusCodes.Status409Conflict,
        type: "https://khaikang.dev/problems/test-management/trace-conflict",
        detail: "The test trace could not be changed because its state conflicts.",
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private bool TryGetAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
