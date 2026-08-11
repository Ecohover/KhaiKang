namespace KhaiKang.Modules.ProjectManagement.Domain;

public static class ProjectManagementCodes
{
    public static string ToCode(this ProjectStatus status) => status switch
    {
        ProjectStatus.Active => "active",
        ProjectStatus.Inactive => "inactive",
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this ProjectMemberStatus status) => status switch
    {
        ProjectMemberStatus.Active => "active",
        ProjectMemberStatus.Removed => "removed",
        _ => throw UnknownValue(status),
    };

    public static ProjectStatus ParseProjectStatus(string code)
    {
        if (CodeEquals(code, "active"))
        {
            return ProjectStatus.Active;
        }

        if (CodeEquals(code, "inactive"))
        {
            return ProjectStatus.Inactive;
        }

        throw UnsupportedDatabaseValue<ProjectStatus>(code);
    }

    public static ProjectMemberStatus ParseProjectMemberStatus(string code)
    {
        if (CodeEquals(code, "active"))
        {
            return ProjectMemberStatus.Active;
        }

        if (CodeEquals(code, "removed"))
        {
            return ProjectMemberStatus.Removed;
        }

        throw UnsupportedDatabaseValue<ProjectMemberStatus>(code);
    }

    private static bool CodeEquals(string actualCode, string expectedCode) =>
        string.Equals(actualCode, expectedCode, StringComparison.OrdinalIgnoreCase);

    private static InvalidOperationException UnsupportedDatabaseValue<T>(string code)
        where T : struct, Enum
        => new($"Unsupported {typeof(T).Name} database value '{code}'.");

    private static ArgumentOutOfRangeException UnknownValue<T>(T value)
        where T : struct, Enum =>
        new(nameof(value), value, $"Unsupported {typeof(T).Name} value.");
}
