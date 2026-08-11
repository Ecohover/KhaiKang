using System.Reflection;
using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests.Architecture;

public sealed class DomainDesignBaselineTests
{
    private const int MaximumPublicParameterCount = 3;

    private static readonly string[] ApprovedLongParameterMembers =
    [
        "KhaiKang.Modules.Identity.Domain.Account::.ctor/5",
        "KhaiKang.Modules.Identity.Domain.Account::Rename/4",
        "KhaiKang.Modules.Identity.Domain.AuditEvent::.ctor/7",
        "KhaiKang.Modules.Identity.Domain.LoginSession::.ctor/5",
        "KhaiKang.Modules.Identity.Domain.SystemRolePermission::.ctor/5",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueAttachment::.ctor/10",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueRelation::.ctor/7",
        "KhaiKang.Modules.ProjectManagement.Domain.ProjectAuditEvent::.ctor/5",
        "KhaiKang.Modules.ProjectManagement.Domain.ProjectRole::.ctor/6",
        "KhaiKang.Modules.TestManagement.Domain.TestCase::.ctor/11",
        "KhaiKang.Modules.TestManagement.Domain.TestCase::Update/9",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseAttachment::.ctor/10",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseRequirementLink::.ctor/7",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseTag::.ctor/5",
        "KhaiKang.Modules.TestManagement.Domain.TestPlan::.ctor/9",
        "KhaiKang.Modules.TestManagement.Domain.TestPlan::Update/7",
        "KhaiKang.Modules.TestManagement.Domain.TestPlanItem::.ctor/6",
        "KhaiKang.Modules.TestManagement.Domain.TestRun::.ctor/8",
        "KhaiKang.Modules.TestManagement.Domain.TestRun::Finish/4",
        "KhaiKang.Modules.TestManagement.Domain.TestRunBugLink::.ctor/7",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItem::.ctor/6",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItem::Record/4",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItemAttachment::.ctor/10",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItemStepResult::.ctor/5",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItemStepResult::Record/4",
        "KhaiKang.Modules.TestManagement.Domain.TestStep::.ctor/7",
        "KhaiKang.Modules.TestManagement.Domain.TestSuite::.ctor/8",
        "KhaiKang.Modules.TestManagement.Domain.TestSuite::Update/7",
        "KhaiKang.Modules.TestManagement.Domain.TestTag::.ctor/5",
        "KhaiKang.Modules.TestManagement.Domain.TestTag::Update/5",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspace::.ctor/6",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspace::Update/5",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspaceMember::.ctor/6",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspaceProject::.ctor/5",
    ];

    private static readonly string[] LegacyAuditPropertyOwners =
    [
        "KhaiKang.Modules.Identity.Domain.Account",
        "KhaiKang.Modules.Identity.Domain.Permission",
        "KhaiKang.Modules.Identity.Domain.SystemRolePermission",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueAttachment",
        "KhaiKang.Modules.ProjectManagement.Domain.IssuePriority",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueRelation",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueRelationType",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueStatus",
        "KhaiKang.Modules.ProjectManagement.Domain.IssueType",
        "KhaiKang.Modules.ProjectManagement.Domain.Project",
        "KhaiKang.Modules.ProjectManagement.Domain.ProjectRole",
        "KhaiKang.Modules.ProjectManagement.Domain.ProjectRolePermission",
        "KhaiKang.Modules.TestManagement.Domain.TestCase",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseAttachment",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseRequirementLink",
        "KhaiKang.Modules.TestManagement.Domain.TestCaseTag",
        "KhaiKang.Modules.TestManagement.Domain.TestPlan",
        "KhaiKang.Modules.TestManagement.Domain.TestPlanItem",
        "KhaiKang.Modules.TestManagement.Domain.TestRun",
        "KhaiKang.Modules.TestManagement.Domain.TestRunBugLink",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItem",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItemAttachment",
        "KhaiKang.Modules.TestManagement.Domain.TestRunItemStepResult",
        "KhaiKang.Modules.TestManagement.Domain.TestStep",
        "KhaiKang.Modules.TestManagement.Domain.TestSuite",
        "KhaiKang.Modules.TestManagement.Domain.TestTag",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspace",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspaceMember",
        "KhaiKang.Modules.TestManagement.Domain.TestWorkspaceProject",
    ];

    [Fact]
    public void PublicDomainMembers_DoNotAddLongParameterLists()
    {
        var currentDebt = DomainTypes()
            .SelectMany(PublicConstructorsAndMethods)
            .Where(member => member.GetParameters().Length > MaximumPublicParameterCount)
            .Select(MemberKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        AssertBaselineMatches(
            ApprovedLongParameterMembers,
            currentDebt,
            "long public Domain parameter list");
    }

    [Fact]
    public void DomainTypes_DoNotAddRepeatedAuditPropertyDeclarations()
    {
        var currentDebt = DomainTypes()
            .Where(type => !type.IsAbstract && DeclaresCompleteAuditMetadata(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        AssertBaselineMatches(
            LegacyAuditPropertyOwners,
            currentDebt,
            "repeated audit property declaration");
    }

    private static IEnumerable<Type> DomainTypes()
    {
        return DomainAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsPublic && type.Namespace?.EndsWith(".Domain", StringComparison.Ordinal) == true);
    }

    private static IEnumerable<Assembly> DomainAssemblies()
    {
        yield return typeof(Account).Assembly;
        yield return typeof(Project).Assembly;
        yield return typeof(TestWorkspace).Assembly;
    }

    private static IEnumerable<MethodBase> PublicConstructorsAndMethods(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.DeclaringType == type && !method.IsSpecialName);

        return constructors.Cast<MethodBase>().Concat(methods);
    }

    private static string MemberKey(MethodBase member)
    {
        return $"{member.DeclaringType!.FullName}::{member.Name}/{member.GetParameters().Length}";
    }

    private static bool DeclaresCompleteAuditMetadata(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        string[] propertyNames =
        [
            "CreatedAt",
            "CreatedByAccountId",
            "UpdatedAt",
            "UpdatedByAccountId",
            "Version",
        ];

        return propertyNames.All(propertyName => type.GetProperty(propertyName, flags) is not null);
    }

    private static void AssertBaselineMatches(
        IReadOnlyCollection<string> approvedDebt,
        IReadOnlyCollection<string> currentDebt,
        string debtDescription)
    {
        var unexpectedDebt = currentDebt.Except(approvedDebt, StringComparer.Ordinal).ToArray();
        var resolvedDebt = approvedDebt.Except(currentDebt, StringComparer.Ordinal).ToArray();

        var message = $"Unexpected {debtDescription}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, unexpectedDebt) +
            $"{Environment.NewLine}Resolved entries that should be removed from the baseline:{Environment.NewLine}" +
            string.Join(Environment.NewLine, resolvedDebt);

        Assert.True(unexpectedDebt.Length == 0 && resolvedDebt.Length == 0, message);
    }
}
