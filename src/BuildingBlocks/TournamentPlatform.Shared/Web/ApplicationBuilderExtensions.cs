using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TournamentPlatform.Shared.Correlation;
using TournamentPlatform.Shared.Exceptions;

namespace TournamentPlatform.Shared.Web;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTournamentPlatformCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseTournamentPlatformExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("TournamentPlatform.ExceptionHandling");

                var problemDetails = CreateProblemDetails(exception);
                context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                {
                    logger.LogError(exception, "Unhandled request exception.");
                }
                else
                {
                    logger.LogWarning(exception, "Handled request exception.");
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    problemDetails,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            });
        });
    }

    private static ProblemDetails CreateProblemDetails(Exception? exception)
    {
        if (exception is not null && exception.GetType().FullName == "FluentValidation.ValidationException")
        {
            return new ProblemDetails
            {
                Type = "https://errors.tournament-platform/validation",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred."
            };
        }

        if (exception is NotFoundException)
        {
            return new ProblemDetails
            {
                Type = "https://errors.tournament-platform/not-found",
                Title = exception.Message,
                Status = StatusCodes.Status404NotFound
            };
        }

        if (exception is ConflictException
            || exception?.GetType().Name.Contains("PersistenceConflict", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new ProblemDetails
            {
                Type = "https://errors.tournament-platform/conflict",
                Title = exception.Message,
                Status = StatusCodes.Status409Conflict
            };
        }

        if (exception is ForbiddenAccessException)
        {
            return new ProblemDetails
            {
                Type = "https://errors.tournament-platform/forbidden",
                Title = exception.Message,
                Status = StatusCodes.Status403Forbidden
            };
        }

        if (exception is DomainException
            || exception?.GetType().Name.Contains("TeamBalancing", StringComparison.OrdinalIgnoreCase) == true
            || exception is ArgumentException)
        {
            return new ProblemDetails
            {
                Type = "https://errors.tournament-platform/domain",
                Title = exception.Message,
                Status = StatusCodes.Status400BadRequest
            };
        }

        return new ProblemDetails
        {
            Type = "https://errors.tournament-platform/unexpected",
            Title = "Unexpected error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred."
        };
    }
}
