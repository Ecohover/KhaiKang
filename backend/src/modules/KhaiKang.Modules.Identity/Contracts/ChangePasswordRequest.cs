namespace KhaiKang.Modules.Identity.Contracts;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
