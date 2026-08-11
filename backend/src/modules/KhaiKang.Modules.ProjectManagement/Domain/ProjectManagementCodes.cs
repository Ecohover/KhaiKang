namespace KhaiKang.Modules.ProjectManagement.Domain;

public static class ProjectManagementCodes
{
    private const string ProjectActiveCode = "active";
    private const string ProjectInactiveCode = "inactive";
    private const string ProjectMemberActiveCode = "active";
    private const string ProjectMemberRemovedCode = "removed";
    private const string TodoCode = "todo";
    private const string DoingCode = "doing";
    private const string DoneCode = "done";

    public static string ToCode(this ProjectStatus status) => status switch
    {
        ProjectStatus.Active => ProjectActiveCode,
        ProjectStatus.Inactive => ProjectInactiveCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this ProjectMemberStatus status) => status switch
    {
        ProjectMemberStatus.Active => ProjectMemberActiveCode,
        ProjectMemberStatus.Removed => ProjectMemberRemovedCode,
        _ => throw UnknownValue(status),
    };

    public static string ToCode(this IssueStatusCategory category) => category switch
    {
        IssueStatusCategory.Todo => TodoCode,
        IssueStatusCategory.Doing => DoingCode,
        IssueStatusCategory.Done => DoneCode,
        _ => throw UnknownValue(category),
    };

    public static ProjectStatus ParseProjectStatus(string code)
    {
        if (CodeEquals(code, ProjectActiveCode))
        {
            return ProjectStatus.Active;
        }

        if (CodeEquals(code, ProjectInactiveCode))
        {
            return ProjectStatus.Inactive;
        }

        throw UnsupportedDatabaseValue<ProjectStatus>(code);
    }

    public static bool IsProjectStatusCode(string code)
    {
        return CodeEquals(code, ProjectActiveCode) ||
            CodeEquals(code, ProjectInactiveCode);
    }

    public static ProjectMemberStatus ParseProjectMemberStatus(string code)
    {
        if (CodeEquals(code, ProjectMemberActiveCode))
        {
            return ProjectMemberStatus.Active;
        }

        if (CodeEquals(code, ProjectMemberRemovedCode))
        {
            return ProjectMemberStatus.Removed;
        }

        throw UnsupportedDatabaseValue<ProjectMemberStatus>(code);
    }

    public static IssueStatusCategory ParseIssueStatusCategory(string code)
    {
        if (CodeEquals(code, TodoCode))
        {
            return IssueStatusCategory.Todo;
        }

        if (CodeEquals(code, DoingCode))
        {
            return IssueStatusCategory.Doing;
        }

        if (CodeEquals(code, DoneCode))
        {
            return IssueStatusCategory.Done;
        }

        throw UnsupportedDatabaseValue<IssueStatusCategory>(code);
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
