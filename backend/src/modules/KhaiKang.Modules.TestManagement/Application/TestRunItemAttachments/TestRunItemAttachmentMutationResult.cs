using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TestRunItemAttachmentMutationResult
{
    private TestRunItemAttachmentMutationResult(
        TestRunItemAttachmentOutcome outcome,
        TestRunItemAttachmentResponse? attachment)
    {
        Outcome = outcome;
        Attachment = attachment;
    }

    public TestRunItemAttachmentOutcome Outcome { get; }

    public TestRunItemAttachmentResponse? Attachment { get; }

    public static TestRunItemAttachmentMutationResult Uploaded(
        TestRunItemAttachmentResponse attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return new TestRunItemAttachmentMutationResult(
            TestRunItemAttachmentOutcome.Succeeded,
            attachment);
    }

    public static TestRunItemAttachmentMutationResult Deleted()
    {
        return new TestRunItemAttachmentMutationResult(
            TestRunItemAttachmentOutcome.Succeeded,
            null);
    }

    public static TestRunItemAttachmentMutationResult Failure(
        TestRunItemAttachmentOutcome outcome)
    {
        if (outcome == TestRunItemAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failure result cannot use the succeeded outcome.");
        }

        return new TestRunItemAttachmentMutationResult(outcome, null);
    }
}
