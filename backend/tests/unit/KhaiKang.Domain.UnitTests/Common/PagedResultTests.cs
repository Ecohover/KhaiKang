using System.Text.Json;
using KhaiKang.CommonUtils.Models;

namespace KhaiKang.Domain.UnitTests.Common;

public sealed class PagedResultTests
{
    [Fact]
    public void Result_ExposesCanonicalJsonShapeAndNavigationState()
    {
        var result = new PagedResult<string>
        {
            Items = ["first", "second"],
            Page = 2,
            PageSize = 2,
            TotalCount = 5,
        };

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(result, JsonSerializerOptions.Web));

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(
            ["hasNextPage", "hasPreviousPage", "items", "page", "pageSize", "totalCount", "totalPages"],
            document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }
}
