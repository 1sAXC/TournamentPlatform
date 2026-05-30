// Platform supports only round-based shooter disciplines. The match score
// is captured both by maps-in-series (for display) and total rounds (for
// ELO weighting), so a non-round discipline like Dota would need a
// different score model.
export const DISCIPLINES = [
  { code: 'CS2', label: 'CS2' },
  { code: 'Valorant', label: 'Valorant' },
  { code: 'Standoff2', label: 'Standoff 2' },
] as const;

export function disciplineLabel(code: string): string {
  return DISCIPLINES.find(d => d.code === code)?.label ?? code;
}

export const FORMATS = [
  { code: 'Swiss', label: 'Swiss System' },
  { code: 'SingleElimination', label: 'Single Elimination' },
  { code: 'DoubleElimination', label: 'Double Elimination' },
] as const;

export function formatLabel(code: string): string {
  return FORMATS.find(f => f.code === code)?.label ?? code;
}

// Planned round count derived from capacity, used before a tournament starts
// and its bracket is generated. Counts the number of distinct rounds played:
//   SE: log2(N) main rounds + a 3rd-place match (same number as the final
//       but a separate round); we count it once since it's a real round.
//   DE: R UB rounds + 2(R-1) LB rounds + 1 GF (the optional reset isn't
//       counted — it depends on who wins).
//   Swiss: configured separately.
export function plannedRoundCount(
  format: string,
  maxPlayers: number,
  teamSize: number,
  swissRounds?: number | null,
): number | null {
  if (format === 'Swiss') return swissRounds ?? null;
  const teams = Math.floor(maxPlayers / Math.max(teamSize, 1));
  if (teams < 2) return null;
  const r = Math.ceil(Math.log2(teams));
  if (format === 'SingleElimination') return r + (r >= 2 ? 1 : 0);
  if (format === 'DoubleElimination') return r + Math.max(0, 2 * (r - 1)) + 1;
  return null;
}

export const STATUS_LABEL: Record<string, string> = {
  Open: 'Открыт',
  Full: 'Заполнен',
  InProgress: 'Идёт',
  Completed: 'Завершён',
  Cancelled: 'Отменён',
};

export const STATUS_TONE: Record<string, string> = {
  Open: 'open',
  Full: 'full',
  InProgress: 'progress',
  Completed: 'done',
  Cancelled: 'cancelled',
};

export const ACCOUNT_STATUS_LABEL: Record<string, string> = {
  Active: 'Активен',
  PendingApproval: 'На рассмотрении',
  Rejected: 'Отклонён',
  Deleted: 'Удалён',
};

export function accountStatusLabel(status: string | undefined | null): string {
  if (!status) return '—';
  return ACCOUNT_STATUS_LABEL[status] ?? status;
}
