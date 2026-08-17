namespace KhaiKang.Modules.ProjectManagement.Application;

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
