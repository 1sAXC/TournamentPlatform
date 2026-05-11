using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public sealed class BracketGeneratorFactory(IEnumerable<IBracketGenerator> generators) : IBracketGeneratorFactory
{
    public IBracketGenerator GetGenerator(TournamentFormat format)
    {
        return format switch
        {
            TournamentFormat.SingleElimination => generators.OfType<SingleEliminationBracketGenerator>().Single(),
            TournamentFormat.DoubleElimination => generators.OfType<DoubleEliminationBracketGenerator>().Single(),
            TournamentFormat.Swiss => generators.OfType<SwissBracketGenerator>().Single(),
            _ => throw new InvalidOperationException($"Unsupported tournament format {format}.")
        };
    }
}
