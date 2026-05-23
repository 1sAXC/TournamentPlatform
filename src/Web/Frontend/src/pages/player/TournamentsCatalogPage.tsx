import { useMemo, useState } from 'react';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { playerNav, organizerNav, adminNav } from '@/features/navigation';
import { useAllTournaments } from '@/features/tournaments/hooks';
import { TournamentCard } from '@/shared/ui/TournamentCard';
import { EmptyState } from '@/shared/ui/EmptyState';
import { useAuth } from '@/shared/auth/useAuth';
import { DISCIPLINES, STATUS_LABEL, STATUS_TONE } from '@/shared/lib/disciplines';

const STATUSES: { value: string; label: string }[] = [
  { value: 'all', label: 'Все статусы' },
  { value: 'RegistrationOpen', label: STATUS_LABEL.RegistrationOpen },
  { value: 'InProgress', label: STATUS_LABEL.InProgress },
  { value: 'Completed', label: STATUS_LABEL.Completed },
];

export function TournamentsCatalogPage() {
  const { role } = useAuth();
  const { data, isLoading } = useAllTournaments();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [discipline, setDiscipline] = useState('all');

  const nav = role === 'Admin' ? adminNav : role === 'Organizer' ? organizerNav : playerNav;
  const tournaments = data ?? [];

  const disciplines = useMemo(() => {
    const set = new Set<string>();
    tournaments.forEach(t => set.add(t.disciplineCode));
    return Array.from(set);
  }, [tournaments]);

  const filtered = useMemo(() => {
    return tournaments.filter(t => {
      if (discipline !== 'all' && t.disciplineCode !== discipline) return false;
      if (statusFilter !== 'all' && t.status !== statusFilter) return false;
      if (search && !t.title.toLowerCase().includes(search.toLowerCase())) return false;
      return true;
    });
  }, [tournaments, discipline, statusFilter, search]);

  return (
    <ScreenFrame nav={nav}>
      <div className="page-head">
        <div>
          <h1>Турниры</h1>
          <div className="sub">Найдите турнир и зарегистрируйтесь для участия</div>
        </div>
      </div>

      <div className="game-tabs">
        <button className={`game-tab ${discipline === 'all' ? 'on' : ''}`} onClick={() => setDiscipline('all')}>Все</button>
        {DISCIPLINES.filter(d => disciplines.includes(d.code)).map((d) => (
          <button
            key={d.code}
            className={`game-tab ${discipline === d.code ? 'on' : ''}`}
            onClick={() => setDiscipline(d.code)}
          >{d.label}</button>
        ))}
      </div>

      <div className="filter-bar">
        <div className="search" style={{ width: 280 }}>
          <input className="input" placeholder="Поиск по названию…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="input select-sm" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          {STATUSES.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
        </select>
        <span className="spacer-x" />
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)' }}>
          {filtered.length} {filtered.length === 1 ? 'турнир' : 'турниров'}
        </span>
      </div>

      {isLoading ? (
        <EmptyState title="Загрузка…" />
      ) : filtered.length === 0 ? (
        <div className="card"><EmptyState title="Ничего не найдено">Попробуйте изменить фильтры</EmptyState></div>
      ) : (
        <div className="t-grid">
          {filtered.map((t) => {
            // sanity check that STATUS_TONE picks up status
            void STATUS_TONE[t.status];
            return <TournamentCard key={t.id} tournament={t} />;
          })}
        </div>
      )}
    </ScreenFrame>
  );
}
