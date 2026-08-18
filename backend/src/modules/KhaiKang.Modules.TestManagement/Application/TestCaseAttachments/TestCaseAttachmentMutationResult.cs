using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed record TestCaseAttachmentMutationResult
{
    private TestCaseAttachmentMutationResult(
        TestCaseAttachmentOutcome outcome,
        TestCaseAttachmentResponse? attachment)
    {
        Outcome = outcome;
        Attachment = attachment;
    }

    public TestCaseAttachmentOutcome Outcome { get; }

    public TestCaseAttachmentResponse? Attachment { get; }

    public static TestCaseAttachmentMutationResult Uploaded(
        TestCaseAttachmentResponse attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return new TestCaseAttachmentMutationResult(
            TestCaseAttachmentOutcome.Succeeded,
            attachment);
    }

    public static TestCaseAttachmentMutationResult Deleted()
    {
        return new TestCaseAttachmentMutationResult(
            TestCaseAttachmentOutcome.Succeeded,
            null);
    }

    public static TestCaseAttachmentMutationResult Failure(
        TestCaseAttachmentOutcome outcome)
    {
        EnsureFailure(outcome);

        return new TestCaseAttachmentMutationResult(outcome, null);
    }

    private static void EnsureFailure(TestCaseAttachmentOutcome outcome)
    {
        if (outcome == TestCaseAttachmentOutcome.Succeeded)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A failure result cannot use the succeeded outcome.");
        }
    }
}
