import { Link } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { playerNav } from '@/features/navigation';
import { useMyTournaments, useUnregisterFromTournament } from '@/features/tournaments/hooks';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { STATUS_LABEL, STATUS_TONE, disciplineLabel, formatLabel } from '@/shared/lib/disciplines';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { Icon } from '@/shared/ui/Icon';

export function MyTournamentsPage() {
  const { data, isLoading } = useMyTournaments();
  const unregister = useUnregisterFromTournament();

  const all = data ?? [];
  const active = all.filter(t => t.status === 'InProgress' || t.status === 'RegistrationOpen');
  const completed = all.filter(t => t.status === 'Completed' || t.status === 'Cancelled');

  function leave(id: string) {
    if (!confirm('Покинуть турнир?')) return;
    unregister.mutate(id, {
      onSuccess: () => showToast('info', 'Вы покинули турнир'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }

  return (
    <ScreenFrame nav={playerNav}>
      <div className="page-head">
        <div>
          <h1>Мои турниры</h1>
          <div className="sub">Турниры, в которых вы участвуете</div>
        </div>
      </div>

      {isLoading ? <EmptyState title="Загрузка…" /> : null}

      {!isLoading && (
        <>
          <div className="row" style={{ gap: 8, marginBottom: 12 }}>
            <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'var(--success)' }} />
            <h3 style={{ fontSize: 13 }}>Активные</h3>
          </div>
          <div className="card" style={{ padding: 0, marginBottom: 24 }}>
            {active.length === 0 ? (
              <EmptyState title="Нет активных турниров">
                <Link to="/tournaments" className="link">Зарегистрируйтесь в каталоге</Link>
              </EmptyState>
            ) : (
              <table className="tbl">
                <thead>
                  <tr><th>Турнир</th><th>Дисциплина</th><th>Формат</th><th>Прогресс</th><th>Статус</th><th /></tr>
                </thead>
                <tbody>
                  {active.map((t) => {
                    const tone = STATUS_TONE[t.status] ?? 'open';
                    const pct = t.swissRounds && t.swissRounds > 0
                      ? Math.round((t.currentRoundNumber / t.swissRounds) * 100) : 0;
                    return (
                      <tr key={t.id}>
                        <td className="strong" style={{ color: 'var(--accent)' }}>{t.title}</td>
                        <td><span className="t-tag">{disciplineLabel(t.disciplineCode)}</span></td>
                        <td className="mono" style={{ color: 'var(--muted)', fontSize: 11 }}>
                          {formatLabel(t.format)} · {t.teamSize}v{t.teamSize}
                        </td>
                        <td>
                          <div style={{ fontSize: 12, marginBottom: 4 }}>
                            {t.status === 'InProgress' && t.swissRounds
                              ? `Раунд ${t.currentRoundNumber} / ${t.swissRounds}`
                              : 'Ожидание старта'}
                          </div>
                          <div className={`t-progress ${tone === 'progress' ? 'full' : ''}`} style={{ width: 120 }}>
                            <div style={{ width: `${pct}%` }} />
                          </div>
                        </td>
                        <td><Badge tone={tone}>{STATUS_LABEL[t.status]}</Badge></td>
                        <td style={{ textAlign: 'right' }}>
                          <div className="row" style={{ gap: 6, justifyContent: 'flex-end' }}>
                            <Link to={`/tournaments/${t.id}`} className="btn btn-sm">Просмотр</Link>
                            {t.status === 'RegistrationOpen' && (
                              <button className="btn btn-sm btn-danger" onClick={() => leave(t.id)} disabled={unregister.isPending}>
                                Покинуть
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>

          <div className="row" style={{ gap: 8, marginBottom: 12 }}>
            <Icon name="check" size={14} style={{ color: 'var(--muted)' }} />
            <h3 style={{ fontSize: 13 }}>Завершённые</h3>
          </div>
          <div className="card" style={{ padding: 0 }}>
            {completed.length === 0 ? (
              <EmptyState title="Пока пусто" />
            ) : (
              <table className="tbl">
                <thead>
                  <tr><th>Турнир</th><th>Дисциплина</th><th>Формат</th><th>Дата</th><th>Статус</th></tr>
                </thead>
                <tbody>
                  {completed.map((t) => (
                    <tr key={t.id}>
                      <td className="strong">{t.title}</td>
                      <td><span className="t-tag">{disciplineLabel(t.disciplineCode)}</span></td>
                      <td className="mono" style={{ color: 'var(--muted)', fontSize: 11 }}>{formatLabel(t.format)}</td>
                      <td className="mono">{formatDate(t.completedAtUtc ?? t.cancelledAtUtc)}</td>
                      <td><Badge tone={STATUS_TONE[t.status] ?? 'done'}>{STATUS_LABEL[t.status]}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </ScreenFrame>
  );
}
