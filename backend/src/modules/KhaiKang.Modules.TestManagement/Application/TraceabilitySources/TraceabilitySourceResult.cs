namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TraceabilitySourceResult<T> where T : class
{
    private TraceabilitySourceResult(TestManagementOutcome outcome, T? value, string? code)
    {
        Outcome = outcome;
        Value = value;
        Code = code;
    }
    public TestManagementOutcome Outcome { get; }
    public T? Value { get; }
    public string? Code { get; }
    public static TraceabilitySourceResult<T> Success(T value) => new(TestManagementOutcome.Succeeded, value, null);
    public static TraceabilitySourceResult<T> Failure(TestManagementOutcome outcome, string? code = null) => new(outcome, null, code);
}
