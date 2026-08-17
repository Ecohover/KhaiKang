namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestRunProgressResponse
{
    public required int Total { get; init; }

    public required int NotRun { get; init; }

    public required int Passed { get; init; }

    public required int Failed { get; init; }

    public required int Blocked { get; init; }

    public required int Skipped { get; init; }
}
