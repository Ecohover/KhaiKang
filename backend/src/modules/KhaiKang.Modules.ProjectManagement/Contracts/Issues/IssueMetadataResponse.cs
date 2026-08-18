namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueMetadataResponse
{
    public required IReadOnlyList<IssueOptionResponse> Types { get; init; }

    public required IReadOnlyList<IssueOptionResponse> Statuses { get; init; }

    public required IReadOnlyList<IssueOptionResponse> Priorities { get; init; }
}
