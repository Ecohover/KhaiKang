namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CommentPageResponse
{
    public required IReadOnlyList<CommentResponse> Items { get; init; }
    public required int Page { get; init; }
    public required bool HasNextPage { get; init; }
    public required bool CanCreate { get; init; }
}
