namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record CreateTestCaseStepRequest
{
    public CreateTestCaseStepRequest(string action, string expectedResult)
    {
        Action = action;
        ExpectedResult = expectedResult;
    }

    public string Action { get; }

    public string ExpectedResult { get; }
}
