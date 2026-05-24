import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { organizerNav } from '@/features/navigation';
import {
  useCancelTournament, useNextSwissRound, useTournament,
} from '@/features/tournaments/hooks';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Icon } from '@/shared/ui/Icon';
import { Card } from '@/shared/ui/Card';
import { TournamentBracket, type BracketRound } from '@/shared/ui/TournamentBracket';
import type { MatchResponse, TournamentDetailsResponse } from '@/shared/api/types';
import { STATUS_LABEL, STATUS_TONE, disciplineLabel, formatLabel } from '@/shared/lib/disciplines';
import { roundLabel } from '@/shared/lib/bracketLabels';
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

  if (isLoading || !data || !id) {
    return <ScreenFrame nav={organizerNav}><EmptyState title={isLoading ? 'Загрузка…' : 'Турнир не найден'} /></ScreenFrame>;
  }

  const tone = STATUS_TONE[data.status] ?? 'open';
  const currentRound = data.rounds.find(r => r.number === data.currentRoundNumber) ?? data.rounds[data.rounds.length - 1];
  const canManage = data.status === 'RegistrationOpen' || data.status === 'InProgress';
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
    if (canManage && m.status !== 'Completed' && m.teamAId && m.teamBId) setResultFor(m);
  });

  return (
    <ScreenFrame nav={organizerNav}>
      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div className="row" style={{ justifyContent: 'space-between' }}>
          <div>
            <h1 style={{ fontSize: 20, marginBottom: 6 }}>{data.title}</h1>
            <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
              <span className="t-tag">{disciplineLabel(data.disciplineCode)}</span>
              <Badge tone={tone}>{STATUS_LABEL[data.status]}</Badge>
              <span style={{ fontSize: 12, color: 'var(--muted)' }}>
                {formatLabel(data.format)} · {data.currentPlayersCount}/{data.maxPlayers}
                {data.swissRounds ? ` · Раунд ${data.currentRoundNumber} из ${data.swissRounds}` : ''}
              </span>
            </div>
          </div>
          <div className="row" style={{ gap: 8 }}>
            <button className="btn btn-sm" onClick={() => refetch()} disabled={isFetching}>
              {isFetching ? 'Обновляем…' : 'Обновить'}
            </button>
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
      </div>

      <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 16, alignItems: 'flex-start', marginBottom: 16 }}>
        <div>
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: 12 }}>
            <h3 style={{ fontSize: 14 }}>
              {currentRound ? `Раунд ${currentRound.number}` : 'Матчи'}
            </h3>
          </div>
          {!currentRound || currentRound.matches.length === 0 ? (
            <div className="card">
              <EmptyState title="Матчей пока нет">
                {data.status === 'RegistrationOpen' ? 'Дождитесь старта турнира' : 'После старта появятся матчи'}
              </EmptyState>
            </div>
          ) : (
            currentRound.matches.map((m) => {
              const teamA = data.teams.find(t => t.id === m.teamAId)?.name ?? '—';
              const teamB = data.teams.find(t => t.id === m.teamBId)?.name ?? '—';
              const done = m.status === 'Completed';
              return (
                <div key={m.id} className="match-card">
                  <div className="mc-head">
                    <span className="mc-id">M{currentRound.number}.{m.matchNumber}</span>
                    <Badge tone={done ? 'done' : 'pending'} />
                  </div>
                  <div className="mc-teams">
                    <span className={`mc-team ${done && m.winnerTeamId === m.teamAId ? 'win' : done ? 'loss' : ''}`}>{teamA}</span>
                    <span className={`mc-score ${done ? 'done' : ''}`}>
                      {done
                        ? (m.winnerTeamId === m.teamAId
                          ? `${m.winnerScore ?? '-'}–${m.loserScore ?? '-'}`
                          : `${m.loserScore ?? '-'}–${m.winnerScore ?? '-'}`)
                        : 'VS'}
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
            })
          )}
        </div>

        <div>
          <h3 style={{ fontSize: 14, marginBottom: 12 }}>Команды</h3>
          <Card pad={false}>
            {data.teams.length === 0 ? (
              <EmptyState title="Команд пока нет" />
            ) : (
              <table className="tbl">
                <thead><tr><th>Seed</th><th>Команда</th><th>Avg ELO</th><th>Игроки</th></tr></thead>
                <tbody>
                  {data.teams.map((t) => (
                    <tr key={t.id}>
                      <td className="mono">{t.seed}</td>
                      <td className="strong">{t.name}</td>
                      <td className="mono">{Math.round(t.averageElo)}</td>
                      <td className="mono" style={{ fontSize: 11 }}>{t.members.length}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>
        </div>
      </div>

      {bracketRounds.length > 0 && (
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
    </ScreenFrame>
  );
}

function buildBracketRounds(
  data: TournamentDetailsResponse,
  onMatchClick: (m: MatchResponse) => void,
): BracketRound[] {
  const totalRounds = data.rounds.length;
  return data.rounds.map((r) => ({
    label: roundLabel(data.format, r.number, totalRounds),
    current: r.number === data.currentRoundNumber && data.status === 'InProgress',
    matches: r.matches.map((m) => ({
      id: m.id,
      label: `M${r.number}.${m.matchNumber}`,
      a: data.teams.find(t => t.id === m.teamAId)?.name ?? null,
      b: data.teams.find(t => t.id === m.teamBId)?.name ?? null,
      sa: m.winnerTeamId === m.teamAId ? m.winnerScore ?? null
        : m.winnerTeamId === m.teamBId ? m.loserScore ?? null : null,
      sb: m.winnerTeamId === m.teamBId ? m.winnerScore ?? null
        : m.winnerTeamId === m.teamAId ? m.loserScore ?? null : null,
      win: m.winnerTeamId
        ? m.winnerTeamId === m.teamAId ? 'a' as const
          : m.winnerTeamId === m.teamBId ? 'b' as const : null
        : null,
      status: m.status === 'Completed' ? 'done' as const
        : m.teamAId && m.teamBId ? 'pending' as const
          : 'tbd' as const,
      onClick: m.status === 'Completed' ? undefined : () => onMatchClick(m),
    })),
  }));
}
