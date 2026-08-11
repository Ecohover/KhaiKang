namespace KhaiKang.Modules.ProjectManagement.Application;

public static class ProjectManagementConstants
{
    public const string SystemAdminRole = "System Admin";
    public const string OwnerRoleCode = "owner";
    public const string ProjectCreatePermission = "project.create";
    public const string ProjectDeactivatePermission = "project.deactivate";
    public const string ProjectReadPermission = "project.read";
    public const string ProjectUpdatePermission = "project.update";
    public const string ProjectMemberAddPermission = "project.member.add";
    public const string ProjectMemberRemovePermission = "project.member.remove";
    public const string ProjectRoleAssignPermission = "project.role.assign";
    public const string IssueCreatePermission = "issue.create";
    public const string IssueReadPermission = "issue.read";
    public const string IssueStatusChangePermission = "issue.status.change";
    public const string IssueUpdatePermission = "issue.update";
    public const string IssueAssigneeChangePermission = "issue.assignee.change";
    public const string IssueRelationCreatePermission = "issue.relation.create";
    public const string IssueAttachmentUploadPermission = "issue.attachment.upload";
    public const string IssueAttachmentDeletePermission = "issue.attachment.delete";
    public const string PermissionClaimType = "permission";

    public static readonly Guid OwnerRoleId = Guid.Parse("4f5961ac-5a4b-49e1-a73c-451d43a39718");
    public static readonly Guid ManagerRoleId = Guid.Parse("836e894f-ca1d-4fd9-af14-b987882400dd");
    public static readonly Guid ContributorRoleId = Guid.Parse("ead22957-af22-47eb-a7de-782145087141");
    public static readonly Guid ReviewerRoleId = Guid.Parse("c5684ccd-30b0-43aa-85ef-7f1c23835492");
}
