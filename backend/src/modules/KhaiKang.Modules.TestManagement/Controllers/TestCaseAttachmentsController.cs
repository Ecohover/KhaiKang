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
[Route("api/v1/test-workspaces/{workspaceId:guid}/cases/{caseId:guid}/attachments")]
public sealed class TestCaseAttachmentsController(TestCaseAttachmentService attachmentService) : ControllerBase
{
    [HttpGet(Name = "ListTestCaseAttachments")]
    [ProducesResponseType<IReadOnlyList<TestCaseAttachmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestCaseAttachmentResponse>>> ListAsync(
        Guid workspaceId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.ListAsync(workspaceId, caseId, accountId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost(Name = "UploadTestCaseAttachment")]
    [RequireAntiforgeryToken(true)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ProducesResponseType<TestCaseAttachmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TestCaseAttachmentResponse>> UploadAsync(
        Guid workspaceId,
        Guid caseId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        await using var stream = file.OpenReadStream();
        var result = await attachmentService.UploadAsync(
            workspaceId,
            caseId,
            accountId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            cancellationToken);
        return result.Outcome switch
        {
            TestCaseAttachmentOutcome.Succeeded => CreatedAtRoute(
                "GetTestCaseAttachmentContent",
                new { workspaceId, caseId, attachmentId = result.Attachment!.Id },
                result.Attachment),
            TestCaseAttachmentOutcome.Forbidden => Forbid(),
            TestCaseAttachmentOutcome.NotFound => NotFound(),
            TestCaseAttachmentOutcome.WorkspaceInactive => Conflict("The test workspace is inactive and its attachments are read-only."),
            TestCaseAttachmentOutcome.FileTooLarge => Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: "The attachment exceeds the configured size limit."),
            TestCaseAttachmentOutcome.InvalidFile => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["A non-empty file is required."] })),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable."),
        };
    }

    [HttpGet("{attachmentId:guid}/content", Name = "GetTestCaseAttachmentContent")]
    public async Task<IActionResult> GetContentAsync(
        Guid workspaceId,
        Guid caseId,
        Guid attachmentId,
        [FromQuery] bool inline,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.OpenContentAsync(
            workspaceId, caseId, attachmentId, accountId, cancellationToken);
        if (result.Outcome == TestCaseAttachmentOutcome.Forbidden) return Forbid();
        if (result.Outcome == TestCaseAttachmentOutcome.NotFound) return NotFound();
        if (result.Outcome != TestCaseAttachmentOutcome.Succeeded)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable.");

        var isInlineImage = inline && result.ContentType!.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return isInlineImage
            ? File(result.Content!, result.ContentType!, enableRangeProcessing: true)
            : File(result.Content!, result.ContentType!, result.FileName!, enableRangeProcessing: true);
    }

    [HttpDelete("{attachmentId:guid}", Name = "DeleteTestCaseAttachment")]
    [RequireAntiforgeryToken(true)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.DeleteAsync(
            workspaceId, caseId, attachmentId, accountId, cancellationToken);
        return result.Outcome switch
        {
            TestCaseAttachmentOutcome.Succeeded => NoContent(),
            TestCaseAttachmentOutcome.Forbidden => Forbid(),
            TestCaseAttachmentOutcome.NotFound => NotFound(),
            TestCaseAttachmentOutcome.WorkspaceInactive => Conflict("The test workspace is inactive and its attachments are read-only."),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable."),
        };
    }

    private bool TryGetAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
