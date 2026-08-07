using System.Security.Claims;
using KhaiKang.CommonUtils.Models;
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
[Route("api/v1/projects/{projectId:guid}/issues")]
public sealed class ProjectIssuesController(IssueService issueService) : ControllerBase
{
    #region Standard Operations

    /// <summary>
    /// Returns one page of tasks visible to the current project member.
    /// </summary>
    [HttpGet(Name = "ListIssues")]
    [ProducesResponseType<PagedResult<IssueResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<IssueResponse>>> ListAsync(
        Guid projectId,
        [FromQuery] PageRequest pageRequest,
        [FromQuery] IssueListQuery query,
        CancellationToken cancellationToken)
    {
        var validation = ValidateListQuery(query);
        if (validation is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(validation));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await issueService.ListAsync(
            projectId,
            accountId,
            pageRequest.Page,
            pageRequest.PageSize,
            query,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns a task by its identifier.
    /// </summary>
    [HttpGet("{issueId:guid}", Name = "GetIssue")]
    [ProducesResponseType<IssueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueResponse>> GetAsync(
        Guid projectId,
        Guid issueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var issue = await issueService.GetAsync(
            projectId,
            issueId,
            accountId,
            cancellationToken);
        return issue is null ? NotFound() : Ok(issue);
    }

    /// <summary>
    /// Creates a task in the project.
    /// </summary>
    [HttpPost(Name = "CreateIssue")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<IssueResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueResponse>> CreateAsync(
        Guid projectId,
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateRequest(request);
        if (validation is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(validation));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await issueService.CreateAsync(
            projectId,
            accountId,
            request,
            cancellationToken);
        if (result.Outcome == IssueMutationOutcome.Succeeded)
        {
            return CreatedAtRoute(
                "GetIssue",
                new { projectId, issueId = result.Issue!.Id },
                result.Issue);
        }

        return MapMutation(result);
    }

    /// <summary>
    /// Replaces the editable content of a task.
    /// </summary>
    [HttpPut("{issueId:guid}", Name = "UpdateIssue")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<IssueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueResponse>> UpdateAsync(
        Guid projectId,
        Guid issueId,
        UpdateIssueRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateUpdateRequest(request);
        if (validation is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(validation));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await issueService.UpdateAsync(
            projectId,
            issueId,
            accountId,
            request,
            cancellationToken);
        return MapMutation(result);
    }

    #endregion

    #region Status Operations

    /// <summary>
    /// Changes the workflow status of a task.
    /// </summary>
    [HttpPut("{issueId:guid}/status", Name = "UpdateIssueStatus")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<IssueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueResponse>> UpdateStatusAsync(
        Guid projectId,
        Guid issueId,
        UpdateIssueStatusRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.StatusCode))
        {
            errors["statusCode"] = ["A valid status code is required."];
        }

        if (request.Version < 1)
        {
            errors["version"] = ["Issue version must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await issueService.ChangeStatusAsync(
            projectId,
            issueId,
            accountId,
            request,
            cancellationToken);
        return MapMutation(result);
    }

    #endregion

    #region Assignment Operations

    /// <summary>
    /// Changes the assignee of a task.
    /// </summary>
    [HttpPut("{issueId:guid}/assignee", Name = "UpdateIssueAssignee")]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<IssueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueResponse>> UpdateAssigneeAsync(
        Guid projectId,
        Guid issueId,
        UpdateIssueAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version < 1)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["version"] = ["Task version must be greater than zero."],
                }));
        }

        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var result = await issueService.ChangeAssigneeAsync(
            projectId,
            issueId,
            accountId,
            request,
            cancellationToken);
        return MapMutation(result);
    }

    #endregion

    #region Metadata Operations

    /// <summary>
    /// Returns the available task types, statuses, and priorities.
    /// </summary>
    [HttpGet("metadata", Name = "GetIssueMetadata")]
    [ProducesResponseType<IssueMetadataResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueMetadataResponse>> GetMetadataAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
        {
            return Unauthorized();
        }

        var metadata = await issueService.GetMetadataAsync(
            projectId,
            accountId,
            cancellationToken);
        return metadata is null ? NotFound() : Ok(metadata);
    }

    #endregion

    private bool TryGetAccountId(out Guid accountId)
    {
        return Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out accountId);
    }

    private ActionResult<IssueResponse> MapMutation(IssueMutationResult result)
    {
        return result.Outcome switch
        {
            IssueMutationOutcome.Succeeded => Ok(result.Issue),
            IssueMutationOutcome.Forbidden => Forbid(),
            IssueMutationOutcome.NotFound => NotFound(),
            IssueMutationOutcome.InvalidOption => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["option"] = ["One or more issue options are invalid."],
                })),
            IssueMutationOutcome.InvalidAssignee => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["assigneeAccountId"] = ["The assignee must be an active project member."],
                })),
            IssueMutationOutcome.ProjectInactive => ConflictProblem(
                "https://khaikang.dev/problems/projects/inactive",
                "project_inactive",
                "The project is inactive and its tasks are read-only."),
            IssueMutationOutcome.VersionConflict => ConflictProblem(
                "https://khaikang.dev/problems/issues/version-conflict",
                "issue_version_conflict",
                "The issue was changed by another user. Reload and try again."),
            IssueMutationOutcome.NumberConflict => ConflictProblem(
                "https://khaikang.dev/problems/issues/number-conflict",
                "issue_number_conflict",
                "The issue number conflicted with another request. Try again."),
            _ => NotFound(),
        };
    }

    private ObjectResult ConflictProblem(string type, string code, string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            type: type,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private static Dictionary<string, string[]>? ValidateCreateRequest(
        CreateIssueRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        {
            errors["title"] = ["Task title is required and cannot exceed 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.TypeCode))
        {
            errors["typeCode"] = ["Issue type is required."];
        }

        if (request.PriorityCode is not null &&
            string.IsNullOrWhiteSpace(request.PriorityCode))
        {
            errors["priorityCode"] = ["Task priority cannot be empty when provided."];
        }

        ValidateText(request.Description, "description", errors);
        ValidateText(request.UserStory, "userStory", errors);
        ValidateText(request.DefinitionOfDone, "definitionOfDone", errors);
        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string[]>? ValidateListQuery(IssueListQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Search?.Length > 200)
        {
            errors["search"] = ["Search cannot exceed 200 characters."];
        }

        if (query.AssigneeAccountId.HasValue && query.Unassigned == true)
        {
            errors["unassigned"] = ["Assignee and unassigned filters cannot be combined."];
        }

        if (!IsOneOf(query.SortBy, "updatedAt", "issueNo"))
        {
            errors["sortBy"] = ["Sort by must be updatedAt or issueNo."];
        }

        if (!IsOneOf(query.SortDirection, "asc", "desc"))
        {
            errors["sortDirection"] = ["Sort direction must be asc or desc."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static bool IsOneOf(string? value, params string[] allowedValues)
    {
        return string.IsNullOrWhiteSpace(value) ||
            allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string[]>? ValidateUpdateRequest(
        UpdateIssueRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        {
            errors["title"] = ["Task title is required and cannot exceed 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.TypeCode))
        {
            errors["typeCode"] = ["Task type is required."];
        }

        if (string.IsNullOrWhiteSpace(request.PriorityCode))
        {
            errors["priorityCode"] = ["Task priority is required."];
        }

        ValidateText(request.Description, "description", errors);
        ValidateText(request.UserStory, "userStory", errors);
        ValidateText(request.DefinitionOfDone, "definitionOfDone", errors);
        ValidateText(request.CompletionSummary, "completionSummary", errors);
        if (request.Version < 1)
        {
            errors["version"] = ["Task version must be greater than zero."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static void ValidateText(
        string? value,
        string field,
        IDictionary<string, string[]> errors)
    {
        const int maximumLength = 20_000;
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"{field} cannot exceed {maximumLength} characters."];
        }
    }
}
