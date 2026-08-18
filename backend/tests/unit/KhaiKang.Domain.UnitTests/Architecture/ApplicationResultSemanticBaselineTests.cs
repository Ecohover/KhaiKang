using System.Text.RegularExpressions;

namespace KhaiKang.Domain.UnitTests.Architecture;

public sealed partial class ApplicationResultSemanticBaselineTests
{
    private static readonly string[] ApprovedBroadResultOperations =
    [
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/IssueAttachments/IssueAttachmentService.cs::DeleteAsync -> IssueAttachmentMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/IssueAttachments/IssueAttachmentService.cs::OpenContentAsync -> IssueAttachmentContentResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/IssueAttachments/IssueAttachmentService.cs::UploadAsync -> IssueAttachmentMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/IssueRelations/IssueRelationService.cs::CreateAsync -> IssueRelationMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/IssueRelations/IssueRelationService.cs::DeleteAsync -> IssueRelationMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/Issues/IssueService.cs::ChangeAssigneeAsync -> IssueMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/Issues/IssueService.cs::ChangeStatusAsync -> IssueMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/Issues/IssueService.cs::CreateAsync -> IssueMutationResult",
        "backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/Issues/IssueService.cs::UpdateAsync -> IssueMutationResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseAttachments/TestCaseAttachmentService.cs::DeleteAsync -> TestCaseAttachmentMutationResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseAttachments/TestCaseAttachmentService.cs::OpenContentAsync -> TestCaseAttachmentContentResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseAttachments/TestCaseAttachmentService.cs::UploadAsync -> TestCaseAttachmentMutationResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseRequirementLinks/TestCaseRequirementLinkService.cs::CreateAsync -> TestManagementResult<TestCaseRequirementLinkResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseRequirementLinks/TestCaseRequirementLinkService.cs::DeleteAsync -> TestManagementResult<object>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCaseRequirementLinks/TestCaseRequirementLinkService.cs::ListAsync -> TestManagementResult<IReadOnlyList<TestCaseRequirementLinkResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::AddMemberAsync -> TestManagementResult<TestWorkspaceMemberResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreateCaseAsync -> TestManagementResult<TestCaseResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreatePlanAsync -> TestManagementResult<TestPlanResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreateRunAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreateSuiteAsync -> TestManagementResult<TestSuiteResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreateTagAsync -> TestManagementResult<TestTagResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::CreateWorkspaceAsync -> TestManagementResult<TestWorkspaceResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::GetCaseAsync -> TestManagementResult<TestCaseResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::GetPlanAsync -> TestManagementResult<TestPlanResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::GetRunAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::LinkWorkspaceProjectAsync -> TestManagementResult<TestWorkspaceProjectResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListCasesAsync -> TestManagementResult<IReadOnlyList<TestCaseResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListMembersAsync -> TestManagementResult<IReadOnlyList<TestWorkspaceMemberResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListPlansAsync -> TestManagementResult<IReadOnlyList<TestPlanResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListRunsAsync -> TestManagementResult<IReadOnlyList<TestRunResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListSuitesAsync -> TestManagementResult<IReadOnlyList<TestSuiteResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::ListWorkspaceProjectsAsync -> TestManagementResult<IReadOnlyList<TestWorkspaceProjectResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::RecordRunItemAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::RecordRunStepAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::RemoveMemberAsync -> TestManagementResult<object>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::RerunAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UnlinkWorkspaceProjectAsync -> TestManagementResult<object>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateCaseAsync -> TestManagementResult<TestCaseResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateMemberAsync -> TestManagementResult<TestWorkspaceMemberResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdatePlanAsync -> TestManagementResult<TestPlanResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateRunStatusAsync -> TestManagementResult<TestRunResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateSuiteAsync -> TestManagementResult<TestSuiteResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateTagAsync -> TestManagementResult<TestTagResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestManagementService.cs::UpdateWorkspaceAsync -> TestManagementResult<TestWorkspaceResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestRunBugs/TestRunBugService.cs::CreateAsync -> TestManagementResult<TestRunBugLinkResponse>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestRunBugs/TestRunBugService.cs::ListAsync -> TestManagementResult<IReadOnlyList<TestRunBugLinkResponse>>",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestRunItemAttachments/TestRunItemAttachmentService.cs::DeleteAsync -> TestRunItemAttachmentMutationResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestRunItemAttachments/TestRunItemAttachmentService.cs::OpenContentAsync -> TestRunItemAttachmentContentResult",
        "backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestRunItemAttachments/TestRunItemAttachmentService.cs::UploadAsync -> TestRunItemAttachmentMutationResult",
    ];

    private static readonly HashSet<string> BroadResultTypeNames =
    [
        "IssueAttachmentContentResult",
        "IssueAttachmentMutationResult",
        "IssueMutationResult",
        "IssueRelationMutationResult",
        "TestCaseAttachmentContentResult",
        "TestCaseAttachmentMutationResult",
        "TestRunItemAttachmentContentResult",
        "TestRunItemAttachmentMutationResult",
    ];

    [Fact]
    public void ApplicationOperations_DoNotGrowBroadResultDebt()
    {
        var currentDebt = ApplicationSourceFiles()
            .SelectMany(FindBroadResultOperations)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var unexpectedDebt = currentDebt
            .Except(ApprovedBroadResultOperations, StringComparer.Ordinal)
            .ToArray();
        var resolvedDebt = ApprovedBroadResultOperations
            .Except(currentDebt, StringComparer.Ordinal)
            .ToArray();
        var message = "Unexpected broad Application Result operations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, unexpectedDebt) +
            Environment.NewLine +
            "Resolved operations that must be removed from the baseline:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, resolvedDebt);

        Assert.True(unexpectedDebt.Length == 0 && resolvedDebt.Length == 0, message);
    }

    [Theory]
    [InlineData("TestManagementResult<ItemResponse>", true)]
    [InlineData("TestManagementResult<IReadOnlyList<ItemResponse>>", true)]
    [InlineData("IssueMutationResult", true)]
    [InlineData("CreateAccountResult", false)]
    [InlineData("CreateIssueCommandResult", false)]
    public void BroadResultRule_DistinguishesDebtFromOperationSpecificResults(
        string returnType,
        bool expected)
    {
        Assert.Equal(expected, IsBroadResultType(returnType));
    }

    private static IEnumerable<string> FindBroadResultOperations(string file)
    {
        var relativePath = RelativePath(file);
        return OperationReturnRegex()
            .Matches(File.ReadAllText(file))
            .Where(match => IsBroadResultType(match.Groups["return"].Value))
            .Select(match => $"{relativePath}::{match.Groups["method"].Value} -> " +
                match.Groups["return"].Value);
    }

    private static bool IsBroadResultType(string returnType)
    {
        return returnType.StartsWith("TestManagementResult<", StringComparison.Ordinal) ||
            BroadResultTypeNames.Contains(returnType);
    }

    private static IReadOnlyList<string> ApplicationSourceFiles()
    {
        return Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot(), "backend", "src", "modules"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file => file.Replace('\\', '/').Contains("/Application/", StringComparison.Ordinal))
            .Where(file => !IsBuildArtifact(file))
            .ToArray();
    }

    private static bool IsBuildArtifact(string file)
    {
        var normalizedPath = file.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "KhaiKang.Backend.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the KhaiKang repository root.");
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/');
    }

    [GeneratedRegex(
        "^\\s*public\\s+(?:async\\s+)?Task<(?<return>[^\\r\\n]+)>\\s+(?<method>[A-Za-z_][A-Za-z0-9_]*)\\s*\\(",
        RegexOptions.Multiline)]
    private static partial Regex OperationReturnRegex();
}
