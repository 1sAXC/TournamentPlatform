import type { CSSProperties, ReactNode } from 'react';

interface Props {
  title?: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
  pad?: boolean;
  style?: CSSProperties;
  className?: string;
}

export function Card({ title, actions, children, pad = true, style, className }: Props) {
  return (
    <div className={className ? `card ${className}` : 'card'} style={style}>
      {title && (
        <div className="card-head">
          <h3>{title}</h3>
          <div className="spacer-x" />
          {actions}
        </div>
      )}
      <div className={pad ? 'card-pad' : ''}>{children}</div>
    </div>
  );
}
