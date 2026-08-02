using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.CommonUtils.Web.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKhaiKangCommonWeb(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.Remove("traceId");
            };
        });

        return services;
    }
}
