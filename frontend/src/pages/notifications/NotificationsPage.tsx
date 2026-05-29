import { useNavigate } from 'react-router-dom';
import { useAllNotifications, useMarkNotificationRead } from '@/features/notifications/hooks';
import { adminNav, organizerNav, playerNav } from '@/features/navigation';
import { useAuth } from '@/shared/auth/useAuth';
import { Card } from '@/shared/ui/Card';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { formatDateTime } from '@/shared/lib/formatters';

export function NotificationsPage() {
  const { role } = useAuth();
  const navigate = useNavigate();
  const { data, isLoading } = useAllNotifications();
  const markRead = useMarkNotificationRead();

  const nav = role === 'Admin' ? adminNav : role === 'Organizer' ? organizerNav : playerNav;
  const items = data?.items ?? [];

  function onClickItem(id: string, linkUrl: string | null, alreadyRead: boolean) {
    if (!alreadyRead) {
      markRead.mutate(id);
    }
    if (linkUrl) {
      navigate(linkUrl);
    }
  }

  return (
    <ScreenFrame nav={nav}>
      <div className="page-head">
        <div>
          <h1>Уведомления</h1>
          <div className="sub">
            {isLoading
              ? 'Загрузка…'
              : data
                ? `Всего ${data.totalCount}, непрочитано ${data.unreadCount}`
                : 'Данные недоступны'}
          </div>
        </div>
      </div>

      <Card pad={false}>
        {isLoading ? (
          <EmptyState title="Загрузка…" />
        ) : items.length === 0 ? (
          <EmptyState title="Уведомлений пока нет">
            Когда появится новый матч или другое событие, оно появится здесь.
          </EmptyState>
        ) : (
          <div className="col" style={{ gap: 0 }}>
            {items.map((item) => {
              const unread = item.readAtUtc === null;
              return (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => onClickItem(item.id, item.linkUrl, !unread)}
                  style={{
                    display: 'block',
                    width: '100%',
                    textAlign: 'left',
                    padding: '14px 16px',
                    borderTop: '1px solid var(--border-soft, rgba(255,255,255,.04))',
                    background: unread ? 'var(--accent-soft)' : 'transparent',
                    cursor: 'pointer',
                    border: 0,
                    color: 'inherit',
                  }}
                >
                  <div className="row" style={{ justifyContent: 'space-between', gap: 8, marginBottom: 6 }}>
                    <span style={{ fontWeight: 600, fontSize: 13 }}>{item.title}</span>
                    <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>
                      {formatDateTime(item.createdAtUtc)}
                    </span>
                  </div>
                  <div style={{ fontSize: 12.5, color: 'var(--text-dim)', lineHeight: 1.5 }}>
                    {item.body}
                  </div>
                  {unread && (
                    <div style={{ marginTop: 6, fontSize: 11, color: 'var(--accent)' }}>● непрочитано</div>
                  )}
                </button>
              );
            })}
          </div>
        )}
      </Card>
    </ScreenFrame>
  );
}
