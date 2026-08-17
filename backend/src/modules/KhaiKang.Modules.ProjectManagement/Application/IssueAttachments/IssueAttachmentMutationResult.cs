using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed record IssueAttachmentMutationResult
{
    private IssueAttachmentMutationResult(
        IssueAttachmentOutcome outcome,
        IssueAttachmentResponse? attachment)
    {
        Outcome = outcome;
        Attachment = attachment;
    }

    public IssueAttachmentOutcome Outcome { get; }

    public IssueAttachmentResponse? Attachment { get; }

    public static IssueAttachmentMutationResult Uploaded(IssueAttachmentResponse attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        return new IssueAttachmentMutationResult(
            IssueAttachmentOutcome.Succeeded,
            attachment);
    }

    public static IssueAttachmentMutationResult Deleted()
    {
        return new IssueAttachmentMutationResult(
            IssueAttachmentOutcome.Succeeded,
            attachment: null);
    }

    public static IssueAttachmentMutationResult Failure(IssueAttachmentOutcome outcome)
    {
        if (outcome == IssueAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Use Uploaded or Deleted when the attachment mutation succeeds.");
        }

        return new IssueAttachmentMutationResult(outcome, attachment: null);
    }
}
