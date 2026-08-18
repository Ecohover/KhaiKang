using System.Security.Cryptography;
using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Application;

public static class ProjectPermissionCatalog
{
    public static readonly ProjectPermissionDefinition[] All =
    [
        new ProjectPermissionDefinition(id: "1e42b09d-9839-4e8c-951c-38c941f9e4ca", code: "project.read"),
        new ProjectPermissionDefinition(id: "f622c88d-d0f0-40ed-86c0-bba2c3ff44c9", code: "project.update"),
        new ProjectPermissionDefinition(id: "23cffaf6-9d01-4116-9a79-a4970bc01eae", code: "project.member.add"),
        new ProjectPermissionDefinition(id: "bb8afb36-8753-4383-bce7-83065a92c0d3", code: "project.member.remove"),
        new ProjectPermissionDefinition(id: "0cc976a0-4f5a-4ce4-9309-6f4476522aa6", code: "project.role.assign"),
        new ProjectPermissionDefinition(id: "3c2bdd52-7445-4a16-9750-1b28d97bf109", code: "issue.create"),
        new ProjectPermissionDefinition(id: "3ed83136-9532-4e6e-8adb-b73ae9863a2c", code: "issue.read"),
        new ProjectPermissionDefinition(id: "a5fa36de-f8eb-491a-981c-f2f17244fa2b", code: "issue.update"),
        new ProjectPermissionDefinition(id: "084b565e-e59c-4b48-9a4d-2de2a58e0a9d", code: "issue.status.change"),
        new ProjectPermissionDefinition(id: "60098112-f880-40d8-97d6-bed5784f83a0", code: "issue.assignee.change"),
        new ProjectPermissionDefinition(id: "2f36b9f4-b7f1-4cd2-af25-46393d560b13", code: "issue.comment.create"),
        new ProjectPermissionDefinition(id: "081eced1-97b4-4f3e-bb05-56cc3053de6f", code: "issue.relation.create"),
        new ProjectPermissionDefinition(id: "bce4a59e-47d6-4664-bda4-c1ea66b50ec1", code: "issue.attachment.upload"),
        new ProjectPermissionDefinition(id: "811e7203-b5ab-4b2c-83aa-8e071f68b36f", code: "issue.attachment.delete"),
    ];

    public static readonly ProjectRolePermissionSeed[] Mappings = BuildMappings();

    private static ProjectRolePermissionSeed[] BuildMappings()
    {
        var owner = ProjectManagementConstants.OwnerRoleId;
        var manager = ProjectManagementConstants.ManagerRoleId;
        var contributor = ProjectManagementConstants.ContributorRoleId;
        var reviewer = ProjectManagementConstants.ReviewerRoleId;
        var mapping = new List<ProjectRolePermissionSeed>();

        AddAll(mapping, owner, All.Select(permission => permission.Id));
        AddAll(mapping, manager, All.Select(permission => permission.Id));
        AddAll(mapping, contributor, Codes(
            "project.read",
            "issue.create",
            "issue.read",
            "issue.update",
            "issue.status.change",
            "issue.assignee.change",
            "issue.comment.create",
            "issue.relation.create",
            "issue.attachment.upload"));
        AddAll(mapping, reviewer, Codes(
            "project.read",
            "issue.read",
            "issue.status.change",
            "issue.assignee.change",
            "issue.comment.create",
            "issue.attachment.upload"));

        return mapping.ToArray();
    }

    private static IEnumerable<Guid> Codes(params string[] codes)
    {
        return All.Where(permission => codes.Contains(permission.Code))
            .Select(permission => permission.Id);
    }

    private static void AddAll(
        ICollection<ProjectRolePermissionSeed> mappings,
        Guid roleId,
        IEnumerable<Guid> permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            mappings.Add(new ProjectRolePermissionSeed
            {
                Id = MappingId(roleId, permissionId),
                ProjectRoleId = roleId,
                PermissionId = permissionId,
            });
        }
    }

    private static Guid MappingId(Guid roleId, Guid permissionId)
    {
        Span<byte> input = stackalloc byte[32];
        roleId.TryWriteBytes(input[..16]);
        permissionId.TryWriteBytes(input[16..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        return new Guid(hash[..16]);
    }
}
