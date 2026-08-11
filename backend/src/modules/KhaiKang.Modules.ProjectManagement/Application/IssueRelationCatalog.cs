namespace KhaiKang.Modules.ProjectManagement.Application;

public static class IssueRelationCatalog
{
    public const string Symmetric = "symmetric";
    public const string Directed = "directed";
    public const string Hierarchical = "hierarchical";

    public static readonly Guid RelatedId = Guid.Parse("2f01d4ad-70e3-4c7f-9b7c-27f5d32001a1");
    public static readonly Guid ParentOfId = Guid.Parse("9fe7e36b-a461-4bfa-8ba8-3e5d201184c2");
    public static readonly Guid BlocksId = Guid.Parse("04fd64b0-e17c-41a4-9907-d36dc630b377");
    public static readonly Guid DuplicatesId = Guid.Parse("5be46e72-6c93-4766-8599-31e90ed45248");
    public static readonly Guid TestsId = Guid.Parse("c8d1fd1f-3528-4b20-bb6e-febda1adcb71");

    public static readonly IssueRelationTypeDefinition[] Types =
    [
        new(RelatedId, "related", "Relates to", "Relates to", Symmetric, 1),
        new(ParentOfId, "parent_of", "Parent of", "Child of", Hierarchical, 2),
        new(BlocksId, "blocks", "Blocks", "Blocked by", Directed, 3),
        new(DuplicatesId, "duplicates", "Duplicates", "Duplicated by", Directed, 4),
        new(TestsId, "tests", "Tests / verifies", "Tested / verified by", Directed, 5),
    ];
}

public sealed record IssueRelationTypeDefinition(
    Guid Id,
    string Code,
    string ForwardLabel,
    string ReverseLabel,
    string DirectionKind,
    int SortOrder);
