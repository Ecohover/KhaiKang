namespace KhaiKang.Modules.ProjectManagement.Application;

public enum RemoveProjectMemberOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    LastOwner,
    VersionConflict,
}
