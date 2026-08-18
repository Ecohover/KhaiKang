using System.Text.RegularExpressions;

namespace KhaiKang.Domain.UnitTests.Architecture;

public sealed partial class PublicTypeLayoutBaselineTests
{
    private static readonly string[] ApprovedMultiplePublicTypeFiles = [];

    private static readonly string[] ApprovedPublicTypeFileNameMismatches = [];

    private static readonly string[] ApprovedPositionalBoundaryContracts = [];

    private static readonly string[] DisallowedTechnicalFolderNames =
    [
        "Enums",
        "Interfaces",
        "Requests",
        "Responses",
        "Results",
    ];

    [Fact]
    public void SourceFiles_DoNotAddMultipleTopLevelPublicTypes()
    {
        var currentDebt = SourceFiles()
            .Select(file => new
            {
                File = file,
                Types = PublicTypeMatches(file),
            })
            .Where(item => item.Types.Count > 1)
            .Select(item => $"{RelativePath(item.File)}=>{string.Join(",", item.Types
                .Select(match => match.Groups["name"].Value)
                .Order(StringComparer.Ordinal))}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        AssertBaselineMatches(
            ApprovedMultiplePublicTypeFiles,
            currentDebt,
            "source file containing multiple top-level public types");
    }

    [Fact]
    public void SinglePublicTypeFiles_DoNotAddFileNameMismatches()
    {
        var currentDebt = SourceFiles()
            .Select(file => new
            {
                File = file,
                Types = PublicTypeMatches(file),
            })
            .Where(item => item.Types.Count == 1)
            .Where(item => !IsEfCoreMigration(item.File))
            .Where(item => Path.GetFileNameWithoutExtension(item.File) != item.Types[0].Groups["name"].Value)
            .Select(item => $"{RelativePath(item.File)}=>{item.Types[0].Groups["name"].Value}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        AssertBaselineMatches(
            ApprovedPublicTypeFileNameMismatches,
            currentDebt,
            "public type whose file name does not match the type name");
    }

    [Fact]
    public void PublicNonDomainRecords_DoNotAddPositionalDeclarations()
    {
        var currentDebt = NonDomainSourceFiles()
            .SelectMany(file => PositionalBoundaryContractRegex().Matches(File.ReadAllText(file))
                .Select(match => $"{RelativePath(file)}=>{match.Groups["name"].Value}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        AssertBaselineMatches(
            ApprovedPositionalBoundaryContracts,
            currentDebt,
            "positional public boundary contract");
    }

    [Fact]
    public void SourceDirectories_DoNotUseTechnicalTypeBuckets()
    {
        var technicalDirectories = SourceDirectories()
            .Where(IsDisallowedTechnicalFolder)
            .Select(RelativePath)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var message = "Technical source folders must be grouped by business resource or use case:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, technicalDirectories);

        Assert.True(technicalDirectories.Length == 0, message);
    }

    [Theory]
    [InlineData("Enums", true)]
    [InlineData("Interfaces", true)]
    [InlineData("Requests", true)]
    [InlineData("Responses", true)]
    [InlineData("Results", true)]
    [InlineData("IssueCommands", false)]
    [InlineData("TestRuns", false)]
    public void TechnicalFolderRule_DistinguishesTypeBucketsFromUseCases(
        string folderName,
        bool expected)
    {
        Assert.Equal(expected, IsDisallowedTechnicalFolder(folderName));
    }

    [Theory]
    [InlineData("public sealed record ExampleRequest(string Value);", "ExampleRequest")]
    [InlineData("public partial record ExampleCommand(string Value);", "ExampleCommand")]
    [InlineData("public readonly record struct ExampleRequest(string Value);", "ExampleRequest")]
    [InlineData("public record class ExampleCommand(string Value);", "ExampleCommand")]
    [InlineData("    public sealed record BatchRequest<T>(T Value);", "BatchRequest")]
    [InlineData("public partial record BatchCommand<TItem, TResult>(TItem Value);", "BatchCommand")]
    [InlineData("public sealed record IssueResponse(Guid Id, string Title);", "IssueResponse")]
    [InlineData("public partial record IssueListQuery(string? Search);", "IssueListQuery")]
    [InlineData("public readonly record struct AccountDirectoryEntry(Guid Id);", "AccountDirectoryEntry")]
    [InlineData("public sealed record OperationResult<T>(T Value);", "OperationResult")]
    [InlineData("public unsafe sealed record NativeResult(nint Value);", "NativeResult")]
    public void PositionalBoundaryContractPattern_RecognizesSupportedRecordDeclarations(
        string declaration,
        string expectedName)
    {
        var positionalMatch = Assert.Single(PositionalBoundaryContractRegex().Matches(declaration));
        var publicTypeMatch = Assert.Single(PublicTypeRegex().Matches(declaration));

        Assert.Equal(expectedName, positionalMatch.Groups["name"].Value);
        Assert.Equal(expectedName, publicTypeMatch.Groups["name"].Value);
    }

    [Theory]
    [InlineData("public sealed record ExampleResponse { public required string Value { get; init; } }")]
    [InlineData("internal sealed record InternalResult(string Value);")]
    [InlineData("public sealed class ExampleResponse(string Value);")]
    [InlineData("public enum ExampleOutcome { Succeeded }")]
    public void PositionalBoundaryContractPattern_IgnoresNonPublicPositionalRecordsAndOtherTypes(
        string declaration)
    {
        Assert.Empty(PositionalBoundaryContractRegex().Matches(declaration));
    }

    private static IReadOnlyList<string> SourceFiles()
    {
        return Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot(), "backend", "src"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file => !IsBuildArtifact(file))
            .ToArray();
    }

    private static IReadOnlyList<string> NonDomainSourceFiles()
    {
        return SourceFiles()
            .Where(file => !IsDomainSourceFile(file))
            .ToArray();
    }

    private static IReadOnlyList<string> SourceDirectories()
    {
        return Directory.EnumerateDirectories(
                Path.Combine(RepositoryRoot(), "backend", "src"),
                "*",
                SearchOption.AllDirectories)
            .Where(directory => !IsBuildArtifact(directory))
            .ToArray();
    }

    private static bool IsDisallowedTechnicalFolder(string directory)
    {
        var folderName = Path.GetFileName(directory);
        return DisallowedTechnicalFolderNames.Contains(
            folderName,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsDomainSourceFile(string file)
    {
        var normalizedPath = file.Replace('\\', '/');
        return normalizedPath.Contains("/Domain/", StringComparison.Ordinal);
    }

    private static bool IsBuildArtifact(string file)
    {
        var normalizedPath = file.Replace('\\', '/');
        return normalizedPath.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEfCoreMigration(string file)
    {
        var normalizedPath = file.Replace('\\', '/');
        return normalizedPath.Contains("/Infrastructure/Migrations/", StringComparison.Ordinal) &&
            char.IsDigit(Path.GetFileName(file)[0]);
    }

    private static MatchCollection PublicTypeMatches(string file)
    {
        return PublicTypeRegex().Matches(File.ReadAllText(file));
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

    [GeneratedRegex(
        "^\\s*public\\s+(?:(?:file|sealed|abstract|static|unsafe|readonly|ref|partial)\\s+)*(?:class|record(?:\\s+(?:class|struct))?|interface|enum|struct)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline)]
    private static partial Regex PublicTypeRegex();

    [GeneratedRegex(
        "^\\s*public\\s+(?:(?:file|sealed|abstract|unsafe|readonly|ref|partial)\\s+)*record(?:\\s+(?:class|struct))?\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*<[^>{}\\r\\n]+>)?\\s*\\(",
        RegexOptions.Multiline)]
    private static partial Regex PositionalBoundaryContractRegex();
}
