import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { adminNav } from '@/features/navigation';
import {
  useAllTournaments, useCancelTournament,
} from '@/features/tournaments/hooks';
import { useAdminDeleteTournament } from '@/features/admin/hooks';
import { Badge } from '@/shared/ui/Badge';
import { Stat } from '@/shared/ui/Stat';
import { Icon } from '@/shared/ui/Icon';
import { EmptyState } from '@/shared/ui/EmptyState';
import { DISCIPLINES, STATUS_LABEL, STATUS_TONE, disciplineLabel } from '@/shared/lib/disciplines';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { AdminCreateTournamentModal } from './AdminCreateTournamentModal';
import { EditTournamentModal } from '@/features/tournaments/EditTournamentModal';
import type { TournamentListItemResponse } from '@/shared/api/types';

export function AdminTournamentsPage() {
  const { data, isLoading } = useAllTournaments();
  const cancel = useCancelTournament();
  const del = useAdminDeleteTournament();
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');
  const [discipline, setDiscipline] = useState('all');
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<TournamentListItemResponse | null>(null);

  const all = data ?? [];
  const counts = useMemo(() => ({
    total: all.length,
    open: all.filter(t => t.status === 'Open' || t.status === 'Full').length,
    progress: all.filter(t => t.status === 'InProgress').length,
    done: all.filter(t => t.status === 'Completed').length,
  }), [all]);

  // List all supported disciplines, not just those with at least one tournament
  // — otherwise a discipline disappears from the filter as soon as its last
  // tournament is removed.
  const disciplines = useMemo(() => DISCIPLINES.map(d => d.code), []);

  const filtered = all.filter(t => {
    if (status !== 'all' && t.status !== status) return false;
    if (discipline !== 'all' && t.disciplineCode !== discipline) return false;
    if (search && !t.title.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  function onCancel(id: string) {
    if (!confirm('Отменить турнир?')) return;
    cancel.mutate(id, {
      onSuccess: () => showToast('info', 'Турнир отменён'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }

  function onDelete(id: string) {
    if (!confirm('Полностью удалить турнир? Это действие необратимо.')) return;
    del.mutate(id, {
      onSuccess: () => showToast('info', 'Турнир удалён'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }

  return (
    <ScreenFrame nav={adminNav}>
      <div className="page-head">
        <div>
          <h1>Все турниры</h1>
          <div className="sub">Администрирование турниров платформы</div>
        </div>
        <div className="actions">
          <button className="btn btn-primary" onClick={() => setCreateOpen(true)}>
            <Icon name="plus" size={14} /> Создать турнир
          </button>
        </div>
      </div>

      {createOpen && <AdminCreateTournamentModal onClose={() => setCreateOpen(false)} />}

      <div className="stat-row" style={{ marginBottom: 18 }}>
        <Stat label="Всего" value={counts.total} />
        <Stat label="Открытых" value={counts.open} />
        <Stat label="Активных" value={counts.progress} delta={counts.progress > 0 ? { text: 'идут сейчас', dir: 'up' } : undefined} />
        <Stat label="Завершённых" value={counts.done} />
      </div>

      <div className="filter-bar">
        <div className="search" style={{ width: 260 }}>
          <input className="input" placeholder="Поиск…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="input select-sm" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="all">Все статусы</option>
          {Object.entries(STATUS_LABEL).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
        <select className="input select-sm" value={discipline} onChange={(e) => setDiscipline(e.target.value)}>
          <option value="all">Все дисциплины</option>
          {disciplines.map(d => <option key={d} value={d}>{disciplineLabel(d)}</option>)}
        </select>
      </div>

      <div className="card" style={{ padding: 0 }}>
        {isLoading ? <EmptyState title="Загрузка…" /> :
          filtered.length === 0 ? <EmptyState title="Ничего не найдено" /> : (
            <table className="tbl">
              <thead>
                <tr><th>Название</th><th>Дисциплина</th><th>Участники</th><th>Статус</th><th>Создан</th><th /></tr>
              </thead>
              <tbody>
                {filtered.map((t) => {
                  const tone = STATUS_TONE[t.status] ?? 'open';
                  const isActive = t.status === 'Open' || t.status === 'Full' || t.status === 'InProgress';
                  const isEditable = t.status === 'Open' || t.status === 'Full';
                  const isDeletable = t.status === 'Cancelled' || t.status === 'Completed';
                  return (
                    <tr key={t.id}>
                      <td className="strong" style={{ color: tone === 'done' || tone === 'cancelled' ? 'var(--muted)' : 'var(--accent)' }}>
                        {t.title}
                      </td>
                      <td><span className="t-tag">{disciplineLabel(t.disciplineCode)}</span></td>
                      <td className="mono">{t.currentPlayersCount} / {t.maxPlayers}</td>
                      <td><Badge tone={tone}>{STATUS_LABEL[t.status]}</Badge></td>
                      <td className="mono" style={{ fontSize: 11 }}>{formatDate(t.createdAtUtc)}</td>
                      <td style={{ textAlign: 'right' }}>
                        <div className="row" style={{ gap: 6, justifyContent: 'flex-end' }}>
                          <button className="btn btn-sm" onClick={() => navigate(`/tournaments/${t.id}`)}>
                            <Icon name="eye" size={11} /> Просмотр
                          </button>
                          {isActive && (
                            <button
                              className="btn btn-sm"
                              onClick={() => navigate(`/organizer/tournaments/${t.id}`)}
                              title="Завершить матчи, запустить следующий раунд"
                            >
                              <Icon name="trophy" size={11} /> Управление
                            </button>
                          )}
                          {isEditable && (
                            <button className="btn btn-sm" onClick={() => setEditing(t)}>
                              <Icon name="pen" size={11} /> Редактировать
                            </button>
                          )}
                          {isActive && (
                            <button className="btn btn-sm btn-danger" onClick={() => onCancel(t.id)} disabled={cancel.isPending}>
                              Отменить
                            </button>
                          )}
                          {isDeletable && (
                            <button className="btn btn-sm btn-ghost" onClick={() => onDelete(t.id)} disabled={del.isPending} aria-label="Удалить">
                              <Icon name="x" size={11} />
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

      {editing && (
        <EditTournamentModal
          tournamentId={editing.id}
          initialTitle={editing.title}
          initialDescription={editing.description ?? ''}
          onClose={() => setEditing(null)}
        />
      )}
    </ScreenFrame>
  );
}
