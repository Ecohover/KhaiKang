using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public static class AiFixedRoleCodes
{
    public static string ToCode(AiFixedRole role) => role switch
    {
        AiFixedRole.PmAi => "pm_ai",
        AiFixedRole.DevelopmentAi => "development_ai",
        AiFixedRole.QaAi => "qa_ai",
        AiFixedRole.ReviewerAi => "reviewer_ai",
        AiFixedRole.DevOpsAi => "devops_ai",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static bool TryParse(string code, out AiFixedRole role)
    {
        role = code switch
        {
            "pm_ai" => AiFixedRole.PmAi,
            "development_ai" => AiFixedRole.DevelopmentAi,
            "qa_ai" => AiFixedRole.QaAi,
            "reviewer_ai" => AiFixedRole.ReviewerAi,
            "devops_ai" => AiFixedRole.DevOpsAi,
            _ => default,
        };
        return code is "pm_ai" or "development_ai" or "qa_ai" or "reviewer_ai" or "devops_ai";
    }
}
