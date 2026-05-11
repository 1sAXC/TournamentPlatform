using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TournamentPlatform.Shared.Correlation;

namespace TournamentPlatform.Shared.Web;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentPlatformApiDefaults(this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddProblemDetails();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "https://errors.tournament-platform/validation",
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred."
                };

                return new BadRequestObjectResult(problemDetails);
            };
        });

        return services;
    }
}
