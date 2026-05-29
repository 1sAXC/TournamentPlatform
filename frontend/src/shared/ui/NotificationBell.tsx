import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMarkNotificationRead, useNotifications } from '@/features/notifications/hooks';
import { formatDateTime } from '@/shared/lib/formatters';
import { Icon } from './Icon';

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const { data, isLoading } = useNotifications();
  const markRead = useMarkNotificationRead();

  useEffect(() => {
    function handler(e: MouseEvent) {
      if (open && wrapRef.current && !wrapRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const unreadCount = data?.unreadCount ?? 0;
  const items = data?.items ?? [];

  function onClickItem(id: string, linkUrl: string | null, alreadyRead: boolean) {
    if (!alreadyRead) {
      markRead.mutate(id);
    }
    setOpen(false);
    if (linkUrl) {
      navigate(linkUrl);
    }
  }

  return (
    <div className="user-wrap" ref={wrapRef} style={{ marginRight: 8 }}>
      <button
        type="button"
        className={`user-trigger ${open ? 'open' : ''}`}
        onClick={() => setOpen((o) => !o)}
        aria-label="Уведомления"
        style={{ position: 'relative', padding: '6px 10px' }}
      >
        <Icon name="bell" size={16} />
        {unreadCount > 0 && (
          <span
            className="count"
            style={{
              position: 'absolute',
              top: 2,
              right: 2,
              minWidth: 16,
              height: 16,
              padding: '0 4px',
              borderRadius: 8,
              background: 'var(--danger)',
              color: 'white',
              fontSize: 10,
              lineHeight: '16px',
              textAlign: 'center',
              fontWeight: 600,
            }}
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="user-menu" style={{ width: 360, maxHeight: 480, overflowY: 'auto' }}>
          <div
            className="user-menu-head"
            style={{ flexDirection: 'column', alignItems: 'flex-start', gap: 2 }}
          >
            <div style={{ fontWeight: 600, fontSize: 13.5 }}>Уведомления</div>
            <div style={{ fontSize: 11.5, color: 'var(--muted)' }}>
              {unreadCount > 0 ? `${unreadCount} непрочитанных` : 'Все прочитано'}
            </div>
          </div>

          {isLoading ? (
            <div style={{ padding: 16, fontSize: 12, color: 'var(--muted)' }}>Загрузка…</div>
          ) : items.length === 0 ? (
            <div style={{ padding: 16, fontSize: 12, color: 'var(--muted)' }}>
              Здесь будут появляться уведомления о новых матчах и других событиях.
            </div>
          ) : (
            <div className="col" style={{ gap: 0 }}>
              {items.map((item) => {
                const unread = item.readAtUtc === null;
                return (
                  <button
                    key={item.id}
                    type="button"
                    className="user-menu-item"
                    onClick={() => onClickItem(item.id, item.linkUrl, !unread)}
                    style={{
                      display: 'block',
                      width: '100%',
                      textAlign: 'left',
                      padding: '10px 12px',
                      borderTop: '1px solid var(--border-soft, rgba(255,255,255,.04))',
                      background: unread ? 'var(--accent-soft)' : 'transparent',
                    }}
                  >
                    <div
                      className="row"
                      style={{ justifyContent: 'space-between', gap: 6, marginBottom: 4 }}
                    >
                      <span style={{ fontWeight: 600, fontSize: 12.5, flex: 1 }}>
                        {item.title}
                      </span>
                      {unread && (
                        <span
                          aria-hidden
                          style={{
                            width: 6,
                            height: 6,
                            borderRadius: '50%',
                            background: 'var(--accent)',
                            flexShrink: 0,
                            marginTop: 4,
                          }}
                        />
                      )}
                    </div>
                    <div style={{ fontSize: 11.5, color: 'var(--text-dim)', lineHeight: 1.5 }}>
                      {item.body}
                    </div>
                    <div
                      className="mono"
                      style={{ fontSize: 10.5, color: 'var(--muted)', marginTop: 4 }}
                    >
                      {formatDateTime(item.createdAtUtc)}
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
