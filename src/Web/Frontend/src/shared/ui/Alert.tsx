import type { ReactNode } from 'react';
import { Icon, type IconName } from './Icon';

interface Props {
  kind?: 'info' | 'warn' | 'error';
  icon?: IconName;
  children: ReactNode;
}

export function Alert({ kind = 'info', icon = 'flag', children }: Props) {
  return (
    <div className={`alert alert-${kind}`}>
      <Icon name={icon} size={14} />
      <span>{children}</span>
    </div>
  );
}
