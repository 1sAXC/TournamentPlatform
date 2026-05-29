// Centralised display logic for a match's score. We capture two scales:
//   - maps   (e.g. "2-1" in a Bo3 series) — the headline organisers/players see.
//   - rounds (sum across all maps, e.g. "26-20") — fed into ELO.
// UI consistently shows maps as the primary score and rounds as a hint.

export interface MatchScoreLike {
  winnerTeamId?: string | null;
  teamAId?: string | null;
  teamBId?: string | null;
  winnerScore?: number | null;
  loserScore?: number | null;
  winnerMaps?: number | null;
  loserMaps?: number | null;
}

export interface FormattedMatchScore {
  /** e.g. "2-1" or "—" when not played. Primary line. */
  primary: string;
  /** e.g. "26-20" — rounds total — or null when rounds aren't recorded. */
  secondary: string | null;
}

export function formatMatchScore(m: MatchScoreLike): FormattedMatchScore {
  const aWon = m.winnerTeamId != null && m.winnerTeamId === m.teamAId;
  const bWon = m.winnerTeamId != null && m.winnerTeamId === m.teamBId;

  function pair(winnerVal: number | null | undefined, loserVal: number | null | undefined): string | null {
    if (winnerVal == null && loserVal == null) return null;
    const a = aWon ? winnerVal : bWon ? loserVal : null;
    const b = aWon ? loserVal : bWon ? winnerVal : null;
    if (a == null && b == null) return null;
    return `${a ?? '-'}–${b ?? '-'}`;
  }

  const maps = pair(m.winnerMaps, m.loserMaps);
  const rounds = pair(m.winnerScore, m.loserScore);

  // Prefer maps for the primary line; if absent (older data / technical
  // defeat without rounds), fall back to rounds; if neither — dash.
  return {
    primary: maps ?? rounds ?? '—',
    secondary: maps !== null && rounds !== null ? rounds : null,
  };
}
