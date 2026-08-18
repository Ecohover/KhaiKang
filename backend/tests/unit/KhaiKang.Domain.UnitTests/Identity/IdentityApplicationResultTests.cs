using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Domain.UnitTests.Identity;

public sealed class IdentityApplicationResultTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LoginSuccess_RequiresAndReturnsBothPayloads()
    {
        var session = CreateLoginSession();
        var user = CreateAuthenticatedUserResponse();

        var result = LoginResult.Success(session, user);

        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
        Assert.Same(session, result.Session);
        Assert.Same(user, result.User);
        Assert.Throws<ArgumentNullException>(() => LoginResult.Success(null!, user));
        Assert.Throws<ArgumentNullException>(() => LoginResult.Success(session, null!));
    }

    [Fact]
    public void InvalidCredentials_DoesNotExposeLoginPayloads()
    {
        var result = LoginResult.InvalidCredentials();

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Session);
        Assert.Null(result.User);
    }

    [Fact]
    public void CreateAccountSuccess_RequiresAResponse()
    {
        var response = CreateAccountResponse();

        var result = CreateAccountResult.Success(response);

        Assert.Equal(CreateAccountOutcome.Succeeded, result.Outcome);
        Assert.Same(response, result.Response);
        Assert.Throws<ArgumentNullException>(() => CreateAccountResult.Success(null!));
    }

    [Theory]
    [InlineData(CreateAccountOutcome.UsernameConflict)]
    [InlineData(CreateAccountOutcome.UserRoleNotConfigured)]
    public void CreateAccountFailure_DoesNotExposeAResponse(CreateAccountOutcome outcome)
    {
        var result = CreateAccountResult.Failure(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Response);
    }

    [Fact]
    public void CreateAccountFailure_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateAccountResult.Failure(CreateAccountOutcome.Succeeded));
    }

    [Fact]
    public void UpdateAccountSuccess_RequiresAnAccount()
    {
        var account = CreateAccountResponse().Account;

        var result = UpdateAccountResult.Success(account);

        Assert.Equal(UpdateAccountOutcome.Succeeded, result.Outcome);
        Assert.Same(account, result.Account);
        Assert.Throws<ArgumentNullException>(() => UpdateAccountResult.Success(null!));
    }

    [Theory]
    [InlineData(UpdateAccountOutcome.NotFound)]
    [InlineData(UpdateAccountOutcome.UsernameConflict)]
    [InlineData(UpdateAccountOutcome.VersionConflict)]
    [InlineData(UpdateAccountOutcome.CannotUpdateOwnAccount)]
    public void UpdateAccountFailure_DoesNotExposeAnAccount(UpdateAccountOutcome outcome)
    {
        var result = UpdateAccountResult.Failure(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Account);
    }

    [Fact]
    public void UpdateAccountFailure_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UpdateAccountResult.Failure(UpdateAccountOutcome.Succeeded));
    }

    [Fact]
    public void UpdateAccountStatusSuccess_RequiresAnAccount()
    {
        var account = CreateAccountResponse().Account;

        var result = UpdateAccountStatusResult.Success(account);

        Assert.Equal(UpdateAccountStatusOutcome.Succeeded, result.Outcome);
        Assert.Same(account, result.Account);
        Assert.Throws<ArgumentNullException>(() => UpdateAccountStatusResult.Success(null!));
    }

    [Theory]
    [InlineData(UpdateAccountStatusOutcome.NotFound)]
    [InlineData(UpdateAccountStatusOutcome.VersionConflict)]
    [InlineData(UpdateAccountStatusOutcome.CannotChangeOwnStatus)]
    public void UpdateAccountStatusFailure_DoesNotExposeAnAccount(
        UpdateAccountStatusOutcome outcome)
    {
        var result = UpdateAccountStatusResult.Failure(outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.Account);
    }

    [Fact]
    public void UpdateAccountStatusFailure_RejectsSucceededOutcome()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UpdateAccountStatusResult.Failure(UpdateAccountStatusOutcome.Succeeded));
    }

    private static LoginSession CreateLoginSession()
    {
        return new LoginSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OccurredAt,
            OccurredAt.AddHours(1),
            isPersistent: false);
    }

    private static AuthenticatedUserResponse CreateAuthenticatedUserResponse()
    {
        return new AuthenticatedUserResponse
        {
            Id = Guid.NewGuid(),
            Username = "reviewer",
            SystemRoles = ["User"],
            SystemPermissions = ["project.read"],
            MustChangePassword = false,
        };
    }

    private static CreateAccountResponse CreateAccountResponse()
    {
        return new CreateAccountResponse
        {
            Account = new AccountResponse
            {
                Id = Guid.NewGuid(),
                Username = "reviewer",
                AccountType = "human",
                Status = "active",
                SystemRoles = ["User"],
                MustChangePassword = true,
                LastLoginAt = null,
                CreatedAt = OccurredAt,
                UpdatedAt = OccurredAt,
                Version = 1,
            },
            InitialPassword = "Temporary-Pass-123!",
        };
    }
}
