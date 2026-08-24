namespace KhaiKang.Modules.CommentManagement.Contracts;

public sealed record CommentResourceAccess
{
    public required bool CanRead { get; init; }
    public required bool CanCreate { get; init; }
}
