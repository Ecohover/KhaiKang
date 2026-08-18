namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TestRunItemAttachmentContentResult
{
    private TestRunItemAttachmentContentResult(
        TestRunItemAttachmentOutcome outcome,
        Stream? content,
        string? contentType,
        string? fileName)
    {
        Outcome = outcome;
        Content = content;
        ContentType = contentType;
        FileName = fileName;
    }

    public TestRunItemAttachmentOutcome Outcome { get; }

    public Stream? Content { get; }

    public string? ContentType { get; }

    public string? FileName { get; }

    public static TestRunItemAttachmentContentResult Success(
        Stream content,
        string contentType,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return new TestRunItemAttachmentContentResult(
            TestRunItemAttachmentOutcome.Succeeded,
            content,
            contentType,
            fileName);
    }

    public static TestRunItemAttachmentContentResult Failure(
        TestRunItemAttachmentOutcome outcome)
    {
        if (outcome == TestRunItemAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failure result cannot use the succeeded outcome.");
        }

        return new TestRunItemAttachmentContentResult(outcome, null, null, null);
    }
}
