namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record IssueAttachmentContentResult
{
    private IssueAttachmentContentResult(
        IssueAttachmentOutcome outcome,
        Stream? content,
        string? contentType,
        string? fileName)
    {
        Outcome = outcome;
        Content = content;
        ContentType = contentType;
        FileName = fileName;
    }

    public IssueAttachmentOutcome Outcome { get; }

    public Stream? Content { get; }

    public string? ContentType { get; }

    public string? FileName { get; }

    public static IssueAttachmentContentResult Success(
        Stream content,
        string contentType,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(fileName);
        return new IssueAttachmentContentResult(
            IssueAttachmentOutcome.Succeeded,
            content,
            contentType,
            fileName);
    }

    public static IssueAttachmentContentResult Failure(IssueAttachmentOutcome outcome)
    {
        if (outcome == IssueAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Success when attachment content is available.");
        }

        return new IssueAttachmentContentResult(
            outcome,
            content: null,
            contentType: null,
            fileName: null);
    }
}
