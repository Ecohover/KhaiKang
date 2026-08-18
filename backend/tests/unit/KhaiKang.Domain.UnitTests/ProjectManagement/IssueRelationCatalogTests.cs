using KhaiKang.Modules.ProjectManagement.Application;

namespace KhaiKang.Domain.UnitTests;

public sealed class IssueRelationCatalogTests
{
    [Fact]
    public void Types_DefinesTheFiveAcceptedRelationKindsInDisplayOrder()
    {
        var relationTypes = IssueRelationCatalog.Types;

        Assert.Equal(
            ["related", "parent_of", "blocks", "duplicates", "tests"],
            relationTypes.Select(type => type.Code));
        Assert.Equal([1, 2, 3, 4, 5], relationTypes.Select(type => type.SortOrder));
        Assert.Equal(5, relationTypes.Select(type => type.Id).Distinct().Count());
    }

    [Fact]
    public void Types_PreservesDirectionSemantics()
    {
        var relationTypes = IssueRelationCatalog.Types.ToDictionary(type => type.Code);

        Assert.Equal(IssueRelationCatalog.Symmetric, relationTypes["related"].DirectionKind);
        Assert.Equal(IssueRelationCatalog.Hierarchical, relationTypes["parent_of"].DirectionKind);
        Assert.Equal(IssueRelationCatalog.Directed, relationTypes["blocks"].DirectionKind);
        Assert.Equal(IssueRelationCatalog.Directed, relationTypes["duplicates"].DirectionKind);
        Assert.Equal(IssueRelationCatalog.Directed, relationTypes["tests"].DirectionKind);
    }
}
