using System.Security.Claims;
using KhaiKang.CommonUtils.Web.Contracts;
using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Modules.ProjectManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects/{projectId:guid}/issues/{issueId:guid}/attachments")]
public sealed class IssueAttachmentsController(IssueAttachmentService attachmentService) : ControllerBase
{
    [HttpGet(Name = "ListIssueAttachments")]
    [ProducesResponseType<IReadOnlyList<IssueAttachmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IssueAttachmentResponse>>> ListAsync(
        Guid projectId,
        Guid issueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.ListAsync(projectId, issueId, accountId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost(Name = "UploadIssueAttachment")]
    [RequireAntiforgeryToken(true)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ProducesResponseType<IssueAttachmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IssueAttachmentResponse>> UploadAsync(
        Guid projectId,
        Guid issueId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        await using var stream = file.OpenReadStream();
        var result = await attachmentService.UploadAsync(
            projectId,
            issueId,
            accountId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            cancellationToken);
        return result.Outcome switch
        {
            IssueAttachmentOutcome.Succeeded => CreatedAtRoute(
                "GetIssueAttachmentContent",
                new { projectId, issueId, attachmentId = result.Attachment!.Id },
                result.Attachment),
            IssueAttachmentOutcome.Forbidden => Forbid(),
            IssueAttachmentOutcome.NotFound => NotFound(),
            IssueAttachmentOutcome.ProjectInactive => Conflict("The project is inactive and its attachments are read-only."),
            IssueAttachmentOutcome.FileTooLarge => Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: "The attachment exceeds the configured size limit."),
            IssueAttachmentOutcome.InvalidFile => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["A non-empty file is required."] })),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable."),
        };
    }

    [HttpGet("{attachmentId:guid}/content", Name = "GetIssueAttachmentContent")]
    public async Task<IActionResult> GetContentAsync(
        Guid projectId,
        Guid issueId,
        Guid attachmentId,
        [FromQuery] bool inline,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.OpenContentAsync(projectId, issueId, attachmentId, accountId, cancellationToken);
        if (result.Outcome == IssueAttachmentOutcome.Forbidden) return Forbid();
        if (result.Outcome == IssueAttachmentOutcome.NotFound) return NotFound();
        if (result.Outcome != IssueAttachmentOutcome.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable.");
        }

        var isInlineImage = inline && result.ContentType!.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return isInlineImage
            ? File(result.Content!, result.ContentType!, enableRangeProcessing: true)
            : File(result.Content!, result.ContentType!, result.FileName!, enableRangeProcessing: true);
    }

    [HttpDelete("{attachmentId:guid}", Name = "DeleteIssueAttachment")]
    [RequireAntiforgeryToken(true)]
    public async Task<IActionResult> DeleteAsync(
        Guid projectId,
        Guid issueId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await attachmentService.DeleteAsync(projectId, issueId, attachmentId, accountId, cancellationToken);
        return result.Outcome switch
        {
            IssueAttachmentOutcome.Succeeded => NoContent(),
            IssueAttachmentOutcome.Forbidden => Forbid(),
            IssueAttachmentOutcome.NotFound => NotFound(),
            IssueAttachmentOutcome.ProjectInactive => Conflict("The project is inactive and its attachments are read-only."),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Attachment storage is unavailable."),
        };
    }

    private bool TryGetAccountId(out Guid accountId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
}
