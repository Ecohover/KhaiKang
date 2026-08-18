namespace KhaiKang.Modules.ProjectManagement.Contracts;

public enum CreateIssueCommandOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidOption,
    InvalidAssignee,
    ProjectInactive,
    Conflict,
}
