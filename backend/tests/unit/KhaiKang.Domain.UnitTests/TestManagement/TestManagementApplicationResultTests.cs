using KhaiKang.Modules.TestManagement.Application;
using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Domain.UnitTests.TestManagement;

public sealed class TestManagementApplicationResultTests
{
    [Fact]
    public void TestManagementResult_SeparatesSuccessFromFailure()
    {
        var success = TestManagementResult<string>.Success("value");
        var failure = TestManagementResult<string>.Failure(
            TestManagementOutcome.Conflict,
            "version_conflict");

        Assert.Equal(TestManagementOutcome.Succeeded, success.Outcome);
        Assert.Equal("value", success.Value);
        Assert.Null(success.Code);
        Assert.Equal(TestManagementOutcome.Conflict, failure.Outcome);
        Assert.Null(failure.Value);
        Assert.Equal("version_conflict", failure.Code);
        Assert.Throws<ArgumentNullException>(
            () => TestManagementResult<string>.Success(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestManagementResult<string>.Failure(TestManagementOutcome.Succeeded));
    }

    [Fact]
    public void TestManagementResult_HidesDirectConstruction()
    {
        var constructors = typeof(TestManagementResult<string>)
            .GetConstructors(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

        Assert.NotEmpty(constructors);
        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));
    }

    [Fact]
    public void TestCaseAttachmentMutationResult_DistinguishesUploadFromDelete()
    {
        var attachment = CreateTestCaseAttachment();

        var uploaded = TestCaseAttachmentMutationResult.Uploaded(attachment);
        var deleted = TestCaseAttachmentMutationResult.Deleted();
        var failure = TestCaseAttachmentMutationResult.Failure(
            TestCaseAttachmentOutcome.InvalidFile);

        Assert.Same(attachment, uploaded.Attachment);
        Assert.Null(deleted.Attachment);
        Assert.Null(failure.Attachment);
        Assert.Equal(TestCaseAttachmentOutcome.Succeeded, uploaded.Outcome);
        Assert.Equal(TestCaseAttachmentOutcome.Succeeded, deleted.Outcome);
        Assert.Equal(TestCaseAttachmentOutcome.InvalidFile, failure.Outcome);
        Assert.Throws<ArgumentNullException>(
            () => TestCaseAttachmentMutationResult.Uploaded(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestCaseAttachmentMutationResult.Failure(TestCaseAttachmentOutcome.Succeeded));
    }

    [Fact]
    public void TestRunItemAttachmentMutationResult_DistinguishesUploadFromDelete()
    {
        var attachment = CreateTestRunItemAttachment();

        var uploaded = TestRunItemAttachmentMutationResult.Uploaded(attachment);
        var deleted = TestRunItemAttachmentMutationResult.Deleted();
        var failure = TestRunItemAttachmentMutationResult.Failure(
            TestRunItemAttachmentOutcome.RunNotInProgress);

        Assert.Same(attachment, uploaded.Attachment);
        Assert.Null(deleted.Attachment);
        Assert.Null(failure.Attachment);
        Assert.Equal(TestRunItemAttachmentOutcome.Succeeded, uploaded.Outcome);
        Assert.Equal(TestRunItemAttachmentOutcome.Succeeded, deleted.Outcome);
        Assert.Equal(TestRunItemAttachmentOutcome.RunNotInProgress, failure.Outcome);
        Assert.Throws<ArgumentNullException>(
            () => TestRunItemAttachmentMutationResult.Uploaded(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestRunItemAttachmentMutationResult.Failure(
                TestRunItemAttachmentOutcome.Succeeded));
    }

    [Fact]
    public void AttachmentContentResults_RequireCompleteSuccessPayloads()
    {
        using var caseContent = new MemoryStream([1, 2, 3]);
        using var runContent = new MemoryStream([4, 5, 6]);

        var caseSuccess = TestCaseAttachmentContentResult.Success(
            caseContent,
            "image/png",
            "case.png");
        var runSuccess = TestRunItemAttachmentContentResult.Success(
            runContent,
            "text/plain",
            "run.txt");

        Assert.Same(caseContent, caseSuccess.Content);
        Assert.Equal("image/png", caseSuccess.ContentType);
        Assert.Equal("case.png", caseSuccess.FileName);
        Assert.Same(runContent, runSuccess.Content);
        Assert.Equal("text/plain", runSuccess.ContentType);
        Assert.Equal("run.txt", runSuccess.FileName);
        Assert.Throws<ArgumentNullException>(
            () => TestCaseAttachmentContentResult.Success(null!, "image/png", "case.png"));
        Assert.Throws<ArgumentException>(
            () => TestRunItemAttachmentContentResult.Success(runContent, " ", "run.txt"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestCaseAttachmentContentResult.Failure(TestCaseAttachmentOutcome.Succeeded));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestRunItemAttachmentContentResult.Failure(
                TestRunItemAttachmentOutcome.Succeeded));
    }

    private static TestCaseAttachmentResponse CreateTestCaseAttachment()
    {
        return new TestCaseAttachmentResponse
        {
            Id = Guid.NewGuid(),
            TestCaseId = Guid.NewGuid(),
            OriginalFileName = "case.png",
            ContentType = "image/png",
            FileSize = 3,
            FileHash = "case-hash",
            UploadedByAccountId = Guid.NewGuid(),
            UploadedByUsername = "tester",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static TestRunItemAttachmentResponse CreateTestRunItemAttachment()
    {
        return new TestRunItemAttachmentResponse
        {
            Id = Guid.NewGuid(),
            TestRunItemId = Guid.NewGuid(),
            OriginalFileName = "run.txt",
            ContentType = "text/plain",
            FileSize = 3,
            FileHash = "run-hash",
            UploadedByAccountId = Guid.NewGuid(),
            UploadedByUsername = "tester",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
