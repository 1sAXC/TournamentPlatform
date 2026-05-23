import type { ReactNode } from 'react';

interface Props {
  title: string;
  children?: ReactNode;
}

export function EmptyState({ title, children }: Props) {
  return (
    <div className="empty">
      <div className="empty-title">{title}</div>
      {children}
    </div>
  );
}
