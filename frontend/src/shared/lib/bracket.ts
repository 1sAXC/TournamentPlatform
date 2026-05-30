import type { BracketRound } from '@/shared/ui/TournamentBracket';
import type { MatchResponse, RoundResponse, TournamentDetailsResponse } from '@/shared/api/types';
import { roundLabel } from '@/shared/lib/bracketLabels';

// A bracket "section" is one horizontal strip of the bracket viewer.
// For a Double Elimination tournament there are typically three sections:
// Upper bracket (fold layout), Lower bracket (linear layout — its round
// shape doesn't halve), and a 1-2 round Grand Final track. For Single
// Elimination there is the main fold plus an optional 3rd-place card.
//
// `layout` tells the renderer how to position matches between rounds:
//   fold   — children pair into next-round parents (classic bracket connectors)
//   linear — each round is its own column with no connectors; used when the
//            round-to-round shape isn't a clean halving (LB absorption rounds,
//            single-match Grand Final / 3rd-place tracks).
export interface BracketSection {
  title?: string;
  layout: 'fold' | 'linear';
  rounds: BracketRound[];
}

export function buildBracketSections(
  data: TournamentDetailsResponse,
  onMatchClick?: (m: MatchResponse) => void,
): BracketSection[] {
  const sorted = [...data.rounds].sort((a, b) => a.number - b.number);
  const mk = (rounds: RoundResponse[]): BracketRound[] => {
    const total = rounds.length;
    return rounds.map((r) => ({
      label: roundLabel(data.format, r.number, total, r.bracketType),
      current: r.number === data.currentRoundNumber && data.status === 'InProgress',
      matches: r.matches.map((m) => mapMatch(data, r, m, onMatchClick)),
    }));
  };

  if (data.format === 'Swiss') {
    return [{ layout: 'linear', rounds: mk(sorted) }];
  }

  if (data.format === 'SingleElimination') {
    const main = sorted.filter(r => r.bracketType === 'Main');
    const thirdPlace = sorted.filter(r => r.bracketType === 'ThirdPlace');
    const sections: BracketSection[] = [];
    if (main.length > 0) sections.push({ layout: 'fold', rounds: mk(main) });
    if (thirdPlace.length > 0) {
      sections.push({ title: 'Матч за 3 место', layout: 'linear', rounds: mk(thirdPlace) });
    }
    return sections;
  }

  if (data.format === 'DoubleElimination') {
    const upper = sorted.filter(r => r.bracketType === 'Upper');
    const lower = sorted.filter(r => r.bracketType === 'Lower');
    const gf = sorted.filter(r => r.bracketType === 'GrandFinal');
    const sections: BracketSection[] = [];
    if (upper.length > 0) sections.push({ title: 'Верхняя сетка', layout: 'fold', rounds: mk(upper) });
    if (lower.length > 0) sections.push({ title: 'Нижняя сетка', layout: 'linear', rounds: mk(lower) });
    if (gf.length > 0) sections.push({ title: 'Гранд-финал', layout: 'linear', rounds: mk(gf) });
    return sections;
  }

  return [{ layout: 'linear', rounds: mk(sorted) }];
}

function mapMatch(
  data: TournamentDetailsResponse,
  round: RoundResponse,
  m: MatchResponse,
  onMatchClick?: (m: MatchResponse) => void,
) {
  return {
    id: m.id,
    label: `M${round.number}.${m.matchNumber}`,
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
  };
}
