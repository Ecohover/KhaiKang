using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<SystemRole> SystemRoles => Set<SystemRole>();

    public DbSet<AccountSystemRole> AccountSystemRoles => Set<AccountSystemRole>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<SystemRolePermission> SystemRolePermissions => Set<SystemRolePermission>();

    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAccount(modelBuilder);
        ConfigureSystemRole(modelBuilder);
        ConfigureAccountSystemRole(modelBuilder);
        ConfigurePermission(modelBuilder);
        ConfigureSystemRolePermission(modelBuilder);
        ConfigureLoginSession(modelBuilder);
        ConfigureAuditEvent(modelBuilder);
    }

    private static void ConfigurePermission(ModelBuilder modelBuilder)
    {
        var permission = modelBuilder.Entity<Permission>();
        permission.ToTable("permissions");
        permission.HasKey(x => x.Id).HasName("pk_permissions");
        permission.Property(x => x.Id).HasColumnName("id");
        permission.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
        permission.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_permissions_code");
        permission.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        permission.Property(x => x.Description).HasColumnName("description");
        permission.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(20);
        permission.HasIndex(x => x.ScopeType).HasDatabaseName("idx_permissions_scope_type");
        permission.Property(x => x.CreatedAt).HasColumnName("created_at");
        permission.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        permission.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        permission.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        permission.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();

        var seededAt = new DateTimeOffset(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);
        permission.HasData(PermissionCatalog.All.Select(definition => new
        {
            Id = Guid.Parse(definition.Id),
            definition.Code,
            definition.Name,
            definition.Description,
            definition.ScopeType,
            CreatedAt = seededAt,
            CreatedByAccountId = (Guid?)null,
            UpdatedAt = seededAt,
            UpdatedByAccountId = (Guid?)null,
            Version = 1,
        }));
    }

    private static void ConfigureSystemRolePermission(ModelBuilder modelBuilder)
    {
        var mapping = modelBuilder.Entity<SystemRolePermission>();
        mapping.ToTable("system_role_permissions");
        mapping.HasKey(x => x.Id).HasName("pk_system_role_permissions");
        mapping.Property(x => x.Id).HasColumnName("id");
        mapping.Property(x => x.SystemRoleId).HasColumnName("system_role_id");
        mapping.Property(x => x.PermissionId).HasColumnName("permission_id");
        mapping.Property(x => x.CreatedAt).HasColumnName("created_at");
        mapping.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        mapping.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        mapping.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        mapping.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        mapping.HasIndex(x => x.SystemRoleId)
            .HasDatabaseName("idx_system_role_permissions_system_role_id");
        mapping.HasIndex(x => x.PermissionId)
            .HasDatabaseName("idx_system_role_permissions_permission_id");
        mapping.HasIndex(x => new { x.SystemRoleId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("uq_system_role_permissions_role_permission");
        mapping.HasOne(x => x.SystemRole)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.SystemRoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_system_role_permissions_role");
        mapping.HasOne(x => x.Permission)
            .WithMany(x => x.SystemRoles)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_system_role_permissions_permission");
    }

    private static void ConfigureAccount(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Account>();
        account.ToTable("accounts");
        account.HasKey(x => x.Id).HasName("pk_accounts");
        account.Property(x => x.Id).HasColumnName("id");
        account.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
        account.Property(x => x.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(100);
        account.HasIndex(x => x.NormalizedUsername).IsUnique().HasDatabaseName("ux_accounts_normalized_username");
        account.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
        account.Property(x => x.AccountType)
            .HasColumnName("account_type")
            .HasMaxLength(50)
            .HasConversion(
                value => value == AccountType.Human ? "human" : "ai_agent",
                value => value == "human" ? AccountType.Human : AccountType.AiAgent);
        account.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<AccountStatus>(value, true));
        account.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        account.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        account.Property(x => x.CreatedAt).HasColumnName("created_at");
        account.Property(x => x.CreatedByAccountId).HasColumnName("created_by_account_id");
        account.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        account.Property(x => x.UpdatedByAccountId).HasColumnName("updated_by_account_id");
        account.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }

    private static void ConfigureSystemRole(ModelBuilder modelBuilder)
    {
        var role = modelBuilder.Entity<SystemRole>();
        role.ToTable("system_roles");
        role.HasKey(x => x.Id).HasName("pk_system_roles");
        role.Property(x => x.Id).HasColumnName("id");
        role.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        role.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(100);
        role.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_system_roles_normalized_name");
        role.HasData(new
        {
            Id = Guid.Parse(IdentityConstants.UserRoleId),
            Name = IdentityConstants.UserRole,
            NormalizedName = IdentityConstants.UserRole.ToUpperInvariant(),
        });
    }

    private static void ConfigureAccountSystemRole(ModelBuilder modelBuilder)
    {
        var mapping = modelBuilder.Entity<AccountSystemRole>();
        mapping.ToTable("account_system_role_mappings");
        mapping.HasKey(x => new { x.AccountId, x.SystemRoleId })
            .HasName("pk_account_system_role_mappings");
        mapping.Property(x => x.AccountId).HasColumnName("account_id");
        mapping.Property(x => x.SystemRoleId).HasColumnName("system_role_id");
        mapping.HasOne(x => x.Account)
            .WithMany(x => x.SystemRoles)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_account_system_roles_account");
        mapping.HasOne(x => x.SystemRole)
            .WithMany(x => x.Accounts)
            .HasForeignKey(x => x.SystemRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_account_system_roles_role");
    }

    private static void ConfigureLoginSession(ModelBuilder modelBuilder)
    {
        var session = modelBuilder.Entity<LoginSession>();
        session.ToTable("login_sessions");
        session.HasKey(x => x.Id).HasName("pk_login_sessions");
        session.Property(x => x.Id).HasColumnName("id");
        session.Property(x => x.AccountId).HasColumnName("account_id");
        session.Property(x => x.CreatedAt).HasColumnName("created_at");
        session.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        session.Property(x => x.IsPersistent).HasColumnName("is_persistent");
        session.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        session.HasIndex(x => new { x.AccountId, x.ExpiresAt })
            .HasDatabaseName("ix_login_sessions_account_expiry");
        session.HasOne(x => x.Account)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_login_sessions_account");
    }

    private static void ConfigureAuditEvent(ModelBuilder modelBuilder)
    {
        var auditEvent = modelBuilder.Entity<AuditEvent>();
        auditEvent.ToTable("audit_events");
        auditEvent.HasKey(x => x.Id).HasName("pk_audit_events");
        auditEvent.Property(x => x.Id).HasColumnName("id");
        auditEvent.Property(x => x.ActorId).HasColumnName("actor_id");
        auditEvent.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(50);
        auditEvent.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100);
        auditEvent.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        auditEvent.Property(x => x.TargetId).HasColumnName("target_id");
        auditEvent.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(50);
        auditEvent.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_events_occurred_at");
    }
}
