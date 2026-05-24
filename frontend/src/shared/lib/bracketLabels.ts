/**
 * Map a round to a human label.
 *
 * For elimination formats we name the final rounds semantically:
 *   …  → 1/8 финала → Четвертьфинал → Полуфинал → Финал.
 * Earlier rounds fall back to "Раунд N" so we don't claim a bracket size
 * we can't actually see.
 *
 * For Swiss every round is just "Раунд N".
 */
export function roundLabel(
  format: string,
  roundNumber: number,
  totalRounds: number,
): string {
  const fmt = format.toLowerCase();
  const isElimination = fmt.includes('elimination') || fmt.includes('elim');

  if (!isElimination) {
    return `Раунд ${roundNumber}`;
  }

  // distance from the final: 0 = final, 1 = semifinal, 2 = quarterfinal, ...
  const fromFinal = totalRounds - roundNumber;

  switch (fromFinal) {
    case 0: return 'Финал';
    case 1: return 'Полуфинал';
    case 2: return 'Четвертьфинал';
    case 3: return '1/8 финала';
    case 4: return '1/16 финала';
    default: return `Раунд ${roundNumber}`;
  }
}
