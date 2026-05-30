/**
 * Map a round to a human label. The label depends on the section the round
 * belongs to (main, upper, lower, grand final, 3rd place) and how close it
 * is to the final of *its own* section — Upper and Lower brackets each have
 * their own "Final"/"Semifinal" sequence in canonical Double Elimination.
 *
 * For Swiss every round is just "Раунд N".
 */
export function roundLabel(
  format: string,
  roundNumber: number,
  totalRounds: number,
  bracketType?: string,
): string {
  const fmt = format.toLowerCase();
  const isElimination = fmt.includes('elimination') || fmt.includes('elim');

  if (!isElimination) {
    return `Раунд ${roundNumber}`;
  }

  if (bracketType === 'GrandFinal') {
    return roundNumber === 1 ? 'Гранд-финал' : 'Reset';
  }
  if (bracketType === 'ThirdPlace') {
    return 'Матч за 3 место';
  }

  const prefix = bracketType === 'Lower' ? 'LB ' : bracketType === 'Upper' ? 'UB ' : '';
  // distance from the final of this bracket: 0 = final, 1 = semifinal, etc.
  const fromFinal = totalRounds - roundNumber;

  switch (fromFinal) {
    case 0: return `${prefix}Финал`;
    case 1: return `${prefix}Полуфинал`;
    case 2: return `${prefix}Четвертьфинал`;
    case 3: return `${prefix}1/8 финала`;
    case 4: return `${prefix}1/16 финала`;
    default: return `${prefix}Раунд ${roundNumber}`;
  }
}
