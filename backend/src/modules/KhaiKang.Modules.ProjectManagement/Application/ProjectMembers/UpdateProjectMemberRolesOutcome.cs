namespace KhaiKang.Modules.ProjectManagement.Application;

public enum UpdateProjectMemberRolesOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidRoles,
    LastOwner,
    VersionConflict,
}
