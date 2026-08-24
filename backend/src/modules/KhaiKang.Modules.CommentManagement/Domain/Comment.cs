using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Modules.CommentManagement.Domain;

public sealed class Comment
{
    private Comment()
    {
    }

    public Comment(CommentCreation creation)
    {
        Id = creation.Id;
        ResourceType = creation.ResourceType;
        ResourceId = creation.ResourceId;
        AuthorAccountId = creation.Actor.AccountId;
        AuthorDisplayName = creation.Actor.DisplayName;
        AuthorAccountType = creation.Actor.AccountType;
        AuthorFixedRole = creation.Actor.FixedRole;
        AuthorOwnerAccountId = creation.Actor.OwnerAccountId;
        AuthorOwnerDisplayName = creation.Actor.OwnerDisplayName;
        Content = creation.Content;
        CreatedAt = creation.CreatedAt;
        CreatedByAccountId = creation.Actor.AccountId;
        UpdatedAt = creation.CreatedAt;
        UpdatedByAccountId = creation.Actor.AccountId;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public CommentResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid AuthorAccountId { get; private set; }
    public string AuthorDisplayName { get; private set; } = null!;
    public string AuthorAccountType { get; private set; } = null!;
    public string? AuthorFixedRole { get; private set; }
    public Guid? AuthorOwnerAccountId { get; private set; }
    public string? AuthorOwnerDisplayName { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; }
}
