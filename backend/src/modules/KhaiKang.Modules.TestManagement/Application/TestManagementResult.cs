namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TestManagementResult<T>
    where T : class
{
    private TestManagementResult(
        TestManagementOutcome outcome,
        T? value,
        string? code)
    {
        if (outcome == TestManagementOutcome.Succeeded && value is null)
        {
            throw new ArgumentNullException(
                nameof(value),
                "A successful result requires a value.");
        }

        if (outcome != TestManagementOutcome.Succeeded && value is not null)
        {
            throw new ArgumentException(
                "A failed result cannot contain a value.",
                nameof(value));
        }

        if (outcome == TestManagementOutcome.Succeeded && code is not null)
        {
            throw new ArgumentException(
                "A successful result cannot contain an error code.",
                nameof(code));
        }

        Outcome = outcome;
        Value = value;
        Code = code;
    }

    public TestManagementOutcome Outcome { get; }

    public T? Value { get; }

    public string? Code { get; }

    public static TestManagementResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new TestManagementResult<T>(TestManagementOutcome.Succeeded, value, null);
    }

    public static TestManagementResult<T> Failure(
        TestManagementOutcome outcome,
        string? code = null)
    {
        if (outcome == TestManagementOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failure result cannot use the succeeded outcome.");
        }

        return new TestManagementResult<T>(outcome, default, code);
    }
}
