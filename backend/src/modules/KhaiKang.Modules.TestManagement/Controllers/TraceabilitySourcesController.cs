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
[Route("api/v1/test-workspaces/{workspaceId:guid}")]
public sealed class TraceabilitySourcesController(TraceabilitySourceService service) : ControllerBase
{
    [HttpGet("cases/{caseId:guid}/source-test-issue", Name = "GetTestCaseSource")]
    [ProducesResponseType<TestCaseSourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestCaseSourceResponse>> GetCaseAsync(Guid workspaceId, Guid caseId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Unauthorized();
        var result = await service.GetCaseSourceAsync(workspaceId, caseId, actorId, cancellationToken);
        return result.Outcome == TestManagementOutcome.Succeeded ? Ok(result.Value) : NotFound();
    }

    [HttpPost("cases/{caseId:guid}/source-test-issue", Name = "LinkTestCaseSource")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<TestCaseSourceResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TestCaseSourceResponse>> CreateCaseAsync(Guid workspaceId, Guid caseId, LinkTestCaseSourceRequest request, CancellationToken cancellationToken)
    {
        if (!TryKey(out var key)) return IdempotencyRequired();
        if (!TryActor(out var actorId)) return Unauthorized();
        return Map(await service.CreateCaseSourceAsync(workspaceId, caseId, actorId, key, request, cancellationToken));
    }

    [HttpGet("bugs/{bugIssueId:guid}/sources", Name = "GetBugSources")]
    [ProducesResponseType<BugSourcesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BugSourcesResponse>> GetBugAsync(Guid workspaceId, Guid bugIssueId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Unauthorized();
        var result = await service.GetBugSourcesAsync(workspaceId, bugIssueId, actorId, cancellationToken);
        return result.Outcome == TestManagementOutcome.Succeeded ? Ok(result.Value) : NotFound();
    }

    [HttpPost("bugs/{bugIssueId:guid}/sources", Name = "LinkBugSources")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<BugSourcesResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BugSourcesResponse>> CreateBugAsync(Guid workspaceId, Guid bugIssueId, LinkBugSourcesRequest request, CancellationToken cancellationToken)
    {
        if (!TryKey(out var key)) return IdempotencyRequired();
        if (!TryActor(out var actorId)) return Unauthorized();
        return Map(await service.CreateBugSourcesAsync(workspaceId, bugIssueId, actorId, key, request, cancellationToken));
    }

    private ActionResult<T> Map<T>(TraceabilitySourceResult<T> result) where T : class => result.Outcome switch
    {
        TestManagementOutcome.Succeeded => StatusCode(StatusCodes.Status201Created, result.Value),
        TestManagementOutcome.Forbidden => Forbid(),
        TestManagementOutcome.NotFound => NotFound(),
        TestManagementOutcome.Invalid => Problem(statusCode: 400, type: "https://khaikang.dev/problems/test-management/trace-invalid", extensions: new Dictionary<string, object?> { ["code"] = result.Code }),
        _ => Problem(statusCode: 409, type: "https://khaikang.dev/problems/test-management/trace-conflict", extensions: new Dictionary<string, object?> { ["code"] = result.Code }),
    };

    private ObjectResult IdempotencyRequired() => Problem(statusCode: 400, type: "https://khaikang.dev/problems/idempotency-key-required", detail: "A single non-empty Idempotency-Key header is required.");
    private bool TryKey(out string key)
    {
        key = string.Empty;
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var values) || values.Count != 1) return false;
        key = values[0]?.Trim() ?? string.Empty;
        return key.Length is > 0 and <= 200;
    }
    private bool TryActor(out Guid actorId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
}
