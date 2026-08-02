using System.ComponentModel.DataAnnotations;

namespace KhaiKang.Modules.Identity.Configuration;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    [Range(5, 120)]
    public int AuthenticationTicketMinutes { get; init; } = 30;

    [Range(1, 72)]
    public int SessionHours { get; init; } = 8;

    [Range(1, 90)]
    public int RememberMeDays { get; init; } = 30;

    [Range(12, 128)]
    public int MinimumPasswordLength { get; init; } = 12;

    public bool? RequireSecureCookies { get; init; }
}
