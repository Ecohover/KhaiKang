using KhaiKang.Modules.TestManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Infrastructure;

public sealed class TestManagementDbContext(DbContextOptions<TestManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<TestWorkspace> Workspaces => Set<TestWorkspace>();
    public DbSet<TestWorkspaceMember> Members => Set<TestWorkspaceMember>();
    public DbSet<TestSuite> Suites => Set<TestSuite>();
    public DbSet<TestCase> Cases => Set<TestCase>();
    public DbSet<TestStep> CaseSteps => Set<TestStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<AccountReference>();
        account.ToTable("accounts", table => table.ExcludeFromMigrations());
        account.HasKey(x => x.Id);
        account.Property(x => x.Id).HasColumnName("id");

        var workspace = modelBuilder.Entity<TestWorkspace>();
        workspace.ToTable("test_workspaces");
        workspace.HasKey(x => x.Id).HasName("pk_test_workspaces");
        workspace.Property(x => x.Id).HasColumnName("id");
        workspace.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        workspace.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_test_workspaces_name");
        workspace.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(10);
        workspace.HasIndex(x => x.Prefix).IsUnique().HasDatabaseName("uq_test_workspaces_prefix");
        workspace.Property(x => x.Description).HasColumnName("description");
        workspace.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(workspace);

        var member = modelBuilder.Entity<TestWorkspaceMember>();
        member.ToTable("test_workspace_members");
        member.HasKey(x => x.Id).HasName("pk_test_workspace_members");
        member.Property(x => x.Id).HasColumnName("id");
        member.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        member.Property(x => x.AccountId).HasColumnName("account_id");
        member.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
        member.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        member.Property(x => x.JoinedAt).HasColumnName("joined_at");
        member.Property(x => x.RemovedAt).HasColumnName("removed_at");
        Audit(member);
        member.HasIndex(x => x.AccountId).HasDatabaseName("idx_test_workspace_members_account_id");
        member.HasIndex(x => new { x.TestWorkspaceId, x.Status })
            .HasDatabaseName("idx_test_workspace_members_workspace_status");
        member.HasIndex(x => new { x.TestWorkspaceId, x.AccountId })
            .IsUnique().HasDatabaseName("uq_test_workspace_members_workspace_account");
        member.HasOne(x => x.Workspace).WithMany(x => x.Members)
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_workspace_members_workspace");
        member.HasOne<AccountReference>().WithMany()
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_workspace_members_account");

        var suite = modelBuilder.Entity<TestSuite>();
        suite.ToTable("test_suites");
        suite.HasKey(x => x.Id).HasName("pk_test_suites");
        suite.Property(x => x.Id).HasColumnName("id");
        suite.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        suite.Property(x => x.ParentId).HasColumnName("parent_id");
        suite.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        suite.Property(x => x.Description).HasColumnName("description");
        suite.Property(x => x.SortOrder).HasColumnName("sort_order");
        suite.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(suite);
        suite.HasIndex(x => new { x.TestWorkspaceId, x.ParentId, x.SortOrder })
            .HasDatabaseName("idx_test_suites_workspace_parent_sort_order");
        suite.HasOne(x => x.Workspace).WithMany(x => x.Suites)
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_suites_workspace");
        suite.HasOne<TestSuite>().WithMany().HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_test_suites_parent");

        var testCase = modelBuilder.Entity<TestCase>();
        testCase.ToTable("test_cases");
        testCase.HasKey(x => x.Id).HasName("pk_test_cases");
        testCase.Property(x => x.Id).HasColumnName("id");
        testCase.Property(x => x.TestSuiteId).HasColumnName("test_suite_id");
        testCase.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        testCase.Property(x => x.Description).HasColumnName("description");
        testCase.Property(x => x.Preconditions).HasColumnName("preconditions");
        testCase.Property(x => x.OverallExpectedResult).HasColumnName("overall_expected_result");
        testCase.Property(x => x.SortOrder).HasColumnName("sort_order");
        testCase.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(testCase);
        testCase.HasIndex(x => x.TestSuiteId).HasDatabaseName("idx_test_cases_test_suite_id");
        testCase.HasIndex(x => new { x.TestSuiteId, x.Status })
            .HasDatabaseName("idx_test_cases_test_suite_status");
        testCase.HasIndex(x => new { x.TestSuiteId, x.SortOrder })
            .HasDatabaseName("idx_test_cases_test_suite_sort_order");
        testCase.HasOne(x => x.Suite).WithMany()
            .HasForeignKey(x => x.TestSuiteId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_cases_test_suite");

        var step = modelBuilder.Entity<TestStep>();
        step.ToTable("test_case_steps");
        step.HasKey(x => x.Id).HasName("pk_test_case_steps");
        step.Property(x => x.Id).HasColumnName("id");
        step.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        step.Property(x => x.StepNo).HasColumnName("step_no");
        step.Property(x => x.Action).HasColumnName("action");
        step.Property(x => x.ExpectedResult).HasColumnName("expected_result");
        Audit(step, concurrencyToken: false);
        step.HasIndex(x => x.TestCaseId).HasDatabaseName("idx_test_case_steps_test_case_id");
        step.HasIndex(x => new { x.TestCaseId, x.StepNo })
            .IsUnique().HasDatabaseName("uq_test_case_steps_case_step_no");
        step.HasOne(x => x.TestCase).WithMany(x => x.Steps)
            .HasForeignKey(x => x.TestCaseId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_case_steps_test_case");
    }

    private static void Audit<TEntity>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity,
        bool concurrencyToken = true)
        where TEntity : class
    {
        entity.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        entity.Property<Guid?>("CreatedByAccountId").HasColumnName("created_by_account_id");
        entity.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        entity.Property<Guid?>("UpdatedByAccountId").HasColumnName("updated_by_account_id");
        var version = entity.Property<int>("Version").HasColumnName("version");
        if (concurrencyToken)
        {
            version.IsConcurrencyToken();
        }
    }
}
