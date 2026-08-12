using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ProjectMemberRequestContractTests
{
    [Fact]
    public void AddProjectMemberRequest_UsesCanonicalJsonShape()
    {
        var request = new AddProjectMemberRequest("reviewer", ["contributor"]);

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web));

        AssertPropertyNames(document.RootElement, "roleCodes", "username");
        Assert.Equal("reviewer", document.RootElement.GetProperty("username").GetString());
        Assert.Equal(
            "contributor",
            document.RootElement.GetProperty("roleCodes")[0].GetString());
    }

    [Fact]
    public void UpdateProjectMemberRolesRequest_DeserializesCanonicalJson()
    {
        const string json = """
            {
              "roleCodes": ["reviewer"],
              "version": 3
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateProjectMemberRolesRequest>(
            json,
            JsonSerializerOptions.Web);

        Assert.NotNull(request);
        Assert.Equal("reviewer", Assert.Single(request.RoleCodes));
        Assert.Equal(3, request.Version);
    }

    private static void AssertPropertyNames(JsonElement element, params string[] expectedNames)
    {
        Assert.Equal(
            expectedNames.Order(StringComparer.Ordinal),
            element
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }
}
