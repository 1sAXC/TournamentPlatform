import { Link } from 'react-router-dom';
import type { TournamentListItemResponse } from '@/shared/api/types';
import { disciplineLabel, formatLabel, STATUS_LABEL, STATUS_TONE } from '@/shared/lib/disciplines';
import { Badge } from './Badge';

interface Props { tournament: TournamentListItemResponse; }

export function TournamentCard({ tournament: t }: Props) {
  const progress = t.maxPlayers > 0 ? (t.currentPlayersCount / t.maxPlayers) * 100 : 0;
  const tone = STATUS_TONE[t.status] ?? 'open';
  const progressClass =
    tone === 'progress' || tone === 'full' ? 'full'
      : tone === 'done' ? 'done'
        : tone === 'cancelled' ? 'canceled' : '';
  const teamLabel = t.teamSize > 1 ? `${t.teamSize}v${t.teamSize} · командный` : '1v1 · одиночный';

  return (
    <Link to={`/tournaments/${t.id}`} className="t-card">
      <div className="t-card-head">
        <div>
          <div className="t-card-name">{t.title}</div>
          <div className="t-card-org">{t.disciplineCode}</div>
        </div>
        <Badge tone={tone}>{STATUS_LABEL[t.status] ?? t.status}</Badge>
      </div>
      <div className="t-card-tags">
        <span className="t-tag">{disciplineLabel(t.disciplineCode)}</span>
        <span className="t-tag fmt">{formatLabel(t.format)}</span>
      </div>
      <div className="t-card-body">
        <div className="t-card-slots">{t.currentPlayersCount} / {t.maxPlayers} участников</div>
        <div className={`t-progress ${progressClass}`}>
          <div style={{ width: `${progress}%` }} />
        </div>
      </div>
      <div className="t-card-foot">
        <span style={{ fontSize: 11.5, color: 'var(--muted)', fontFamily: 'var(--font-mono)' }}>{teamLabel}</span>
        {t.status === 'InProgress' && t.currentRoundNumber > 0 && t.swissRounds
          ? <span style={{ fontSize: 11.5, color: 'var(--muted)' }}>Раунд {t.currentRoundNumber} / {t.swissRounds}</span>
          : <span style={{ fontSize: 11.5, color: 'var(--accent)' }}>Подробнее →</span>}
      </div>
    </Link>
  );
}
