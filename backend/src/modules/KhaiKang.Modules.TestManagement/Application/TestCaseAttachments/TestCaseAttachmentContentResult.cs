namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TestCaseAttachmentContentResult
{
    private TestCaseAttachmentContentResult(
        TestCaseAttachmentOutcome outcome,
        Stream? content,
        string? contentType,
        string? fileName)
    {
        Outcome = outcome;
        Content = content;
        ContentType = contentType;
        FileName = fileName;
    }

    public TestCaseAttachmentOutcome Outcome { get; }

    public Stream? Content { get; }

    public string? ContentType { get; }

    public string? FileName { get; }

    public static TestCaseAttachmentContentResult Success(
        Stream content,
        string contentType,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return new TestCaseAttachmentContentResult(
            TestCaseAttachmentOutcome.Succeeded,
            content,
            contentType,
            fileName);
    }

    public static TestCaseAttachmentContentResult Failure(
        TestCaseAttachmentOutcome outcome)
    {
        if (outcome == TestCaseAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failure result cannot use the succeeded outcome.");
        }

        return new TestCaseAttachmentContentResult(outcome, null, null, null);
    }
}
