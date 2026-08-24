namespace KhaiKang.Modules.Identity.Application;

public static class IdentityConstants
{
    public const string AuthenticationScheme = "KhaiKang.Cookie";
    public const string RefreshCookieName = "KhaiKang.Refresh";
    public const string SessionIdClaim = "session_id";
    public const string SystemAdminRole = "System Admin";
    public const string UserRole = "User";
    public const string UserRoleId = "94fbc708-1764-4d11-a8e6-df5e786cb678";
    public const string AiAccountCreatePermission = "ai_account.create";
    public const string AiTokenAuthenticationScheme = "KhaiKang.AiToken";
    public const string AuthenticationSelectorScheme = "KhaiKang.Authentication";
    public const string HumanSessionPolicy = "human_session";
    public const string AccountTypeClaim = "account_type";
    public const string FixedRoleClaim = "fixed_role";
    public const string OwnerAccountIdClaim = "owner_account_id";
}
