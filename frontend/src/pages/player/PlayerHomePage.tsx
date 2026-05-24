import { Link } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { playerNav } from '@/features/navigation';
import { useMyTournaments } from '@/features/tournaments/hooks';
import { usePlayerRatings, useRatingHistory } from '@/features/ratings/hooks';
import { useAuth } from '@/shared/auth/useAuth';
import { Stat } from '@/shared/ui/Stat';
import { Icon } from '@/shared/ui/Icon';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { STATUS_LABEL, STATUS_TONE, disciplineLabel } from '@/shared/lib/disciplines';
import { formatDate } from '@/shared/lib/formatters';

export function PlayerHomePage() {
  const { user } = useAuth();
  const my = useMyTournaments();
  const ratings = usePlayerRatings(user?.userId);
  const history = useRatingHistory(user?.userId);

  const totalWins = (ratings.data ?? []).reduce((s, r) => s + r.wins, 0);
  const totalMatches = (ratings.data ?? []).reduce((s, r) => s + r.matchesPlayed, 0);
  const bestElo = (ratings.data ?? []).reduce((max, r) => Math.max(max, r.elo), 0);
  const bestDiscipline = (ratings.data ?? []).find(r => r.elo === bestElo)?.disciplineCode;
  const winrate = totalMatches > 0 ? Math.round((totalWins / totalMatches) * 100) : 0;
  const myActive = (my.data ?? []).filter(t => t.status === 'InProgress' || t.status === 'Open' || t.status === 'Full');

  return (
    <ScreenFrame nav={playerNav}>
      <div className="page-head">
        <div>
          <h1>Добро пожаловать, {user?.nickname ?? user?.email}</h1>
          <div className="sub">Ваш игровой дашборд</div>
        </div>
        <div className="actions">
          <Link to="/tournaments" className="btn btn-primary">
            <Icon name="trophy" size={14} /> Найти турнир
          </Link>
        </div>
      </div>

      <div className="stat-row" style={{ marginBottom: 22 }}>
        <Stat
          label="Лучший рейтинг ELO"
          value={bestElo || '—'}
          delta={bestDiscipline ? { text: `${disciplineLabel(bestDiscipline)} · лучший`, dir: 'up' } : undefined}
        />
        <Stat label="Побед" value={totalWins} delta={{ text: `из ${totalMatches} матчей` }} />
        <Stat label="Турниров" value={my.data?.length ?? 0} delta={{ text: 'всего' }} />
        <Stat
          label="Винрейт"
          value={`${winrate}%`}
          delta={{ text: 'за всё время', dir: winrate >= 50 ? 'up' : 'dn' }}
        />
      </div>

      <div className="row" style={{ justifyContent: 'space-between', marginBottom: 12 }}>
        <h3 style={{ fontSize: 14 }}>Мои активные турниры</h3>
        <Link to="/tournaments" className="btn btn-sm btn-ghost">Все турниры →</Link>
      </div>

      {myActive.length === 0 ? (
        <div className="card" style={{ marginBottom: 22 }}>
          <EmptyState title="Активных турниров нет">
            Загляните в каталог и зарегистрируйтесь
          </EmptyState>
        </div>
      ) : (
        <div className="grid" style={{ gridTemplateColumns: 'repeat(2,1fr)', gap: 12, marginBottom: 22 }}>
          {myActive.slice(0, 4).map((t) => {
            const tone = STATUS_TONE[t.status] ?? 'open';
            const progress = t.swissRounds && t.swissRounds > 0
              ? Math.round((t.currentRoundNumber / t.swissRounds) * 100)
              : 0;
            return (
              <Link to={`/tournaments/${t.id}`} key={t.id} className="card card-pad" style={{ textDecoration: 'none', color: 'inherit', display: 'block' }}>
                <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}>
                  <span className="t-tag">{disciplineLabel(t.disciplineCode)}</span>
                  <Badge tone={tone}>{STATUS_LABEL[t.status]}</Badge>
                </div>
                <div style={{ fontWeight: 600, marginBottom: 4 }}>{t.title}</div>
                <div className="mono" style={{ fontSize: 11, color: 'var(--muted)', marginBottom: 12 }}>
                  {t.status === 'InProgress' && t.swissRounds
                    ? `Раунд ${t.currentRoundNumber} / ${t.swissRounds}`
                    : 'Ожидание старта'}
                </div>
                <div className={`t-progress ${tone === 'progress' ? 'full' : ''}`}>
                  <div style={{ width: `${progress}%` }} />
                </div>
              </Link>
            );
          })}
        </div>
      )}

      <h3 style={{ fontSize: 14, marginBottom: 12 }}>Последние изменения рейтинга</h3>
      <div className="card" style={{ padding: 0 }}>
        {(history.data ?? []).length === 0 ? (
          <EmptyState title="Истории пока нет">
            Сыграйте матчи, чтобы увидеть динамику рейтинга
          </EmptyState>
        ) : (
          <table className="tbl">
            <thead>
              <tr>
                <th>Дисциплина</th>
                <th>Было</th>
                <th>Стало</th>
                <th>Δ ELO</th>
                <th>Дата</th>
              </tr>
            </thead>
            <tbody>
              {(history.data ?? []).slice(0, 8).map((h) => (
                <tr key={h.id}>
                  <td className="strong">{disciplineLabel(h.disciplineCode)}</td>
                  <td className="mono">{h.oldElo}</td>
                  <td className="mono">{h.newElo}</td>
                  <td className="mono" style={{ color: h.delta >= 0 ? 'var(--success)' : 'var(--danger)' }}>
                    {h.delta >= 0 ? '+' : ''}{h.delta}
                  </td>
                  <td className="mono">{formatDate(h.createdAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </ScreenFrame>
  );
}
