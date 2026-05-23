import { useAuth } from '@/shared/auth/useAuth';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { Alert } from '@/shared/ui/Alert';
import { Card } from '@/shared/ui/Card';
import { useNavigate } from 'react-router-dom';
import { Avatar } from '@/shared/ui/Avatar';
import { RoleBadge } from '@/shared/ui/RoleBadge';

export function OrgPendingPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const displayName = user?.organizerName ?? user?.email ?? '—';

  return (
    <ScreenFrame nav={[{ to: '/organizer/profile', label: 'Профиль', icon: 'user' }]}>
      <div className="page-head"><h1>Заявка на рассмотрении</h1></div>

      <Alert kind="warn" icon="flag">
        Ваш аккаунт ожидает подтверждения администратором. После одобрения вы сможете создавать турниры.
      </Alert>

      <div style={{ maxWidth: 520 }}>
        <Card>
          <div className="row" style={{ gap: 14, marginBottom: 14 }}>
            <Avatar name={displayName} size="lg" variant="org" />
            <div className="col" style={{ gap: 4 }}>
              <div style={{ fontWeight: 600, fontSize: 15 }}>{displayName}</div>
              <div className="row" style={{ gap: 8 }}>
                <RoleBadge role="Organizer" />
              </div>
              <div style={{ fontSize: 12, color: 'var(--muted)' }}>{user?.email}</div>
            </div>
          </div>
          <div style={{ fontSize: 12.5, color: 'var(--text-dim)', lineHeight: 1.6 }}>
            Статус заявки: <span className="mono" style={{ color: 'var(--warning)' }}>{user?.accountStatus ?? 'PendingApproval'}</span>.
            Обычно рассмотрение занимает до 24 часов.
          </div>
          <div className="row" style={{ gap: 8, marginTop: 14 }}>
            <button className="btn btn-danger" onClick={() => { logout(); navigate('/login', { replace: true }); }}>
              Выйти из аккаунта
            </button>
          </div>
        </Card>
      </div>
    </ScreenFrame>
  );
}
