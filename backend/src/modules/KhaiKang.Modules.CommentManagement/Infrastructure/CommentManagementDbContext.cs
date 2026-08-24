using KhaiKang.Modules.CommentManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.CommentManagement.Infrastructure;

public sealed class CommentManagementDbContext(DbContextOptions<CommentManagementDbContext> options) : DbContext(options)
{
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentIdempotencyRecord> IdempotencyRecords => Set<CommentIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var comment = modelBuilder.Entity<Comment>();
        comment.ToTable("comments", table =>
        {
            table.HasCheckConstraint(
                "ck_comments_resource_type",
                "resource_type IN ('issue', 'test_case', 'test_plan', 'test_run')");
            table.HasCheckConstraint(
                "ck_comments_author_account_type",
                "author_account_type IN ('human', 'ai_agent')");
            table.HasCheckConstraint("ck_comments_version", "version >= 1");
        });
        comment.HasKey(item => item.Id).HasName("pk_comments");
        comment.Property(item => item.Id).HasColumnName("id");
        comment.Property(item => item.ResourceType).HasColumnName("resource_type").HasMaxLength(30)
            .HasConversion(
                resourceType => ToResourceTypeCode(resourceType),
                resourceType => FromResourceTypeCode(resourceType));
        comment.Property(item => item.ResourceId).HasColumnName("resource_id");
        comment.Property(item => item.AuthorAccountId).HasColumnName("author_account_id");
        comment.Property(item => item.AuthorDisplayName).HasColumnName("author_display_name").HasMaxLength(100);
        comment.Property(item => item.AuthorAccountType).HasColumnName("author_account_type").HasMaxLength(20);
        comment.Property(item => item.AuthorFixedRole).HasColumnName("author_fixed_role").HasMaxLength(30);
        comment.Property(item => item.AuthorOwnerAccountId).HasColumnName("author_owner_account_id");
        comment.Property(item => item.AuthorOwnerDisplayName).HasColumnName("author_owner_display_name").HasMaxLength(100);
        comment.Property(item => item.Content).HasColumnName("content").HasMaxLength(10000);
        comment.Property(item => item.IsDeleted).HasColumnName("is_deleted");
        comment.Property(item => item.DeletedAt).HasColumnName("deleted_at");
        var commentCreatedAt = comment.Property(item => item.CreatedAt).HasColumnName("created_at");
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            commentCreatedAt.HasConversion(
                timestamp => timestamp.UtcTicks,
                ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        }
        comment.Property(item => item.CreatedByAccountId).HasColumnName("created_by_account_id");
        comment.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        comment.Property(item => item.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        comment.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        comment.HasIndex(item => new { item.ResourceType, item.ResourceId, item.CreatedAt, item.Id })
            .IsDescending(false, false, true, true)
            .HasDatabaseName("idx_comments_resource_created");
        comment.HasIndex(item => item.AuthorAccountId).HasDatabaseName("idx_comments_author");

        var idempotency = modelBuilder.Entity<CommentIdempotencyRecord>();
        idempotency.ToTable("comment_idempotency_records", table =>
        {
            table.HasCheckConstraint(
                "ck_comment_idempotency_state",
                "state IN ('incomplete_transient_failure', 'terminal_success', 'terminal_business_rejection')");
            table.HasCheckConstraint(
                "ck_comment_idempotency_fingerprint",
                "length(request_fingerprint) = 64");
            table.HasCheckConstraint(
                "ck_comment_idempotency_terminal_snapshot",
                "(state = 'incomplete_transient_failure' AND response_snapshot IS NULL AND rejection_snapshot IS NULL AND target_id IS NULL AND completed_at IS NULL) OR " +
                "(state = 'terminal_success' AND response_snapshot IS NOT NULL AND rejection_snapshot IS NULL AND target_id IS NOT NULL AND completed_at IS NOT NULL) OR " +
                "(state = 'terminal_business_rejection' AND response_snapshot IS NULL AND rejection_snapshot IS NOT NULL AND target_id IS NULL AND completed_at IS NOT NULL)");
        });
        idempotency.HasKey(item => item.Id).HasName("pk_comment_idempotency_records");
        idempotency.Property(item => item.Id).HasColumnName("id");
        idempotency.Property(item => item.ActorAccountId).HasColumnName("actor_account_id");
        idempotency.Property(item => item.OperationCode).HasColumnName("operation_code").HasMaxLength(100);
        idempotency.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        idempotency.Property(item => item.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        idempotency.Property(item => item.State).HasColumnName("state").HasMaxLength(50)
            .HasConversion(
                state => ToIdempotencyStateCode(state),
                state => FromIdempotencyStateCode(state));
        idempotency.Property(item => item.ResponseSnapshot).HasColumnName("response_snapshot").HasColumnType("jsonb");
        idempotency.Property(item => item.RejectionSnapshot).HasColumnName("rejection_snapshot").HasColumnType("jsonb");
        idempotency.Property(item => item.TargetId).HasColumnName("target_id");
        idempotency.Property(item => item.CreatedAt).HasColumnName("created_at");
        idempotency.Property(item => item.CompletedAt).HasColumnName("completed_at");
        idempotency.HasIndex(item => new { item.ActorAccountId, item.OperationCode, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_comment_idempotency_actor_operation_key");
    }

    private static string ToResourceTypeCode(Contracts.CommentResourceType resourceType) => resourceType switch
    {
        Contracts.CommentResourceType.Issue => "issue",
        Contracts.CommentResourceType.TestCase => "test_case",
        Contracts.CommentResourceType.TestPlan => "test_plan",
        Contracts.CommentResourceType.TestRun => "test_run",
        _ => throw new InvalidOperationException($"Unsupported comment resource type '{resourceType}'."),
    };

    private static Contracts.CommentResourceType FromResourceTypeCode(string resourceType) => resourceType switch
    {
        "issue" => Contracts.CommentResourceType.Issue,
        "test_case" => Contracts.CommentResourceType.TestCase,
        "test_plan" => Contracts.CommentResourceType.TestPlan,
        "test_run" => Contracts.CommentResourceType.TestRun,
        _ => throw new InvalidOperationException($"Unsupported persisted comment resource type '{resourceType}'."),
    };

    private static string ToIdempotencyStateCode(CommentIdempotencyState state) => state switch
    {
        CommentIdempotencyState.IncompleteTransientFailure => "incomplete_transient_failure",
        CommentIdempotencyState.TerminalSuccess => "terminal_success",
        CommentIdempotencyState.TerminalBusinessRejection => "terminal_business_rejection",
        _ => throw new InvalidOperationException($"Unsupported comment idempotency state '{state}'."),
    };

    private static CommentIdempotencyState FromIdempotencyStateCode(string state) => state switch
    {
        "incomplete_transient_failure" => CommentIdempotencyState.IncompleteTransientFailure,
        "terminal_success" => CommentIdempotencyState.TerminalSuccess,
        "terminal_business_rejection" => CommentIdempotencyState.TerminalBusinessRejection,
        _ => throw new InvalidOperationException($"Unsupported persisted comment idempotency state '{state}'."),
    };
}
