using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public enum IssueMutationOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidOption,
    InvalidAssignee,
    ProjectInactive,
    VersionConflict,
    NumberConflict,
}

public sealed record IssueMutationResult(
    IssueMutationOutcome Outcome,
    IssueResponse? Issue = null);
