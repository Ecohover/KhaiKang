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
[Route("api/v1/projects/{projectId:guid}")]
public sealed class IssueRelationsController(IssueRelationService relationService) : ControllerBase
{
    [HttpGet("issue-relation-types", Name = "ListIssueRelationTypes")]
    [ProducesResponseType<IReadOnlyList<IssueRelationTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<IssueRelationTypeResponse>>> ListTypesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await relationService.ListTypesAsync(projectId, accountId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("issues/{issueId:guid}/relations", Name = "ListIssueRelations")]
    [ProducesResponseType<IReadOnlyList<IssueRelationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<IssueRelationResponse>>> ListAsync(
        Guid projectId,
        Guid issueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await relationService.ListAsync(projectId, issueId, accountId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("issues/{issueId:guid}/relations", Name = "CreateIssueRelation")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<IssueRelationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueRelationResponse>> CreateAsync(
        Guid projectId,
        Guid issueId,
        CreateIssueRelationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateCreateRequest(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await relationService.CreateAsync(
            projectId,
            issueId,
            accountId,
            request,
            cancellationToken);
        if (result.Outcome == IssueRelationMutationOutcome.Succeeded)
        {
            return StatusCode(StatusCodes.Status201Created, result.Relation);
        }

        return MapMutation(result);
    }

    [HttpDelete("issues/{issueId:guid}/relations/{relationId:guid}", Name = "DeleteIssueRelation")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        Guid projectId,
        Guid issueId,
        Guid relationId,
        [FromQuery] int version,
        CancellationToken cancellationToken)
    {
        if (version < 1)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["version"] = ["Relation version must be greater than zero."],
                }));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await relationService.DeleteAsync(
            projectId,
            issueId,
            relationId,
            accountId,
            version,
            cancellationToken);
        return result.Outcome == IssueRelationMutationOutcome.Succeeded
            ? NoContent()
            : MapMutation(result).Result!;
    }

    private ActionResult<IssueRelationResponse> MapMutation(IssueRelationMutationResult result)
    {
        return result.Outcome switch
        {
            IssueRelationMutationOutcome.Succeeded => Ok(result.Relation),
            IssueRelationMutationOutcome.Forbidden => Forbid(),
            IssueRelationMutationOutcome.NotFound => NotFound(),
            IssueRelationMutationOutcome.InvalidType => Validation("relationTypeCode", "The relation type is invalid or inactive."),
            IssueRelationMutationOutcome.InvalidDirection => Validation("direction", "Direction must be forward or reverse."),
            IssueRelationMutationOutcome.SelfRelation => Validation("relatedIssueId", "An Issue cannot relate to itself."),
            IssueRelationMutationOutcome.ProjectInactive => Conflict(
                "project_inactive",
                "The project is inactive and Issue relations are read-only."),
            IssueRelationMutationOutcome.Duplicate => Conflict(
                "issue_relation_duplicate",
                "The active Issue relation already exists."),
            IssueRelationMutationOutcome.ParentConflict => Conflict(
                "issue_relation_parent_conflict",
                "The child Issue already has an active parent."),
            IssueRelationMutationOutcome.HierarchyCycle => Conflict(
                "issue_relation_hierarchy_cycle",
                "The parent relation would create a hierarchy cycle."),
            IssueRelationMutationOutcome.VersionConflict => Conflict(
                "issue_relation_version_conflict",
                "The Issue relation was changed by another user. Reload and try again."),
            _ => NotFound(),
        };
    }

    private ActionResult<IssueRelationResponse> Validation(string field, string message)
    {
        return ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]> { [field] = [message] }));
    }

    private ActionResult<IssueRelationResponse> Conflict(string code, string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            type: $"https://khaikang.dev/problems/issues/{code.Replace('_', '-')}",
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateIssueRelationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.RelationTypeCode) || request.RelationTypeCode.Length > 50)
        {
            errors["relationTypeCode"] = ["Relation type is required and cannot exceed 50 characters."];
        }

        if (request.RelatedIssueId == Guid.Empty)
        {
            errors["relatedIssueId"] = ["A related Issue is required."];
        }

        if (!string.Equals(request.Direction, "forward", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Direction, "reverse", StringComparison.OrdinalIgnoreCase))
        {
            errors["direction"] = ["Direction must be forward or reverse."];
        }

        return errors;
    }

    private bool TryGetAccountId(out Guid accountId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
    }
}
