using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.ProjectManagement.Infrastructure;

public sealed class ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectRole> ProjectRoles => Set<ProjectRole>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<ProjectMemberRole> ProjectMemberRoles => Set<ProjectMemberRole>();

    public DbSet<ProjectRolePermission> ProjectRolePermissions => Set<ProjectRolePermission>();

    public DbSet<ProjectAuditEvent> ProjectAuditEvents => Set<ProjectAuditEvent>();

    public DbSet<IssueType> IssueTypes => Set<IssueType>();

    public DbSet<IssueStatus> IssueStatuses => Set<IssueStatus>();

    public DbSet<IssuePriority> IssuePriorities => Set<IssuePriority>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<IssueAttachment> IssueAttachments => Set<IssueAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enforceAccountForeignKeys = Database.IsNpgsql();
        if (enforceAccountForeignKeys)
        {
            ConfigureAccountReference(modelBuilder);
        }

        ConfigureProject(modelBuilder, enforceAccountForeignKeys);
        ConfigureProjectRole(modelBuilder);
        ConfigurePermissionReference(modelBuilder, enforceAccountForeignKeys);
        ConfigureProjectRolePermission(modelBuilder);
        ConfigureProjectMember(modelBuilder, enforceAccountForeignKeys);
        ConfigureProjectMemberRole(modelBuilder);
        ConfigureIssueType(modelBuilder);
        ConfigureIssueStatus(modelBuilder);
        ConfigureIssuePriority(modelBuilder);
        ConfigureIssue(modelBuilder, enforceAccountForeignKeys);
        ConfigureIssueAttachment(modelBuilder, enforceAccountForeignKeys);
        ConfigureProjectAuditEvent(modelBuilder, enforceAccountForeignKeys);
    }

    private static void ConfigurePermissionReference(ModelBuilder modelBuilder, bool useSharedTable)
    {
        var permission = modelBuilder.Entity<PermissionReference>();
        if (useSharedTable)
        {
            permission.ToTable("permissions", table => table.ExcludeFromMigrations());
        }
        else
        {
            permission.ToTable("project_permission_references");
            permission.HasData(ProjectPermissionCatalog.All.Select(definition => new
            {
                definition.Id,
                definition.Code,
            }));
        }

        permission.HasKey(x => x.Id);
        permission.Property(x => x.Id).HasColumnName("id");
        permission.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
        permission.HasIndex(x => x.Code).IsUnique();
    }

    private static void ConfigureAccountReference(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<AccountReference>();
        account.ToTable("accounts", table => table.ExcludeFromMigrations());
        account.HasKey(x => x.Id);
        account.Property(x => x.Id).HasColumnName("id");
    }

    private static void ConfigureProject(ModelBuilder modelBuilder, bool enforceAccountForeignKeys)
    {
        var project = modelBuilder.Entity<Project>();
        project.ToTable("projects");
        project.HasKey(x => x.Id).HasName("pk_projects");
        project.Property(x => x.Id).HasColumnName("id");
        project.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
        project.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_projects_code");
        project.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        project.Property(x => x.Description).HasColumnName("description");
        project.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<ProjectStatus>(value, true));
        project.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        project.HasIndex(x => x.CreatedByAccountId)
            .HasDatabaseName("idx_projects_created_by_account_id");
        if (enforceAccountForeignKeys)
        {
            project.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_projects_created_by_account");
        }
        project.Property(x => x.CreatedAt).HasColumnName("created_at");
        project.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        project.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        project.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }

    private static void ConfigureProjectRole(ModelBuilder modelBuilder)
    {
        var role = modelBuilder.Entity<ProjectRole>();
        role.ToTable("project_roles");
        role.HasKey(x => x.Id).HasName("pk_project_roles");
        role.Property(x => x.Id).HasColumnName("id");
        role.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
        role.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_project_roles_code");
        role.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        role.Property(x => x.Description).HasColumnName("description");
        role.Property(x => x.IsSystem).HasColumnName("is_system");
        role.Property(x => x.IsActive).HasColumnName("is_active");
        role.Property(x => x.SortOrder).HasColumnName("sort_order");
        role.Property(x => x.CreatedAt).HasColumnName("created_at");
        role.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        role.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        role.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        role.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();

        var seededAt = new DateTimeOffset(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);
        role.HasData(
            SeedRole("4f5961ac-5a4b-49e1-a73c-451d43a39718", "owner", "Owner", "專案擁有者", 1, seededAt),
            SeedRole("836e894f-ca1d-4fd9-af14-b987882400dd", "manager", "Manager", "專案管理者", 2, seededAt),
            SeedRole("ead22957-af22-47eb-a7de-782145087141", "contributor", "Contributor", "專案參與者", 3, seededAt),
            SeedRole("c5684ccd-30b0-43aa-85ef-7f1c23835492", "reviewer", "Reviewer", "專案審查者", 4, seededAt));
    }

    private static void ConfigureProjectRolePermission(ModelBuilder modelBuilder)
    {
        var mapping = modelBuilder.Entity<ProjectRolePermission>();
        mapping.ToTable("project_role_permissions");
        mapping.HasKey(x => x.Id).HasName("pk_project_role_permissions");
        mapping.Property(x => x.Id).HasColumnName("id");
        mapping.Property(x => x.ProjectRoleId).HasColumnName("project_role_id");
        mapping.Property(x => x.PermissionId).HasColumnName("permission_id");
        mapping.Property(x => x.CreatedAt).HasColumnName("created_at");
        mapping.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        mapping.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        mapping.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        mapping.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        mapping.HasIndex(x => x.ProjectRoleId)
            .HasDatabaseName("idx_project_role_permissions_project_role_id");
        mapping.HasIndex(x => x.PermissionId)
            .HasDatabaseName("idx_project_role_permissions_permission_id");
        mapping.HasIndex(x => new { x.ProjectRoleId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("uq_project_role_permissions_role_permission");
        mapping.HasOne(x => x.ProjectRole)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.ProjectRoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_role_permissions_role");
        mapping.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_project_role_permissions_permission");

        var seededAt = new DateTimeOffset(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);
        mapping.HasData(ProjectPermissionCatalog.Mappings.Select(seed => new
        {
            seed.Id,
            seed.ProjectRoleId,
            seed.PermissionId,
            CreatedAt = seededAt,
            CreatedByAccountId = (Guid?)null,
            UpdatedAt = seededAt,
            UpdatedByAccountId = (Guid?)null,
            Version = 1,
        }));
    }

    private static object SeedRole(
        string id,
        string code,
        string name,
        string description,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        return new
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = createdAt,
            CreatedByAccountId = (Guid?)null,
            UpdatedAt = createdAt,
            UpdatedByAccountId = (Guid?)null,
            Version = 1,
        };
    }

    private static void ConfigureProjectMember(
        ModelBuilder modelBuilder,
        bool enforceAccountForeignKeys)
    {
        var member = modelBuilder.Entity<ProjectMember>();
        member.ToTable("project_members");
        member.HasKey(x => x.Id).HasName("pk_project_members");
        member.Property(x => x.Id).HasColumnName("id");
        member.Property(x => x.ProjectId).HasColumnName("project_id");
        member.Property(x => x.AccountId).HasColumnName("account_id");
        member.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        member.Property(x => x.JoinedAt).HasColumnName("joined_at");
        member.Property(x => x.RemovedAt).HasColumnName("removed_at");
        member.Property(x => x.CreatedAt).HasColumnName("created_at");
        member.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        member.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        member.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        member.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        member.HasIndex(x => x.ProjectId).HasDatabaseName("idx_project_members_project_id");
        member.HasIndex(x => x.AccountId).HasDatabaseName("idx_project_members_account_id");
        member.HasIndex(x => new { x.ProjectId, x.Status })
            .HasDatabaseName("idx_project_members_project_status");
        member.HasIndex(x => new { x.ProjectId, x.AccountId })
            .IsUnique()
            .HasFilter("status = 'active'")
            .HasDatabaseName("uq_project_members_active_member");
        member.HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_members_project");
        if (enforceAccountForeignKeys)
        {
            member.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_project_members_account");
        }
    }

    private static void ConfigureProjectMemberRole(ModelBuilder modelBuilder)
    {
        var mapping = modelBuilder.Entity<ProjectMemberRole>();
        mapping.ToTable("project_member_roles");
        mapping.HasKey(x => x.Id).HasName("pk_project_member_roles");
        mapping.Property(x => x.Id).HasColumnName("id");
        mapping.Property(x => x.ProjectMemberId).HasColumnName("project_member_id");
        mapping.Property(x => x.ProjectRoleId).HasColumnName("project_role_id");
        mapping.Property(x => x.CreatedAt).HasColumnName("created_at");
        mapping.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        mapping.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        mapping.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        mapping.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        mapping.HasIndex(x => x.ProjectMemberId)
            .HasDatabaseName("idx_project_member_roles_project_member_id");
        mapping.HasIndex(x => x.ProjectRoleId)
            .HasDatabaseName("idx_project_member_roles_project_role_id");
        mapping.HasIndex(x => new { x.ProjectMemberId, x.ProjectRoleId })
            .IsUnique()
            .HasDatabaseName("uq_project_member_roles_member_role");
        mapping.HasOne(x => x.ProjectMember)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.ProjectMemberId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_project_member_roles_member");
        mapping.HasOne(x => x.ProjectRole)
            .WithMany()
            .HasForeignKey(x => x.ProjectRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_project_member_roles_role");
    }

    private static void ConfigureIssueType(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssueType>();
        entity.ToTable("issue_types");
        entity.HasKey(x => x.Id).HasName("pk_issue_types");
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(x => x.Description).HasColumnName("description");
        entity.Property(x => x.IsSystem).HasColumnName("is_system");
        entity.Property(x => x.IsActive).HasColumnName("is_active");
        entity.Property(x => x.SortOrder).HasColumnName("sort_order");
        ConfigureIssueLookupAudit(entity);
        entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_issue_types_code");
        entity.HasData(IssueCatalog.Types.Select(item => SeedIssueLookup(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.SortOrder)));
    }

    private static void ConfigureIssueStatus(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssueStatus>();
        entity.ToTable("issue_statuses");
        entity.HasKey(x => x.Id).HasName("pk_issue_statuses");
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(x => x.Description).HasColumnName("description");
        entity.Property(x => x.Category).HasColumnName("category").HasMaxLength(20);
        entity.Property(x => x.IsSystem).HasColumnName("is_system");
        entity.Property(x => x.IsActive).HasColumnName("is_active");
        entity.Property(x => x.SortOrder).HasColumnName("sort_order");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        entity.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_issue_statuses_code");

        var seededAt = new DateTimeOffset(2026, 7, 22, 0, 0, 0, TimeSpan.Zero);
        entity.HasData(IssueCatalog.Statuses.Select(item => new
        {
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.Category,
            IsSystem = true,
            IsActive = true,
            item.SortOrder,
            CreatedAt = seededAt,
            CreatedByAccountId = (Guid?)null,
            UpdatedAt = seededAt,
            UpdatedByAccountId = (Guid?)null,
            Version = 1,
        }));
    }

    private static void ConfigureIssuePriority(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IssuePriority>();
        entity.ToTable("issue_priorities");
        entity.HasKey(x => x.Id).HasName("pk_issue_priorities");
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(x => x.Description).HasColumnName("description");
        entity.Property(x => x.IsSystem).HasColumnName("is_system");
        entity.Property(x => x.IsActive).HasColumnName("is_active");
        entity.Property(x => x.SortOrder).HasColumnName("sort_order");
        ConfigureIssueLookupAudit(entity);
        entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_issue_priorities_code");
        entity.HasData(IssueCatalog.Priorities.Select(item => SeedIssueLookup(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.SortOrder)));
    }

    private static void ConfigureIssue(
        ModelBuilder modelBuilder,
        bool enforceAccountForeignKeys)
    {
        var issue = modelBuilder.Entity<Issue>();
        issue.ToTable("issues");
        issue.HasKey(x => x.Id).HasName("pk_issues");
        issue.Property(x => x.Id).HasColumnName("id");
        issue.Property(x => x.ProjectId).HasColumnName("project_id");
        issue.Property(x => x.IssueNo).HasColumnName("issue_no");
        issue.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        issue.Property(x => x.Description).HasColumnName("description");
        issue.Property(x => x.UserStory).HasColumnName("user_story");
        issue.Property(x => x.DefinitionOfDone).HasColumnName("definition_of_done");
        issue.Property(x => x.IssueTypeId).HasColumnName("issue_type_id");
        issue.Property(x => x.IssueStatusId).HasColumnName("issue_status_id");
        issue.Property(x => x.IssuePriorityId).HasColumnName("issue_priority_id");
        issue.Property(x => x.ReporterAccountId).HasColumnName("reporter_account_id");
        issue.Property(x => x.AssigneeAccountId).HasColumnName("assignee_account_id");
        issue.Property(x => x.CompletionSummary).HasColumnName("completion_summary");
        issue.Property(x => x.CompletedAt).HasColumnName("completed_at");
        issue.Property(x => x.CreatedAt).HasColumnName("created_at");
        issue.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        issue.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        issue.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        issue.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        issue.HasIndex(x => x.ProjectId).HasDatabaseName("idx_issues_project_id");
        issue.HasIndex(x => x.IssueTypeId).HasDatabaseName("idx_issues_issue_type_id");
        issue.HasIndex(x => x.IssueStatusId).HasDatabaseName("idx_issues_issue_status_id");
        issue.HasIndex(x => x.IssuePriorityId).HasDatabaseName("idx_issues_issue_priority_id");
        issue.HasIndex(x => x.ReporterAccountId).HasDatabaseName("idx_issues_reporter_account_id");
        issue.HasIndex(x => x.AssigneeAccountId).HasDatabaseName("idx_issues_assignee_account_id");
        issue.HasIndex(x => new { x.ProjectId, x.IssueStatusId })
            .HasDatabaseName("idx_issues_project_status");
        issue.HasIndex(x => new { x.ProjectId, x.AssigneeAccountId })
            .HasDatabaseName("idx_issues_project_assignee");
        issue.HasIndex(x => new { x.ProjectId, x.IssueNo })
            .IsUnique()
            .HasDatabaseName("uq_issues_project_issue_no");
        issue.HasOne(x => x.Project)
            .WithMany(x => x.Issues)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_issues_project");
        issue.HasOne(x => x.IssueType)
            .WithMany()
            .HasForeignKey(x => x.IssueTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_issues_issue_type");
        issue.HasOne(x => x.IssueStatus)
            .WithMany()
            .HasForeignKey(x => x.IssueStatusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_issues_issue_status");
        issue.HasOne(x => x.IssuePriority)
            .WithMany()
            .HasForeignKey(x => x.IssuePriorityId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_issues_issue_priority");
        if (enforceAccountForeignKeys)
        {
            issue.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.ReporterAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_issues_reporter_account");
            issue.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.AssigneeAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_issues_assignee_account");
        }
    }

    private static void ConfigureIssueLookupAudit<TEntity>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        entity.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        entity.Property<Guid?>("CreatedByAccountId").HasColumnName("created_by_account_id");
        entity.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        entity.Property<Guid?>("UpdatedByAccountId").HasColumnName("updated_by_account_id");
        entity.Property<int>("Version").HasColumnName("version").IsConcurrencyToken();
    }

    private static void ConfigureIssueAttachment(
        ModelBuilder modelBuilder,
        bool enforceAccountForeignKeys)
    {
        var attachment = modelBuilder.Entity<IssueAttachment>();
        attachment.ToTable("issue_attachments");
        attachment.HasKey(x => x.Id).HasName("pk_issue_attachments");
        attachment.Property(x => x.Id).HasColumnName("id");
        attachment.Property(x => x.IssueId).HasColumnName("issue_id");
        attachment.Property(x => x.UploadedByAccountId).HasColumnName("uploaded_by_account_id");
        attachment.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        attachment.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(20);
        attachment.Property(x => x.StorageKey).HasColumnName("storage_key");
        attachment.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200);
        attachment.Property(x => x.FileSize).HasColumnName("file_size");
        attachment.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64);
        attachment.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        attachment.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        attachment.Property(x => x.CreatedAt).HasColumnName("created_at");
        attachment.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        attachment.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        attachment.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        attachment.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        attachment.HasIndex(x => x.IssueId).HasDatabaseName("idx_issue_attachments_issue_id");
        attachment.HasIndex(x => x.UploadedByAccountId)
            .HasDatabaseName("idx_issue_attachments_uploaded_by_account_id");
        attachment.HasIndex(x => new { x.IssueId, x.IsDeleted })
            .HasDatabaseName("idx_issue_attachments_issue_deleted");
        attachment.HasOne(x => x.Issue)
            .WithMany()
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_issue_attachments_issue");
        if (enforceAccountForeignKeys)
        {
            attachment.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.UploadedByAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_issue_attachments_uploaded_by_account");
        }
    }

    private static object SeedIssueLookup(
        Guid id,
        string code,
        string name,
        string description,
        int sortOrder)
    {
        var seededAt = new DateTimeOffset(2026, 7, 22, 0, 0, 0, TimeSpan.Zero);
        return new
        {
            Id = id,
            Code = code,
            Name = name,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = seededAt,
            CreatedByAccountId = (Guid?)null,
            UpdatedAt = seededAt,
            UpdatedByAccountId = (Guid?)null,
            Version = 1,
        };
    }

    private static void ConfigureProjectAuditEvent(
        ModelBuilder modelBuilder,
        bool enforceAccountForeignKeys)
    {
        var auditEvent = modelBuilder.Entity<ProjectAuditEvent>();
        auditEvent.ToTable("project_audit_events");
        auditEvent.HasKey(x => x.Id).HasName("pk_project_audit_events");
        auditEvent.Property(x => x.Id).HasColumnName("id");
        auditEvent.Property(x => x.ActorId).HasColumnName("actor_id");
        auditEvent.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(50);
        auditEvent.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100);
        auditEvent.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        auditEvent.Property(x => x.TargetId).HasColumnName("target_id");
        auditEvent.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(50);
        auditEvent.HasIndex(x => x.OccurredAt)
            .HasDatabaseName("idx_project_audit_events_occurred_at");
        if (enforceAccountForeignKeys)
        {
            auditEvent.HasOne<AccountReference>()
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_project_audit_events_actor");
        }
    }
}
