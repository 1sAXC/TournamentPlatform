import type { ReactNode } from 'react';

interface Props {
  title: string;
  sub: string;
  alert?: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
}

export function AuthShell({ title, sub, alert, footer, children }: Props) {
  return (
    <div className="tp-auth">
      <div className="card">
        <div className="brand-head">
          <div className="tp-brand" style={{ fontSize: 22, justifyContent: 'center' }}>
            <span className="b1">Tournament</span><span className="b2">Platform</span>
          </div>
          <div className="eyebrow" style={{ marginTop: 10 }}>Турнирная платформа</div>
        </div>
        <h1>{title}</h1>
        <p className="sub">{sub}</p>
        {alert}
        {children}
        {footer && <div className="foot">{footer}</div>}
      </div>
    </div>
  );
}
