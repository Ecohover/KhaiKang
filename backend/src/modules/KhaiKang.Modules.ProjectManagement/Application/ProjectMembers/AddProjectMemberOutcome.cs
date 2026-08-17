namespace KhaiKang.Modules.ProjectManagement.Application;

public enum AddProjectMemberOutcome
{
    Succeeded,
    NotFound,
    AccountNotFound,
    Forbidden,
    AlreadyMember,
    InvalidRoles,
}
