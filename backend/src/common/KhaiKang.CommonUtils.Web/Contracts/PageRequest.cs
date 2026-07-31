using System.ComponentModel.DataAnnotations;

namespace KhaiKang.CommonUtils.Web.Contracts;

/// <summary>
/// Defines the standard one-based paging parameters for collection endpoints.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = DefaultPage;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}
