namespace KhaiKang.Modules.ProjectManagement.Application;

public enum IssueRelationMutationOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidType,
    InvalidDirection,
    SelfRelation,
    ProjectInactive,
    Duplicate,
    ParentConflict,
    HierarchyCycle,
    VersionConflict,
}
