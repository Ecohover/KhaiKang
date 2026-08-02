namespace KhaiKang.Modules.Identity.Contracts;

public sealed record LoginRequest(string Username, string Password, bool RememberMe);
