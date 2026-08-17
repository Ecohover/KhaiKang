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
