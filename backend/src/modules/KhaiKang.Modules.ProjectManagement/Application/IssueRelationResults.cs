using KhaiKang.Modules.ProjectManagement.Contracts;

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

public sealed record IssueRelationMutationResult(
    IssueRelationMutationOutcome Outcome,
    IssueRelationResponse? Relation = null);
