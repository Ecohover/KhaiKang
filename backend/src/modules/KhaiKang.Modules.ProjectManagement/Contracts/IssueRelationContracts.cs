namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueRelationTypeResponse(
    Guid Id,
    string Code,
    string ForwardLabel,
    string ReverseLabel,
    string DirectionKind);

public sealed record IssueRelationIssueResponse(
    Guid Id,
    int IssueNo,
    string Key,
    string Title,
    string TypeCode,
    string StatusCode);

public sealed record IssueRelationResponse(
    Guid Id,
    Guid ProjectId,
    string RelationTypeCode,
    string ForwardLabel,
    string ReverseLabel,
    string DirectionKind,
    IssueRelationIssueResponse SourceIssue,
    IssueRelationIssueResponse TargetIssue,
    DateTimeOffset CreatedAt,
    int Version);

public sealed record CreateIssueRelationRequest(
    string RelationTypeCode,
    Guid RelatedIssueId,
    string Direction);
