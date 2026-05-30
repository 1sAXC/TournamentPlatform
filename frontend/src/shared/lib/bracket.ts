import type { BracketRound } from '@/shared/ui/TournamentBracket';
import type { MatchResponse, TournamentDetailsResponse } from '@/shared/api/types';
import { roundLabel } from '@/shared/lib/bracketLabels';

// Builds the bracket view-model from a tournament's rounds. Pass `onMatchClick`
// to make cells clickable — receivers can navigate to the match page (public
// view) or open an editor modal (organizer view). The callback decides what
// to do per match; this helper just wires it through. Cells with no teams
// yet (status 'tbd') are never clickable.
export function buildBracketRounds(
  data: TournamentDetailsResponse,
  onMatchClick?: (m: MatchResponse) => void,
): BracketRound[] {
  const totalRounds = data.rounds.length;
  return data.rounds.map((r) => ({
    label: roundLabel(data.format, r.number, totalRounds),
    current: r.number === data.currentRoundNumber && data.status === 'InProgress',
    matches: r.matches.map((m) => ({
      id: m.id,
      label: `M${r.number}.${m.matchNumber}`,
      a: data.teams.find(t => t.id === m.teamAId)?.name ?? null,
      b: data.teams.find(t => t.id === m.teamBId)?.name ?? null,
      // Bracket cells are compact — show maps (2-1) rather than rounds (26-20).
      // Fall back to rounds for older records where maps are not populated.
      sa: m.winnerTeamId === m.teamAId ? (m.winnerMaps ?? m.winnerScore ?? null)
        : m.winnerTeamId === m.teamBId ? (m.loserMaps ?? m.loserScore ?? null) : null,
      sb: m.winnerTeamId === m.teamBId ? (m.winnerMaps ?? m.winnerScore ?? null)
        : m.winnerTeamId === m.teamAId ? (m.loserMaps ?? m.loserScore ?? null) : null,
      win: m.winnerTeamId
        ? m.winnerTeamId === m.teamAId ? 'a' as const
          : m.winnerTeamId === m.teamBId ? 'b' as const : null
        : null,
      status: m.status === 'Completed' ? 'done' as const
        : m.teamAId && m.teamBId ? 'pending' as const
          : 'tbd' as const,
      onClick: !onMatchClick || !m.teamAId || !m.teamBId ? undefined : () => onMatchClick(m),
    })),
  }));
}
