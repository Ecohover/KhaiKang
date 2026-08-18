namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestSuiteRequest
{
    public required Guid? ParentId { get; init; }

    public required string Name { get; init; }

    public required string? Description { get; init; }

    public required int SortOrder { get; init; }
}
