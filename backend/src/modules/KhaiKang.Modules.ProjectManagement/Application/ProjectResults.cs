using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public enum CreateProjectOutcome
{
    Succeeded,
    CodeConflict,
}

public sealed record CreateProjectResult(
    CreateProjectOutcome Outcome,
    ProjectResponse? Project = null);

public enum UpdateProjectOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    VersionConflict,
}

public sealed record UpdateProjectResult(
    UpdateProjectOutcome Outcome,
    ProjectResponse? Project = null);

public enum ProjectMemberMutationOutcome
{
    Succeeded,
    NotFound,
    AccountNotFound,
    Forbidden,
    AlreadyMember,
    InvalidRoles,
    LastOwner,
    VersionConflict,
}

public sealed record ProjectMemberMutationResult(
    ProjectMemberMutationOutcome Outcome,
    ProjectMemberResponse? Member = null);
