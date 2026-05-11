using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public interface IBracketGeneratorFactory
{
    IBracketGenerator GetGenerator(TournamentFormat format);
}
