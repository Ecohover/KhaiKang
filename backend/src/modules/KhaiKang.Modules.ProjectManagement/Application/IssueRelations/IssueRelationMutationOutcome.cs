namespace KhaiKang.Modules.ProjectManagement.Application;

public enum IssueRelationMutationOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    InvalidType,
    InvalidDirection,
    InvalidTarget,
    InvalidParentChildTypes,
    SelfRelation,
    ProjectInactive,
    Duplicate,
    ParentConflict,
    HierarchyCycle,
    VersionConflict,
    IdempotencyConflict,
}
