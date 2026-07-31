using System.Security.Claims;
using System.Text.RegularExpressions;
using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KhaiKang.Modules.ProjectManagement.Endpoints;

public static partial class ProjectManagementEndpointExtensions
{
    public static IEndpointRouteBuilder MapProjectManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/v1/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        projects.MapGet("/", async (
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            return TryGetAccountId(principal, out var accountId)
                ? Results.Ok(await service.ListAsync(accountId, cancellationToken))
                : Results.Unauthorized();
        })
        .WithName("ListProjects")
        .Produces<IReadOnlyList<ProjectResponse>>()
        .Produces(StatusCodes.Status401Unauthorized);

        projects.MapGet("/{projectId:guid}", async (
            Guid projectId,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var project = await service.GetAsync(projectId, accountId, cancellationToken);
            return project is null ? Results.NotFound() : Results.Ok(project);
        })
        .WithName("GetProject")
        .Produces<ProjectResponse>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        projects.MapPost("/", async (
            CreateProjectRequest request,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var result = await service.CreateAsync(accountId, request, cancellationToken);
            return result.Outcome == CreateProjectOutcome.CodeConflict
                ? Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/projects/code-conflict",
                    "project_code_conflict",
                    "The project code is already in use.")
                : Results.Created($"/api/v1/projects/{result.Project!.Id}", result.Project);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .RequireAuthorization(ProjectManagementConstants.ProjectCreatePermission)
        .WithName("CreateProject")
        .Produces<ProjectResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapPut("/{projectId:guid}", async (
            Guid projectId,
            UpdateProjectRequest request,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateUpdateRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var result = await service.UpdateAsync(
                projectId,
                accountId,
                request,
                principal.HasClaim(
                    ProjectManagementConstants.PermissionClaimType,
                    ProjectManagementConstants.ProjectDeactivatePermission),
                cancellationToken);
            return result.Outcome switch
            {
                UpdateProjectOutcome.Succeeded => Results.Ok(result.Project),
                UpdateProjectOutcome.Forbidden => Results.Forbid(),
                UpdateProjectOutcome.VersionConflict => Problem(
                    StatusCodes.Status409Conflict,
                    "https://khaikang.dev/problems/projects/version-conflict",
                    "project_version_conflict",
                    "The project was changed by another user. Reload it and try again."),
                _ => Results.NotFound(),
            };
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("UpdateProject")
        .Produces<ProjectResponse>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapGet("/{projectId:guid}/roles", async (
            Guid projectId,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var roles = await service.ListRolesAsync(projectId, accountId, cancellationToken);
            return roles is null ? Results.NotFound() : Results.Ok(roles);
        })
        .WithName("ListProjectRoles")
        .Produces<IReadOnlyList<ProjectRoleResponse>>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        projects.MapGet("/{projectId:guid}/members", async (
            Guid projectId,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var members = await service.ListMembersAsync(projectId, accountId, cancellationToken);
            return members is null ? Results.NotFound() : Results.Ok(members);
        })
        .WithName("ListProjectMembers")
        .Produces<IReadOnlyList<ProjectMemberResponse>>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        projects.MapPost("/{projectId:guid}/members", async (
            Guid projectId,
            AddProjectMemberRequest request,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateMemberRequest(request.Username, request.RoleCodes);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var result = await service.AddMemberAsync(
                projectId,
                accountId,
                request,
                cancellationToken);
            return MapMemberMutation(result, StatusCodes.Status201Created);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("AddProjectMember")
        .Produces<ProjectMemberResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapPut("/{projectId:guid}/members/{memberId:guid}/roles", async (
            Guid projectId,
            Guid memberId,
            UpdateProjectMemberRolesRequest request,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateMemberRequest(null, request.RoleCodes, request.Version);
            if (validation is not null)
            {
                return validation;
            }

            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var result = await service.UpdateMemberRolesAsync(
                projectId,
                memberId,
                accountId,
                request,
                cancellationToken);
            return MapMemberMutation(result, StatusCodes.Status200OK);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("UpdateProjectMemberRoles")
        .Produces<ProjectMemberResponse>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapDelete("/{projectId:guid}/members/{memberId:guid}", async (
            Guid projectId,
            Guid memberId,
            int version,
            ClaimsPrincipal principal,
            ProjectManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (version < 1)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["version"] = ["Member version must be greater than zero."],
                });
            }

            if (!TryGetAccountId(principal, out var accountId))
            {
                return Results.Unauthorized();
            }

            var result = await service.RemoveMemberAsync(
                projectId,
                memberId,
                accountId,
                version,
                cancellationToken);
            return result.Outcome == ProjectMemberMutationOutcome.Succeeded
                ? Results.NoContent()
                : MapMemberMutation(result, StatusCodes.Status200OK);
        })
        .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
        .WithName("RemoveProjectMember")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static IResult? ValidateCreateRequest(CreateProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateName(request.Name, errors);
        if (string.IsNullOrWhiteSpace(request.Code) ||
            request.Code.Length > 100 ||
            !ProjectCodePattern().IsMatch(request.Code.Trim()))
        {
            errors["code"] =
                ["Project code must be 1-100 letters, numbers, hyphens, or underscores."];
        }

        ValidateDescription(request.Description, errors);
        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult? ValidateUpdateRequest(UpdateProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateName(request.Name, errors);
        ValidateDescription(request.Description, errors);
        if (request.Status is not ("active" or "inactive"))
        {
            errors["status"] = ["Project status must be active or inactive."];
        }

        if (request.Version < 1)
        {
            errors["version"] = ["Project version must be greater than zero."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static void ValidateName(string name, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            errors["name"] = ["Project name is required and cannot exceed 200 characters."];
        }
    }

    private static void ValidateDescription(
        string? description,
        IDictionary<string, string[]> errors)
    {
        if (description?.Length > 4000)
        {
            errors["description"] = ["Project description cannot exceed 4000 characters."];
        }
    }

    private static IResult? ValidateMemberRequest(
        string? username,
        IReadOnlyList<string> roleCodes,
        int? version = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (username is not null &&
            (string.IsNullOrWhiteSpace(username) || username.Length > 200))
        {
            errors["username"] = ["Username is required and cannot exceed 200 characters."];
        }

        if (roleCodes.Count is < 1 or > 4 || roleCodes.Any(string.IsNullOrWhiteSpace))
        {
            errors["roleCodes"] = ["Select between one and four valid project roles."];
        }

        if (version is < 1)
        {
            errors["version"] = ["Member version must be greater than zero."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult MapMemberMutation(
        ProjectMemberMutationResult result,
        int successStatus)
    {
        return result.Outcome switch
        {
            ProjectMemberMutationOutcome.Succeeded when successStatus == StatusCodes.Status201Created =>
                Results.Json(result.Member, statusCode: StatusCodes.Status201Created),
            ProjectMemberMutationOutcome.Succeeded => Results.Ok(result.Member),
            ProjectMemberMutationOutcome.Forbidden => Results.Forbid(),
            ProjectMemberMutationOutcome.NotFound or ProjectMemberMutationOutcome.AccountNotFound =>
                Results.NotFound(),
            ProjectMemberMutationOutcome.InvalidRoles => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["roleCodes"] = ["One or more project roles are invalid."],
                }),
            ProjectMemberMutationOutcome.AlreadyMember => Problem(
                StatusCodes.Status409Conflict,
                "https://khaikang.dev/problems/projects/member-already-active",
                "project_member_already_active",
                "The account is already an active project member."),
            ProjectMemberMutationOutcome.LastOwner => Problem(
                StatusCodes.Status409Conflict,
                "https://khaikang.dev/problems/projects/last-owner",
                "project_last_owner_required",
                "The project must keep at least one active Owner."),
            ProjectMemberMutationOutcome.VersionConflict => Problem(
                StatusCodes.Status409Conflict,
                "https://khaikang.dev/problems/projects/member-version-conflict",
                "project_member_version_conflict",
                "The project member was changed by another user. Reload and try again."),
            _ => Results.NotFound(),
        };
    }

    private static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId)
    {
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
    }

    private static IResult Problem(int status, string type, string code, string detail)
    {
        return Results.Problem(
            statusCode: status,
            type: type,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$")]
    private static partial Regex ProjectCodePattern();
}
