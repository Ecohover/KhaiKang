using System.Security.Claims;
using KhaiKang.CommonUtils.Web.Contracts;
using KhaiKang.Modules.TestManagement.Application;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Modules.TestManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/test-workspaces/{workspaceId:guid}/runs/{runId:guid}/items/{itemId:guid}/attachments")]
public sealed class TestRunItemAttachmentsController(
    TestRunItemAttachmentService attachmentService) : ControllerBase
{
    [HttpGet(Name = "ListTestRunItemAttachments")]
    [ProducesResponseType<IReadOnlyList<TestRunItemAttachmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestRunItemAttachmentResponse>>> ListAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.ListAsync(
            workspaceId, runId, itemId, accountId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost(Name = "UploadTestRunItemAttachment")]
    [RequireAntiforgeryToken(true)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ProducesResponseType<TestRunItemAttachmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TestRunItemAttachmentResponse>> UploadAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        await using var stream = file.OpenReadStream();
        var result = await attachmentService.UploadAsync(
            workspaceId,
            runId,
            itemId,
            accountId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            cancellationToken);
        return result.Outcome switch
        {
            TestRunItemAttachmentOutcome.Succeeded => CreatedAtRoute(
                "GetTestRunItemAttachmentContent",
                new { workspaceId, runId, itemId, attachmentId = result.Attachment!.Id },
                result.Attachment),
            TestRunItemAttachmentOutcome.Forbidden => Forbid(),
            TestRunItemAttachmentOutcome.NotFound => NotFound(),
            TestRunItemAttachmentOutcome.WorkspaceInactive => Conflict(
                "The test workspace is inactive and its attachments are read-only."),
            TestRunItemAttachmentOutcome.RunNotInProgress => Conflict(
                "The test run is not in progress and its attachments are read-only."),
            TestRunItemAttachmentOutcome.FileTooLarge => Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                detail: "The attachment exceeds the configured size limit."),
            TestRunItemAttachmentOutcome.InvalidFile => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["A non-empty file is required."] })),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "Attachment storage is unavailable."),
        };
    }

    [HttpGet("{attachmentId:guid}/content", Name = "GetTestRunItemAttachmentContent")]
    public async Task<IActionResult> GetContentAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid attachmentId,
        [FromQuery] bool inline,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.OpenContentAsync(
            workspaceId, runId, itemId, attachmentId, accountId, cancellationToken);
        if (result.Outcome == TestRunItemAttachmentOutcome.Forbidden) return Forbid();
        if (result.Outcome == TestRunItemAttachmentOutcome.NotFound) return NotFound();
        if (result.Outcome != TestRunItemAttachmentOutcome.Succeeded)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "Attachment storage is unavailable.");

        var isInlineImage = inline &&
            result.ContentType!.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return isInlineImage
            ? File(result.Content!, result.ContentType!, enableRangeProcessing: true)
            : File(
                result.Content!,
                result.ContentType!,
                result.FileName!,
                enableRangeProcessing: true);
    }

    [HttpDelete("{attachmentId:guid}", Name = "DeleteTestRunItemAttachment")]
    [RequireAntiforgeryToken(true)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.DeleteAsync(
            workspaceId, runId, itemId, attachmentId, accountId, cancellationToken);
        return result.Outcome switch
        {
            TestRunItemAttachmentOutcome.Succeeded => NoContent(),
            TestRunItemAttachmentOutcome.Forbidden => Forbid(),
            TestRunItemAttachmentOutcome.NotFound => NotFound(),
            TestRunItemAttachmentOutcome.WorkspaceInactive => Conflict(
                "The test workspace is inactive and its attachments are read-only."),
            TestRunItemAttachmentOutcome.RunNotInProgress => Conflict(
                "The test run is not in progress and its attachments are read-only."),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "Attachment storage is unavailable."),
        };
    }

    private bool TryGetAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
