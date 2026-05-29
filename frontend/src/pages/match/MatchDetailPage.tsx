import { Link, useNavigate, useParams } from 'react-router-dom';
import { adminNav, organizerNav, playerNav } from '@/features/navigation';
import { useMatchDetails } from '@/features/tournaments/hooks';
import { useAuth } from '@/shared/auth/useAuth';
import { Alert } from '@/shared/ui/Alert';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Icon } from '@/shared/ui/Icon';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { disciplineLabel, formatLabel, STATUS_LABEL, STATUS_TONE } from '@/shared/lib/disciplines';
import { formatDateTime } from '@/shared/lib/formatters';
import type { MatchTeamDetails } from '@/shared/api/types';

const matchStatusLabel: Record<string, string> = {
  Pending: 'Не начат',
  InProgress: 'Идёт',
  Completed: 'Завершён',
  Cancelled: 'Отменён',
};

export function MatchDetailPage() {
  const { tournamentId, matchId } = useParams<{ tournamentId: string; matchId: string }>();
  const { role } = useAuth();
  const navigate = useNavigate();
  const { data, isLoading, isError } = useMatchDetails(tournamentId, matchId);

  const nav = role === 'Admin' ? adminNav : role === 'Organizer' ? organizerNav : playerNav;

  if (isLoading) {
    return <ScreenFrame nav={nav}><EmptyState title="Загрузка…" /></ScreenFrame>;
  }
  if (isError || !data) {
    return (
      <ScreenFrame nav={nav}>
        <EmptyState title="Матч не найден">
          Возможно, матч был удалён или ссылка устарела.
        </EmptyState>
      </ScreenFrame>
    );
  }

  const matchDone = data.matchStatus === 'Completed';
  // Maps for the headline score (e.g. "2 : 1"), rounds for the tooltip.
  const mapsA = matchDone
    ? (data.winnerTeamId === data.teamA?.id ? data.winnerMaps : data.loserMaps)
    : null;
  const mapsB = matchDone
    ? (data.winnerTeamId === data.teamB?.id ? data.winnerMaps : data.loserMaps)
    : null;
  const roundsA = matchDone
    ? (data.winnerTeamId === data.teamA?.id ? data.winnerScore : data.loserScore)
    : null;
  const roundsB = matchDone
    ? (data.winnerTeamId === data.teamB?.id ? data.winnerScore : data.loserScore)
    : null;

  const tournamentTone = STATUS_TONE[data.tournamentStatus as string] ?? 'open';

  return (
    <ScreenFrame nav={nav}>
      <div className="page-head">
        <div>
          <h1>Матч #{data.matchNumber} · Раунд {data.roundNumber}</h1>
          <div className="sub">
            <Link to={`/tournaments/${data.tournamentId}`} className="link">
              {data.tournamentTitle}
            </Link>
          </div>
        </div>
        <div className="actions">
          <button className="btn" onClick={() => navigate(`/tournaments/${data.tournamentId}`)}>
            ← К турниру
          </button>
        </div>
      </div>

      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div className="row" style={{ gap: 14, alignItems: 'flex-start' }}>
          <div style={{
            width: 56, height: 56, borderRadius: 8,
            background: 'var(--accent-soft)',
            display: 'grid', placeItems: 'center',
            color: 'var(--accent)',
          }}>
            <Icon name="trophy" size={26} />
          </div>
          <div style={{ flex: 1 }}>
            <h2 style={{ fontSize: 18, marginBottom: 6 }}>{data.tournamentTitle}</h2>
            <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
              <Badge tone={tournamentTone}>{STATUS_LABEL[data.tournamentStatus as string] ?? data.tournamentStatus}</Badge>
              <span className="t-tag">{disciplineLabel(data.disciplineCode)}</span>
              <span className="t-tag fmt">{formatLabel(data.format)}</span>
              <span style={{ fontSize: 12, color: 'var(--muted)' }}>
                · {data.teamSize > 1 ? `Командный ${data.teamSize}v${data.teamSize}` : 'Одиночный 1v1'}
              </span>
            </div>
          </div>
          <Badge tone={matchDone ? 'done' : 'pending'}>{matchStatusLabel[data.matchStatus] ?? data.matchStatus}</Badge>
        </div>
      </div>

      {data.tournamentDescription && (
        <Card title="Описание турнира" className="" >
          <div style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: 13 }}>
            {data.tournamentDescription}
          </div>
        </Card>
      )}

      <div className="grid" style={{ gridTemplateColumns: '1fr auto 1fr', gap: 16, alignItems: 'stretch', marginTop: 16 }}>
        <TeamCard team={data.teamA} mapScore={mapsA} canSeeContacts={data.canSeeContacts} />
        <div className="col" style={{ alignItems: 'center', justifyContent: 'center', minWidth: 90 }}>
          {matchDone ? (
            <>
              <div style={{ fontSize: 11, color: 'var(--muted)', marginBottom: 2 }}>по картам</div>
              <div style={{ fontSize: 24, fontWeight: 700, fontFamily: 'var(--font-mono)' }}>
                {mapsA ?? '–'} : {mapsB ?? '–'}
              </div>
              {(roundsA !== null || roundsB !== null) && (
                <div className="mono" style={{ fontSize: 11, color: 'var(--muted)', marginTop: 4 }}>
                  раунды {roundsA ?? '–'} : {roundsB ?? '–'}
                </div>
              )}
            </>
          ) : (
            <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--muted)' }}>VS</div>
          )}
          {matchDone && data.completedAtUtc && (
            <div className="mono" style={{ fontSize: 11, color: 'var(--muted)', marginTop: 6 }}>
              {formatDateTime(data.completedAtUtc)}
            </div>
          )}
        </div>
        <TeamCard team={data.teamB} mapScore={mapsB} canSeeContacts={data.canSeeContacts} />
      </div>

      <div style={{ marginTop: 16 }}>
        <Card title="Организатор турнира">
          <div className="row" style={{ gap: 12, alignItems: 'center' }}>
            <Avatar name={data.organizer.organizerName ?? data.tournamentTitle} variant="org" />
            <div className="col" style={{ gap: 2, flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 13 }}>
                {data.organizer.organizerName ?? 'Организатор турнира'}
              </div>
              {data.organizer.contactHandle ? (
                <div style={{ fontSize: 12.5, color: 'var(--text-dim)' }}>
                  Контакт: <span className="mono" style={{ color: 'var(--accent)' }}>{data.organizer.contactHandle}</span>
                </div>
              ) : (
                <div style={{ fontSize: 12, color: 'var(--muted)' }}>Контакт не указан</div>
              )}
            </div>
          </div>
        </Card>
      </div>

      {!data.canSeeContacts && (
        <div style={{ marginTop: 16 }}>
          <Alert kind="info" icon="flag">
            Контакты участников видны только игрокам, состоящим в одной из команд матча,
            организатору турнира и администратору. Чтобы согласовать матч, обратитесь к
            капитану своей команды или к организатору.
          </Alert>
        </div>
      )}
    </ScreenFrame>
  );
}

function TeamCard({
  team,
  mapScore,
  canSeeContacts,
}: {
  team: MatchTeamDetails | null;
  mapScore: number | null;
  canSeeContacts: boolean;
}) {
  if (!team) {
    return (
      <Card>
        <EmptyState title="Соперник не определён">
          Пара будет известна после завершения предыдущих матчей.
        </EmptyState>
      </Card>
    );
  }

  return (
    <Card title={team.name}>
      <div className="row" style={{ justifyContent: 'space-between', marginBottom: 10 }}>
        <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>
          Seed #{team.seed}
        </span>
        <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>
          Средний ELO: {Math.round(team.averageElo)}
        </span>
        {mapScore !== null && (
          <span className="mono" style={{ fontSize: 14, fontWeight: 700, color: 'var(--accent)' }}>
            {mapScore} карт
          </span>
        )}
      </div>
      <div className="col" style={{ gap: 6 }}>
        {team.members.map((m) => (
          <div
            key={m.playerId}
            className="row"
            style={{
              padding: '8px 10px',
              background: 'var(--surface-2)',
              borderRadius: 5,
              gap: 10,
              alignItems: 'flex-start',
            }}
          >
            <Avatar name={m.nickname} size="sm" variant="plr" />
            <div className="col" style={{ gap: 2, flex: 1 }}>
              <div className="row" style={{ gap: 6, alignItems: 'center' }}>
                <span style={{ fontWeight: 500, fontSize: 13 }}>{m.nickname}</span>
                {m.isCaptain && <Badge tone="success">Капитан</Badge>}
                <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>ELO {m.elo}</span>
              </div>
              {canSeeContacts ? (
                m.contactHandle ? (
                  <div style={{ fontSize: 11.5, color: 'var(--text-dim)' }}>
                    Контакт: <span className="mono" style={{ color: 'var(--accent)' }}>{m.contactHandle}</span>
                  </div>
                ) : (
                  <div style={{ fontSize: 11.5, color: 'var(--muted)' }}>Контакт не указан</div>
                )
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
