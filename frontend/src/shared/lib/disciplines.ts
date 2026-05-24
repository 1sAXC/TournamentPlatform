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
