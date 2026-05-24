import { Link, useNavigate } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { organizerNav } from '@/features/navigation';
import { useCancelTournament, useOrganizerTournaments } from '@/features/tournaments/hooks';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { STATUS_LABEL, STATUS_TONE, disciplineLabel, formatLabel } from '@/shared/lib/disciplines';
import { Stat } from '@/shared/ui/Stat';
import { Icon } from '@/shared/ui/Icon';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';

export function OrgTournamentsPage() {
  const { data, isLoading } = useOrganizerTournaments();
  const cancel = useCancelTournament();
  const navigate = useNavigate();

  const all = data ?? [];
  const open = all.filter(t => t.status === 'Open' || t.status === 'Full').length;
  const active = all.filter(t => t.status === 'InProgress').length;
  const total = all.length;
  const participants = all.reduce((s, t) => s + t.currentPlayersCount, 0);

  function onCancel(id: string) {
    if (!confirm('Отменить турнир? Это действие необратимо.')) return;
    cancel.mutate(id, {
      onSuccess: () => showToast('info', 'Турнир отменён'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось отменить'),
    });
  }

  return (
    <ScreenFrame nav={organizerNav}>
      <div className="page-head">
        <div>
          <h1>Мои турниры</h1>
          <div className="sub">Управляйте своими турнирами</div>
        </div>
        <div className="actions">
          <Link to="/organizer/create" className="btn btn-primary">
            <Icon name="plus" size={14} /> Создать турнир
          </Link>
        </div>
      </div>

      <div className="stat-row" style={{ marginBottom: 18 }}>
        <Stat label="Всего" value={total} />
        <Stat label="Активных" value={active} delta={active > 0 ? { text: 'идут сейчас', dir: 'up' } : undefined} />
        <Stat label="Открытых" value={open} />
        <Stat label="Участников" value={participants} />
      </div>

      <div className="card" style={{ padding: 0 }}>
        {isLoading ? <EmptyState title="Загрузка…" /> :
          all.length === 0 ? (
            <EmptyState title="У вас ещё нет турниров">
              <Link to="/organizer/create" className="link">Создать первый</Link>
            </EmptyState>
          ) : (
            <table className="tbl">
              <thead>
                <tr><th>Название</th><th>Дисциплина</th><th>Формат</th><th>Участники</th><th>Статус</th><th /></tr>
              </thead>
              <tbody>
                {all.map((t) => {
                  const tone = STATUS_TONE[t.status] ?? 'open';
                  const done = t.status === 'Completed' || t.status === 'Cancelled';
                  return (
                    <tr key={t.id} onClick={() => navigate(`/organizer/tournaments/${t.id}`)} style={{ cursor: 'pointer' }}>
                      <td className="strong">
                        <div className="col" style={{ gap: 2 }}>
                          <span style={{ color: done ? 'var(--muted)' : 'var(--accent)' }}>{t.title}</span>
                        </div>
                      </td>
                      <td><span className="t-tag">{disciplineLabel(t.disciplineCode)}</span></td>
                      <td className="mono" style={{ fontSize: 11.5, color: 'var(--muted)' }}>
                        {formatLabel(t.format)} · {t.teamSize}v{t.teamSize}
                      </td>
                      <td className="mono">{t.currentPlayersCount} / {t.maxPlayers}</td>
                      <td><Badge tone={tone}>{STATUS_LABEL[t.status]}</Badge></td>
                      <td style={{ textAlign: 'right' }} onClick={(e) => e.stopPropagation()}>
                        {done ? (
                          <Link to={`/organizer/tournaments/${t.id}`} className="btn btn-sm btn-ghost">Просмотр</Link>
                        ) : (
                          <div className="row" style={{ gap: 6, justifyContent: 'flex-end' }}>
                            <Link
                              to={`/organizer/tournaments/${t.id}`}
                              className={`btn btn-sm ${t.status === 'InProgress' ? 'btn-primary' : ''}`}
                            >
                              Управлять
                            </Link>
                            <button className="btn btn-sm btn-danger" onClick={() => onCancel(t.id)} disabled={cancel.isPending}>
                              Отменить
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
      </div>
    </ScreenFrame>
  );
}
