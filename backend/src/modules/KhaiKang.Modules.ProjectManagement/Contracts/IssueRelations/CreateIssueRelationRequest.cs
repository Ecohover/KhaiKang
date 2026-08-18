namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record CreateIssueRelationRequest
{
    public CreateIssueRelationRequest(
        string relationTypeCode,
        Guid relatedIssueId,
        string direction)
    {
        RelationTypeCode = relationTypeCode;
        RelatedIssueId = relatedIssueId;
        Direction = direction;
    }

    public string RelationTypeCode { get; }

    public Guid RelatedIssueId { get; }

    public string Direction { get; }
}
