namespace KhaiKang.Modules.ProjectManagement.Domain;

public enum IssueRelationIdempotencyState
{
    IncompleteTransientFailure,
    TerminalSuccess,
    TerminalBusinessRejection,
}
