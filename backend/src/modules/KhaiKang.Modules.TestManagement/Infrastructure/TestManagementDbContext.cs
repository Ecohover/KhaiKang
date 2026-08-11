using KhaiKang.Modules.TestManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace KhaiKang.Modules.TestManagement.Infrastructure;

public sealed class TestManagementDbContext(DbContextOptions<TestManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<TestWorkspace> Workspaces => Set<TestWorkspace>();
    public DbSet<TestWorkspaceMember> Members => Set<TestWorkspaceMember>();
    public DbSet<TestWorkspaceProject> WorkspaceProjects => Set<TestWorkspaceProject>();
    public DbSet<TestSuite> Suites => Set<TestSuite>();
    public DbSet<TestCase> Cases => Set<TestCase>();
    public DbSet<TestStep> CaseSteps => Set<TestStep>();
    public DbSet<TestTag> Tags => Set<TestTag>();
    public DbSet<TestCaseTag> CaseTags => Set<TestCaseTag>();
    public DbSet<TestPlan> Plans => Set<TestPlan>();
    public DbSet<TestPlanItem> PlanItems => Set<TestPlanItem>();
    public DbSet<TestRun> Runs => Set<TestRun>();
    public DbSet<TestRunItem> RunItems => Set<TestRunItem>();
    public DbSet<TestRunItemStepResult> RunItemStepResults => Set<TestRunItemStepResult>();
    public DbSet<TestCaseAttachment> CaseAttachments => Set<TestCaseAttachment>();
    public DbSet<TestRunItemAttachment> RunItemAttachments => Set<TestRunItemAttachment>();
    public DbSet<TestCaseRequirementLink> CaseRequirementLinks => Set<TestCaseRequirementLink>();
    public DbSet<TestRunBugLink> RunBugLinks => Set<TestRunBugLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<AccountReference>();
        account.ToTable("accounts", table => table.ExcludeFromMigrations());
        account.HasKey(x => x.Id);
        account.Property(x => x.Id).HasColumnName("id");

        var projectReference = modelBuilder.Entity<ProjectReference>();
        projectReference.ToTable("projects", table => table.ExcludeFromMigrations());
        projectReference.HasKey(x => x.Id);
        projectReference.Property(x => x.Id).HasColumnName("id");

        var issueReference = modelBuilder.Entity<IssueReference>();
        issueReference.ToTable("issues", table => table.ExcludeFromMigrations());
        issueReference.HasKey(x => x.Id);
        issueReference.Property(x => x.Id).HasColumnName("id");

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

        var workspaceProject = modelBuilder.Entity<TestWorkspaceProject>();
        workspaceProject.ToTable("test_workspace_projects");
        workspaceProject.HasKey(x => x.Id).HasName("pk_test_workspace_projects");
        workspaceProject.Property(x => x.Id).HasColumnName("id");
        workspaceProject.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        workspaceProject.Property(x => x.ProjectId).HasColumnName("project_id");
        Audit(workspaceProject);
        workspaceProject.HasIndex(x => x.ProjectId)
            .HasDatabaseName("idx_test_workspace_projects_project_id");
        workspaceProject.HasIndex(x => new { x.TestWorkspaceId, x.ProjectId })
            .IsUnique()
            .HasDatabaseName("uq_test_workspace_projects_workspace_project");
        workspaceProject.HasOne(x => x.Workspace).WithMany(x => x.Projects)
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_workspace_projects_workspace");
        workspaceProject.HasOne<ProjectReference>().WithMany()
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_workspace_projects_project");

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
        testCase.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        testCase.Property(x => x.TestSuiteId).HasColumnName("test_suite_id");
        testCase.Property(x => x.CaseNo).HasColumnName("case_no");
        testCase.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        testCase.Property(x => x.Description).HasColumnName("description");
        testCase.Property(x => x.Preconditions).HasColumnName("preconditions");
        testCase.Property(x => x.OverallExpectedResult).HasColumnName("overall_expected_result");
        testCase.Property(x => x.SortOrder).HasColumnName("sort_order");
        testCase.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(testCase);
        testCase.HasIndex(x => x.TestSuiteId).HasDatabaseName("idx_test_cases_test_suite_id");
        testCase.HasIndex(x => new { x.TestWorkspaceId, x.CaseNo }).IsUnique()
            .HasDatabaseName("uq_test_cases_workspace_case_no");
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

        var tag = modelBuilder.Entity<TestTag>();
        tag.ToTable("test_tags");
        tag.HasKey(x => x.Id).HasName("pk_test_tags");
        tag.Property(x => x.Id).HasColumnName("id");
        tag.Property(x => x.Name).HasColumnName("name").HasMaxLength(50);
        tag.Property(x => x.Description).HasColumnName("description");
        tag.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(tag);
        tag.HasIndex(x => x.Status).HasDatabaseName("idx_test_tags_status");
        tag.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_test_tags_name");

        var caseTag = modelBuilder.Entity<TestCaseTag>();
        caseTag.ToTable("test_case_tags");
        caseTag.HasKey(x => x.Id).HasName("pk_test_case_tags");
        caseTag.Property(x => x.Id).HasColumnName("id");
        caseTag.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        caseTag.Property(x => x.TestTagId).HasColumnName("test_tag_id");
        Audit(caseTag);
        caseTag.HasIndex(x => x.TestCaseId).HasDatabaseName("idx_test_case_tags_test_case_id");
        caseTag.HasIndex(x => x.TestTagId).HasDatabaseName("idx_test_case_tags_test_tag_id");
        caseTag.HasIndex(x => new { x.TestCaseId, x.TestTagId }).IsUnique()
            .HasDatabaseName("uq_test_case_tags_case_tag");
        caseTag.HasOne(x => x.TestCase).WithMany(x => x.Tags).HasForeignKey(x => x.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_test_case_tags_case");
        caseTag.HasOne(x => x.Tag).WithMany(x => x.Cases).HasForeignKey(x => x.TestTagId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_test_case_tags_tag");

        var plan = modelBuilder.Entity<TestPlan>();
        plan.ToTable("test_plans", table => table.HasCheckConstraint(
            "ck_test_plans_test_issue_pair",
            "(test_issue_project_id IS NULL AND test_issue_id IS NULL) OR " +
            "(test_issue_project_id IS NOT NULL AND test_issue_id IS NOT NULL)"));
        plan.HasKey(x => x.Id).HasName("pk_test_plans");
        plan.Property(x => x.Id).HasColumnName("id");
        plan.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        plan.Property(x => x.PlanNo).HasColumnName("plan_no");
        plan.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        plan.Property(x => x.Description).HasColumnName("description");
        plan.Property(x => x.TestIssueProjectId).HasColumnName("test_issue_project_id");
        plan.Property(x => x.TestIssueId).HasColumnName("test_issue_id");
        plan.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        Audit(plan);
        plan.HasIndex(x => new { x.TestWorkspaceId, x.Status })
            .HasDatabaseName("idx_test_plans_workspace_status");
        plan.HasIndex(x => new { x.TestWorkspaceId, x.PlanNo })
            .IsUnique().HasDatabaseName("uq_test_plans_workspace_plan_no");
        plan.HasOne(x => x.Workspace).WithMany()
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_plans_workspace");
        plan.HasOne<ProjectReference>().WithMany()
            .HasForeignKey(x => x.TestIssueProjectId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_plans_test_issue_project");
        plan.HasOne<IssueReference>().WithMany()
            .HasForeignKey(x => x.TestIssueId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_plans_test_issue");

        var planItem = modelBuilder.Entity<TestPlanItem>();
        planItem.ToTable("test_plan_items");
        planItem.HasKey(x => x.Id).HasName("pk_test_plan_items");
        planItem.Property(x => x.Id).HasColumnName("id");
        planItem.Property(x => x.TestPlanId).HasColumnName("test_plan_id");
        planItem.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        planItem.Property(x => x.SortOrder).HasColumnName("sort_order");
        Audit(planItem);
        planItem.HasIndex(x => new { x.TestPlanId, x.TestCaseId })
            .IsUnique().HasDatabaseName("uq_test_plan_items_plan_case");
        planItem.HasIndex(x => new { x.TestPlanId, x.SortOrder })
            .HasDatabaseName("idx_test_plan_items_plan_sort_order");
        planItem.HasOne<TestPlan>().WithMany(x => x.Items)
            .HasForeignKey(x => x.TestPlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_plan_items_plan");
        planItem.HasOne(x => x.TestCase).WithMany()
            .HasForeignKey(x => x.TestCaseId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_plan_items_case");

        var run = modelBuilder.Entity<TestRun>();
        run.ToTable("test_runs", table => table.HasCheckConstraint(
            "ck_test_runs_test_issue_pair",
            "(test_issue_project_id IS NULL AND test_issue_id IS NULL) OR " +
            "(test_issue_project_id IS NOT NULL AND test_issue_id IS NOT NULL)"));
        run.HasKey(x => x.Id).HasName("pk_test_runs");
        run.Property(x => x.Id).HasColumnName("id");
        run.Property(x => x.TestPlanId).HasColumnName("test_plan_id");
        run.Property(x => x.RunNo).HasColumnName("run_no");
        run.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        run.Property(x => x.TestIssueProjectId).HasColumnName("test_issue_project_id");
        run.Property(x => x.TestIssueId).HasColumnName("test_issue_id");
        run.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        run.Property(x => x.StartedByAccountId).HasColumnName("started_by_account_id");
        run.Property(x => x.StartedAt).HasColumnName("started_at");
        run.Property(x => x.CompletedAt).HasColumnName("completed_at");
        run.Property(x => x.Summary).HasColumnName("summary");
        Audit(run);
        run.HasIndex(x => new { x.TestPlanId, x.Status })
            .HasDatabaseName("idx_test_runs_plan_status");
        run.HasIndex(x => new { x.TestPlanId, x.RunNo })
            .IsUnique().HasDatabaseName("uq_test_runs_plan_run_no");
        run.HasOne(x => x.Plan).WithMany()
            .HasForeignKey(x => x.TestPlanId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_runs_plan");
        run.HasOne<AccountReference>().WithMany()
            .HasForeignKey(x => x.StartedByAccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_runs_started_by_account");
        run.HasOne<ProjectReference>().WithMany()
            .HasForeignKey(x => x.TestIssueProjectId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_runs_test_issue_project");
        run.HasOne<IssueReference>().WithMany()
            .HasForeignKey(x => x.TestIssueId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_runs_test_issue");

        var runItem = modelBuilder.Entity<TestRunItem>();
        runItem.ToTable("test_run_items");
        runItem.HasKey(x => x.Id).HasName("pk_test_run_items");
        runItem.Property(x => x.Id).HasColumnName("id");
        runItem.Property(x => x.TestRunId).HasColumnName("test_run_id");
        runItem.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        runItem.Property(x => x.SortOrder).HasColumnName("sort_order");
        runItem.Property(x => x.CaseTitle).HasColumnName("case_title").HasMaxLength(200);
        runItem.Property(x => x.CaseDescription).HasColumnName("case_description");
        runItem.Property(x => x.Preconditions).HasColumnName("preconditions");
        runItem.Property(x => x.OverallExpectedResult).HasColumnName("overall_expected_result");
        runItem.Property(x => x.ResultStatus).HasColumnName("result_status").HasMaxLength(20);
        runItem.Property(x => x.ActualResult).HasColumnName("actual_result");
        runItem.Property(x => x.ExecutedByAccountId).HasColumnName("executed_by_account_id");
        runItem.Property(x => x.ExecutedAt).HasColumnName("executed_at");
        Audit(runItem);
        runItem.HasIndex(x => new { x.TestRunId, x.SortOrder })
            .HasDatabaseName("idx_test_run_items_run_sort_order");
        runItem.HasOne<TestRun>().WithMany(x => x.Items)
            .HasForeignKey(x => x.TestRunId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_run_items_run");
        runItem.HasOne<TestCase>().WithMany()
            .HasForeignKey(x => x.TestCaseId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_run_items_case");
        runItem.HasOne<AccountReference>().WithMany()
            .HasForeignKey(x => x.ExecutedByAccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_run_items_executed_by_account");

        var runStep = modelBuilder.Entity<TestRunItemStepResult>();
        runStep.ToTable("test_run_item_step_results");
        runStep.HasKey(x => x.Id).HasName("pk_test_run_item_step_results");
        runStep.Property(x => x.Id).HasColumnName("id");
        runStep.Property(x => x.TestRunItemId).HasColumnName("test_run_item_id");
        runStep.Property(x => x.StepNo).HasColumnName("step_no");
        runStep.Property(x => x.Action).HasColumnName("action");
        runStep.Property(x => x.ExpectedResult).HasColumnName("expected_result");
        runStep.Property(x => x.ResultStatus).HasColumnName("result_status").HasMaxLength(20);
        runStep.Property(x => x.ActualResult).HasColumnName("actual_result");
        runStep.Property(x => x.ExecutedByAccountId).HasColumnName("executed_by_account_id");
        runStep.Property(x => x.ExecutedAt).HasColumnName("executed_at");
        Audit(runStep);
        runStep.HasIndex(x => new { x.TestRunItemId, x.StepNo })
            .IsUnique().HasDatabaseName("uq_test_run_item_steps_item_step_no");
        runStep.HasOne<TestRunItem>().WithMany(x => x.Steps)
            .HasForeignKey(x => x.TestRunItemId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_run_item_steps_run_item");
        runStep.HasOne<AccountReference>().WithMany()
            .HasForeignKey(x => x.ExecutedByAccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_run_item_steps_executed_by_account");

        var caseAttachment = modelBuilder.Entity<TestCaseAttachment>();
        caseAttachment.ToTable("test_case_attachments");
        caseAttachment.HasKey(x => x.Id).HasName("pk_test_case_attachments");
        caseAttachment.Property(x => x.Id).HasColumnName("id");
        caseAttachment.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        caseAttachment.Property(x => x.UploadedByAccountId).HasColumnName("uploaded_by_account_id");
        caseAttachment.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        caseAttachment.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(20);
        caseAttachment.Property(x => x.StorageKey).HasColumnName("storage_key");
        caseAttachment.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200);
        caseAttachment.Property(x => x.FileSize).HasColumnName("file_size");
        caseAttachment.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64);
        caseAttachment.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        caseAttachment.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        Audit(caseAttachment);
        caseAttachment.HasIndex(x => new { x.TestCaseId, x.IsDeleted }).HasDatabaseName("idx_test_case_attachments_case_deleted");
        caseAttachment.HasOne(x => x.TestCase).WithMany().HasForeignKey(x => x.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_test_case_attachments_case");
        caseAttachment.HasOne<AccountReference>().WithMany().HasForeignKey(x => x.UploadedByAccountId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_test_case_attachments_uploaded_by_account");

        var runAttachment = modelBuilder.Entity<TestRunItemAttachment>();
        runAttachment.ToTable("test_run_item_attachments");
        runAttachment.HasKey(x => x.Id).HasName("pk_test_run_item_attachments");
        runAttachment.Property(x => x.Id).HasColumnName("id");
        runAttachment.Property(x => x.TestRunItemId).HasColumnName("test_run_item_id");
        runAttachment.Property(x => x.UploadedByAccountId).HasColumnName("uploaded_by_account_id");
        runAttachment.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        runAttachment.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(20);
        runAttachment.Property(x => x.StorageKey).HasColumnName("storage_key");
        runAttachment.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200);
        runAttachment.Property(x => x.FileSize).HasColumnName("file_size");
        runAttachment.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64);
        runAttachment.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        runAttachment.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        Audit(runAttachment);
        runAttachment.HasIndex(x => new { x.TestRunItemId, x.IsDeleted }).HasDatabaseName("idx_test_run_item_attachments_item_deleted");
        runAttachment.HasOne(x => x.TestRunItem).WithMany().HasForeignKey(x => x.TestRunItemId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_test_run_item_attachments_item");
        runAttachment.HasOne<AccountReference>().WithMany().HasForeignKey(x => x.UploadedByAccountId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_test_run_item_attachments_uploaded_by_account");

        var requirementLink = modelBuilder.Entity<TestCaseRequirementLink>();
        requirementLink.ToTable("test_case_requirement_links", table => table.HasCheckConstraint(
            "ck_test_case_requirement_links_deleted_metadata",
            "(is_deleted = false AND deleted_at IS NULL AND deleted_by_account_id IS NULL) OR " +
            "(is_deleted = true AND deleted_at IS NOT NULL AND deleted_by_account_id IS NOT NULL)"));
        requirementLink.HasKey(x => x.Id).HasName("pk_test_case_requirement_links");
        requirementLink.Property(x => x.Id).HasColumnName("id");
        requirementLink.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        requirementLink.Property(x => x.TestCaseId).HasColumnName("test_case_id");
        requirementLink.Property(x => x.ProjectId).HasColumnName("project_id");
        requirementLink.Property(x => x.RequirementIssueId).HasColumnName("requirement_issue_id");
        requirementLink.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        requirementLink.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        requirementLink.Property(x => x.DeletedByAccountId).HasColumnName("deleted_by_account_id");
        Audit(requirementLink);
        requirementLink.HasIndex(x => new { x.RequirementIssueId, x.IsDeleted })
            .HasDatabaseName("idx_test_case_requirement_links_issue_active");
        requirementLink.HasIndex(x => new { x.TestWorkspaceId, x.ProjectId, x.IsDeleted })
            .HasDatabaseName("idx_test_case_requirement_links_workspace_project_active");
        requirementLink.HasIndex(x => new { x.TestCaseId, x.RequirementIssueId })
            .IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_test_case_requirement_links_active");
        requirementLink.HasOne(x => x.Workspace).WithMany()
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_case_requirement_links_workspace");
        requirementLink.HasOne(x => x.TestCase).WithMany()
            .HasForeignKey(x => x.TestCaseId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_case_requirement_links_case");
        requirementLink.HasOne<ProjectReference>().WithMany()
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_case_requirement_links_project");
        requirementLink.HasOne<IssueReference>().WithMany()
            .HasForeignKey(x => x.RequirementIssueId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_case_requirement_links_issue");
        requirementLink.HasOne<AccountReference>().WithMany()
            .HasForeignKey(x => x.DeletedByAccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_case_requirement_links_deleted_by_account");

        var runBugLink = modelBuilder.Entity<TestRunBugLink>();
        runBugLink.ToTable("test_run_bug_links");
        runBugLink.HasKey(x => x.Id).HasName("pk_test_run_bug_links");
        runBugLink.Property(x => x.Id).HasColumnName("id");
        runBugLink.Property(x => x.TestWorkspaceId).HasColumnName("test_workspace_id");
        runBugLink.Property(x => x.TestRunId).HasColumnName("test_run_id");
        runBugLink.Property(x => x.ProjectId).HasColumnName("project_id");
        runBugLink.Property(x => x.BugIssueId).HasColumnName("bug_issue_id");
        Audit(runBugLink);
        runBugLink.HasIndex(x => x.TestRunId)
            .HasDatabaseName("idx_test_run_bug_links_run");
        runBugLink.HasIndex(x => new { x.TestWorkspaceId, x.ProjectId })
            .HasDatabaseName("idx_test_run_bug_links_workspace_project");
        runBugLink.HasIndex(x => x.BugIssueId).IsUnique()
            .HasDatabaseName("uq_test_run_bug_links_bug_issue");
        runBugLink.HasOne(x => x.Workspace).WithMany()
            .HasForeignKey(x => x.TestWorkspaceId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_run_bug_links_workspace");
        runBugLink.HasOne(x => x.TestRun).WithMany()
            .HasForeignKey(x => x.TestRunId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_test_run_bug_links_run");
        runBugLink.HasOne<ProjectReference>().WithMany()
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_run_bug_links_project");
        runBugLink.HasOne<IssueReference>().WithMany()
            .HasForeignKey(x => x.BugIssueId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_test_run_bug_links_issue");
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
