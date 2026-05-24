export const DISCIPLINES = [
  { code: 'CS2', label: 'CS2' },
  { code: 'Valorant', label: 'Valorant' },
  { code: 'Standoff2', label: 'Standoff 2' },
  { code: 'PUBG', label: 'PUBG' },
  { code: 'Dota2', label: 'Dota 2' },
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

export const STATUS_LABEL: Record<string, string> = {
  Draft: 'Черновик',
  RegistrationOpen: 'Открыт',
  InProgress: 'Идёт',
  Completed: 'Завершён',
  Cancelled: 'Отменён',
};

export const STATUS_TONE: Record<string, string> = {
  Draft: 'pending',
  RegistrationOpen: 'open',
  InProgress: 'progress',
  Completed: 'done',
  Cancelled: 'cancelled',
};
