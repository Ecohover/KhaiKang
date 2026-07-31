namespace KhaiKang.Modules.Identity.Contracts;

public sealed record CreateAccountResponse(
    AccountResponse Account,
    string InitialPassword);
