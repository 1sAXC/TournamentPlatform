import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { organizerNav } from '@/features/navigation';
import {
  useCancelTournament, useNextSwissRound, useTournament, useUpdateTournament,
} from '@/features/tournaments/hooks';
import { Alert } from '@/shared/ui/Alert';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Icon } from '@/shared/ui/Icon';
import { Card } from '@/shared/ui/Card';
import { Modal } from '@/shared/ui/Modal';
import { Field } from '@/shared/ui/Field';
import { PStat } from '@/shared/ui/PStat';
import { TournamentBracket } from '@/shared/ui/TournamentBracket';
import type { MatchResponse, TournamentDetailsResponse } from '@/shared/api/types';
import { STATUS_LABEL, STATUS_TONE, disciplineLabel, formatLabel } from '@/shared/lib/disciplines';
import { buildBracketRounds } from '@/shared/lib/bracket';
import { formatDate } from '@/shared/lib/formatters';
import { formatMatchScore } from '@/shared/lib/matchScore';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { OrgMatchResultModal } from './OrgMatchResultModal';

export function OrgManagePage() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, refetch, isFetching } = useTournament(id);
  const cancel = useCancelTournament();
  const nextRound = useNextSwissRound();
  const navigate = useNavigate();
  const [resultFor, setResultFor] = useState<MatchResponse | null>(null);
  const [editing, setEditing] = useState(false);

  if (isLoading || !data || !id) {
    return <ScreenFrame nav={organizerNav}><EmptyState title={isLoading ? 'Загрузка…' : 'Турнир не найден'} /></ScreenFrame>;
  }

  const tone = STATUS_TONE[data.status] ?? 'open';
  const currentRound = data.rounds.find(r => r.number === data.currentRoundNumber) ?? data.rounds[data.rounds.length - 1];
  const canManage = data.status === 'Open' || data.status === 'Full' || data.status === 'InProgress';
  const canEdit = data.status === 'Open' || data.status === 'Full';
  const canStartNextSwiss = data.format === 'Swiss'
    && data.status === 'InProgress'
    && currentRound
    && currentRound.matches.every(m => m.status === 'Completed');

  function onCancel() {
    if (!confirm('Отменить турнир?')) return;
    cancel.mutate(id!, {
      onSuccess: () => { showToast('info', 'Турнир отменён'); navigate('/organizer'); },
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }
  function onNextRound() {
    nextRound.mutate(id!, {
      onSuccess: () => showToast('success', 'Создан следующий раунд'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }

  const bracketRounds = buildBracketRounds(data, (m) => {
    // For organizer: clicking an active match opens the result modal;
    // clicking a completed match navigates to the public match page so
    // the organizer can see contacts and rosters without leaving the
    // tournament context.
    if (canManage && m.status !== 'Completed' && m.teamAId && m.teamBId) {
      setResultFor(m);
    } else if (m.status === 'Completed' && id) {
      navigate(`/tournaments/${id}/matches/${m.id}`);
    }
  });

  const standings = computeStandings(data);
  const activeParticipants = data.participants.filter(p => p.isActive);

  return (
    <ScreenFrame nav={organizerNav}>
      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div className="row" style={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div className="row" style={{ gap: 14, alignItems: 'flex-start' }}>
            <div style={{
              width: 56, height: 56, borderRadius: 8,
              background: 'var(--accent-soft)',
              display: 'grid', placeItems: 'center',
              color: 'var(--accent)',
            }}>
              <Icon name="trophy" size={26} />
            </div>
            <div>
              <h1 style={{ fontSize: 22, marginBottom: 6 }}>{data.title}</h1>
              <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
                <Badge tone={tone}>{STATUS_LABEL[data.status]}</Badge>
                <span className="t-tag">{disciplineLabel(data.disciplineCode)}</span>
                <span className="t-tag fmt">{formatLabel(data.format)}</span>
                <span style={{ fontSize: 12, color: 'var(--muted)' }}>
                  · {data.teamSize > 1 ? `Командный ${data.teamSize}v${data.teamSize}` : 'Одиночный 1v1'}
                  {data.swissRounds ? ` · Раунд ${data.currentRoundNumber} из ${data.swissRounds}` : ''}
                </span>
              </div>
            </div>
          </div>
          <div className="row" style={{ gap: 8 }}>
            <button className="btn btn-sm" onClick={() => refetch()} disabled={isFetching}>
              {isFetching ? 'Обновляем…' : 'Обновить'}
            </button>
            {canEdit && (
              <button className="btn" onClick={() => setEditing(true)}>
                <Icon name="pen" size={13} /> Редактировать
              </button>
            )}
            {canStartNextSwiss && (
              <button className="btn btn-primary" onClick={onNextRound} disabled={nextRound.isPending}>
                <Icon name="check" size={13} /> Следующий раунд
              </button>
            )}
            {canManage && (
              <button className="btn btn-danger" onClick={onCancel} disabled={cancel.isPending}>
                Отменить турнир
              </button>
            )}
          </div>
        </div>
        <div className="pstat-row" style={{ marginTop: 18 }}>
          <PStat
            value={`${data.currentPlayersCount}/${data.maxPlayers}`}
            label="Участников"
            tone="accent"
          />
          <PStat value={data.rounds.length || data.swissRounds || '—'} label="Раундов" />
          <PStat
            value={data.startedAtUtc ? formatDate(data.startedAtUtc) : '—'}
            label={data.startedAtUtc ? 'Старт' : 'Регистрация'}
            tone={data.startedAtUtc ? 'warning' : undefined}
          />
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 16, alignItems: 'flex-start', marginBottom: 16 }}>
        <div className="col" style={{ gap: 14 }}>
          {data.description && (
            <Card title="Описание">
              <div style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: 13 }}>
                {data.description}
              </div>
            </Card>
          )}

          <Card title={`Участники (${data.currentPlayersCount} / ${data.maxPlayers})`}>
            {activeParticipants.length === 0 ? (
              <EmptyState title="Пока никого нет">Дождитесь регистраций</EmptyState>
            ) : (
              <div className="col" style={{ gap: 6 }}>
                {activeParticipants.map((p) => (
                  <div key={p.id} className="row" style={{ padding: '8px 10px', background: 'var(--surface-2)', borderRadius: 5, gap: 10 }}>
                    <Avatar name={p.playerNickname} size="sm" variant="plr" />
                    <span style={{ fontWeight: 500, fontSize: 13, flex: 1 }}>{p.playerNickname}</span>
                    <Badge tone="success">Принят</Badge>
                  </div>
                ))}
              </div>
            )}
          </Card>

          <Card title={`Команды (${data.teams.length})`}>
            {data.teams.length === 0 ? (
              <EmptyState title="Команд пока нет">Команды появятся после старта турнира</EmptyState>
            ) : (
              <div className="col" style={{ gap: 8 }}>
                {data.teams.map((team) => (
                  <div key={team.id} style={{ padding: 10, background: 'var(--surface-2)', borderRadius: 5 }}>
                    <div className="row" style={{ justifyContent: 'space-between', marginBottom: 6 }}>
                      <div style={{ fontWeight: 600, fontSize: 13 }}>
                        <span className="mono" style={{ color: 'var(--muted)', marginRight: 6 }}>#{team.seed}</span>
                        {team.name}
                      </div>
                      <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>
                        Средний ELO: {Math.round(team.averageElo)}
                      </span>
                    </div>
                    <div className="row" style={{ flexWrap: 'wrap', gap: 6 }}>
                      {team.members.map((m) => (
                        <span key={m.playerId} className="t-tag">{m.nickname} · {m.elo}</span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>

        <div className="col" style={{ gap: 14 }}>
          <Card title="Турнирная таблица" pad={false}>
            {standings.length === 0 ? (
              <div style={{ padding: '12px 14px' }}>
                <Alert kind="info" icon="cal">Турнир ещё не начался. Таблица появится после старта.</Alert>
              </div>
            ) : (
              <table className="tbl">
                <thead>
                  <tr><th>#</th><th>Команда</th><th>В</th><th>П</th><th>Раунды</th></tr>
                </thead>
                <tbody>
                  {standings.map((s, i) => (
                    <tr key={s.teamId}>
                      <td className="mono" style={{ color: i === 0 ? 'var(--accent)' : 'var(--muted)' }}>{i + 1}</td>
                      <td className="strong">{s.name}</td>
                      <td className="mono" style={{ color: 'var(--success)' }}>{s.wins}</td>
                      <td className="mono">{s.losses}</td>
                      <td className="mono">{s.roundsFor}–{s.roundsAgainst}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>

          <Card title={currentRound ? `Текущий раунд · #${currentRound.number}` : 'Матчи'}>
            {!currentRound || currentRound.matches.length === 0 ? (
              <EmptyState title="Матчей пока нет">
                {data.status === 'Open' || data.status === 'Full' ? 'Дождитесь старта турнира' : 'После старта появятся матчи'}
              </EmptyState>
            ) : (
              <div className="col" style={{ gap: 8 }}>
                {currentRound.matches.map((m) => {
                  const teamA = data.teams.find(t => t.id === m.teamAId)?.name ?? '—';
                  const teamB = data.teams.find(t => t.id === m.teamBId)?.name ?? '—';
                  const done = m.status === 'Completed';
                  return (
                    <div key={m.id} className="match-card" style={{ marginBottom: 0 }}>
                      <div className="mc-head">
                        <span className="mc-id">M{currentRound.number}.{m.matchNumber}</span>
                        <Badge tone={done ? 'done' : 'pending'} />
                      </div>
                      <div className="mc-teams">
                        <span className={`mc-team ${done && m.winnerTeamId === m.teamAId ? 'win' : done ? 'loss' : ''}`}>{teamA}</span>
                        <span className={`mc-score ${done ? 'done' : ''}`} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
                          {done ? (
                            <>
                              <span>{formatMatchScore(m).primary}</span>
                              {formatMatchScore(m).secondary && (
                                <span className="mono" style={{ fontSize: 10, color: 'var(--muted)' }}>
                                  раунды {formatMatchScore(m).secondary}
                                </span>
                              )}
                            </>
                          ) : 'VS'}
                        </span>
                        <span className={`mc-team ${done && m.winnerTeamId === m.teamBId ? 'win' : done ? 'loss' : ''}`}>{teamB}</span>
                      </div>
                      {!done && canManage && m.teamAId && m.teamBId && (
                        <button className="btn btn-sm btn-primary" style={{ width: '100%' }} onClick={() => setResultFor(m)}>
                          Внести результат
                        </button>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </Card>
        </div>
      </div>

      {data.format !== 'Swiss' && bracketRounds.length > 0 && (
        <>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 12 }}>
            <h3 style={{ fontSize: 14 }}>Турнирная сетка</h3>
            <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>
              {data.teams.length} команд · {data.rounds.length} раундов · {formatLabel(data.format)}
            </span>
          </div>
          <TournamentBracket rounds={bracketRounds} />
        </>
      )}

      {resultFor && (
        <OrgMatchResultModal
          tournament={data}
          match={resultFor}
          onClose={() => setResultFor(null)}
        />
      )}

      {editing && (
        <EditTournamentModal
          tournamentId={id}
          initialTitle={data.title}
          initialDescription={data.description ?? ''}
          onClose={() => setEditing(false)}
        />
      )}
    </ScreenFrame>
  );
}

function EditTournamentModal({
  tournamentId, initialTitle, initialDescription, onClose,
}: {
  tournamentId: string;
  initialTitle: string;
  initialDescription: string;
  onClose: () => void;
}) {
  const update = useUpdateTournament(tournamentId);
  const [title, setTitle] = useState(initialTitle);
  const [description, setDescription] = useState(initialDescription);
  const [error, setError] = useState<string | null>(null);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!title.trim()) { setError('Введите название турнира'); return; }
    update.mutate(
      { title: title.trim(), description: description.trim() || null },
      {
        onSuccess: () => { showToast('success', 'Турнир обновлён'); onClose(); },
        onError: (err) => setError(toApiError(err).title ?? 'Не удалось сохранить'),
      },
    );
  }

  return (
    <Modal
      onClose={onClose}
      title="Редактировать турнир"
      footer={<>
        <button className="btn" onClick={onClose}>Отмена</button>
        <button className="btn btn-primary" onClick={onSubmit} disabled={update.isPending}>
          {update.isPending ? 'Сохраняем…' : 'Сохранить'}
        </button>
      </>}
    >
      <form className="col" style={{ gap: 12 }} onSubmit={onSubmit}>
        <Field label="Название турнира">
          <input className="input" value={title} onChange={(e) => setTitle(e.target.value)} />
        </Field>
        <Field label="Описание">
          <textarea
            className="textarea" value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </Field>
        {error && <div className="helper hint-error">{error}</div>}
      </form>
    </Modal>
  );
}

interface StandingRow {
  teamId: string;
  name: string;
  wins: number;
  losses: number;
  roundsFor: number;
  roundsAgainst: number;
}

function computeStandings(data: TournamentDetailsResponse): StandingRow[] {
  const map = new Map<string, StandingRow>();
  data.teams.forEach((t) => map.set(t.id, { teamId: t.id, name: t.name, wins: 0, losses: 0, roundsFor: 0, roundsAgainst: 0 }));
  data.rounds.forEach((r) => r.matches.forEach((m) => {
    if (m.status !== 'Completed' || !m.winnerTeamId || !m.loserTeamId) return;
    const w = map.get(m.winnerTeamId);
    const l = map.get(m.loserTeamId);
    if (w) { w.wins += 1; w.roundsFor += m.winnerScore ?? 0; w.roundsAgainst += m.loserScore ?? 0; }
    if (l) { l.losses += 1; l.roundsFor += m.loserScore ?? 0; l.roundsAgainst += m.winnerScore ?? 0; }
  }));
  return Array.from(map.values()).sort((a, b) => b.wins - a.wins || (b.roundsFor - b.roundsAgainst) - (a.roundsFor - a.roundsAgainst));
}
