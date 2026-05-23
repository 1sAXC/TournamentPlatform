import type { ReactNode } from 'react';

export type BadgeTone =
  | 'open' | 'full' | 'progress' | 'done' | 'cancelled' | 'pending' | 'success';

const labels: Record<string, string> = {
  open: 'Открыт',
  full: 'Заполнен',
  progress: 'Идёт',
  done: 'Завершён',
  cancelled: 'Отменён',
  pending: 'На проверке',
  success: 'Активен',
};

interface Props {
  tone?: BadgeTone | string;
  children?: ReactNode;
  noDot?: boolean;
}

export function Badge({ tone, children, noDot }: Props) {
  const label = children ?? (tone ? labels[tone] ?? tone : '');
  return (
    <span className={`badge ${tone || ''} ${noDot ? 'no-dot' : ''}`}>
      {label}
    </span>
  );
}
