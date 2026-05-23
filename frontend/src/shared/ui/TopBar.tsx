import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { Icon, type IconName } from './Icon';
import { Avatar } from './Avatar';
import { RoleBadge } from './RoleBadge';
import { useAuth } from '@/shared/auth/useAuth';

export interface NavLinkItem {
  to: string;
  label: string;
  icon?: IconName;
  count?: number;
  end?: boolean;
}

interface Props {
  nav: NavLinkItem[];
  showProfile?: boolean;
}

export function TopBar({ nav, showProfile = true }: Props) {
  const { user, logout, role } = useAuth();
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    function handler(e: MouseEvent) {
      if (open && wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const variant = role === 'Admin' ? 'adm' : role === 'Organizer' ? 'org' : 'plr';
  const displayName = user?.nickname || user?.organizerName || user?.email || '—';
  const profilePath =
    role === 'Player' ? '/profile' :
      role === 'Organizer' ? '/organizer/profile' :
        null;

  return (
    <header className="tp-topbar">
      <Link to="/" className="tp-brand">
        <span className="b1">Tournament</span><span className="b2">Platform</span>
      </Link>
      <div className="brand-sep" />
      <nav className="tp-nav">
        {nav.map((it) => (
          <NavLink
            key={it.to}
            to={it.to}
            end={it.end}
            className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
          >
            {it.icon && <Icon name={it.icon} size={14} />}
            <span>{it.label}</span>
            {it.count != null && <span className="count">{it.count}</span>}
          </NavLink>
        ))}
      </nav>
      <div className="spacer" />
      {showProfile && user && (
        <div className="user-wrap" ref={wrapRef}>
          <button
            type="button"
            className={`user-trigger ${open ? 'open' : ''}`}
            onClick={() => setOpen((o) => !o)}
          >
            <Avatar name={displayName} variant={variant} />
            <span style={{ color: 'var(--text)' }}>{displayName}</span>
            <Icon name="chevDown" size={12} style={{ color: 'var(--muted)' }} />
          </button>
          {open && (
            <div className="user-menu">
              <div className="user-menu-head">
                <Avatar name={displayName} size="lg" variant={variant} />
                <div className="col" style={{ gap: 4 }}>
                  <div style={{ fontWeight: 600, fontSize: 13.5 }}>{displayName}</div>
                  {role && <div className="row" style={{ gap: 6 }}><RoleBadge role={role} /></div>}
                </div>
              </div>
              {profilePath && (
                <div className="user-menu-list">
                  <button className="user-menu-item" onClick={() => { setOpen(false); navigate(profilePath); }}>
                    <Icon name="user" size={14} />
                    <span>Профиль</span>
                  </button>
                </div>
              )}
              <div className="user-menu-foot">
                <button
                  className="user-menu-item danger"
                  onClick={() => { setOpen(false); logout(); navigate('/login', { replace: true }); }}
                >
                  <Icon name="out" size={14} />
                  <span>Выйти из аккаунта</span>
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </header>
  );
}
