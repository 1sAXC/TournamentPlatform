using Microsoft.Extensions.DependencyInjection;
using Tournament.Application.Brackets;
using Tournament.Application.Matches;
using Tournament.Application.TeamBalancer;
using Tournament.Application.Tournaments.Services;

namespace Tournament.Application.DependencyInjection;

public static class TournamentApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentApplication(this IServiceCollection services)
    {
        services.AddScoped<ITournamentService, TournamentService>();
        services.AddScoped<ITournamentLifecycleService, TournamentLifecycleService>();
        services.AddScoped<IPlayerRatingProjectionService, PlayerRatingProjectionService>();
        services.AddSingleton<IRandomProvider, RandomProvider>();
        services.AddScoped<ITeamBalancer, GreedyTeamBalancer>();
        services.AddScoped<IBracketGenerator, SingleEliminationBracketGenerator>();
        services.AddScoped<IBracketGenerator, DoubleEliminationBracketGenerator>();
        services.AddScoped<IBracketGenerator, SwissBracketGenerator>();
        services.AddScoped<IBracketGeneratorFactory, BracketGeneratorFactory>();
        services.AddScoped<ISwissRoundService, SwissRoundService>();
        services.AddScoped<IMatchResultService, MatchResultService>();
        return services;
    }
}
