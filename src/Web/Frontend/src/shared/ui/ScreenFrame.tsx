import type { ReactNode } from 'react';
import { TopBar, type NavLinkItem } from './TopBar';

interface Props {
  nav: NavLinkItem[];
  children: ReactNode;
}

export function ScreenFrame({ nav, children }: Props) {
  return (
    <div className="tp-screen">
      <TopBar nav={nav} />
      <main className="tp-main">
        <div className="page">{children}</div>
      </main>
    </div>
  );
}
