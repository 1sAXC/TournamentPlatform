import { useState } from 'react';
import type { MatchResponse, TeamResponse, TournamentDetailsResponse } from '@/shared/api/types';
import { Modal } from '@/shared/ui/Modal';
import { Field } from '@/shared/ui/Field';
import { Alert } from '@/shared/ui/Alert';
import { useCompleteMatch } from '@/features/tournaments/hooks';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';

interface Props {
  tournament: TournamentDetailsResponse;
  match: MatchResponse;
  onClose: () => void;
}

export function OrgMatchResultModal({ tournament, match, onClose }: Props) {
  const teamA = tournament.teams.find(t => t.id === match.teamAId);
  const teamB = tournament.teams.find(t => t.id === match.teamBId);
  const candidates = [teamA, teamB].filter(Boolean) as TeamResponse[];
  const [winnerId, setWinnerId] = useState<string>(teamA?.id ?? '');
  const [winnerScore, setWinnerScore] = useState<string>('2');
  const [loserScore, setLoserScore] = useState<string>('1');
  const [isTechnical, setIsTechnical] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const complete = useCompleteMatch(tournament.id);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!winnerId) { setError('Выберите победителя'); return; }
    const w = isTechnical ? null : Number(winnerScore);
    const l = isTechnical ? null : Number(loserScore);
    if (!isTechnical && (Number.isNaN(w!) || Number.isNaN(l!))) {
      setError('Введите числовой счёт');
      return;
    }
    complete.mutate({
      matchId: match.id,
      req: { winnerTeamId: winnerId, winnerScore: w, loserScore: l, isTechnicalDefeat: isTechnical },
    }, {
      onSuccess: () => { showToast('success', 'Результат сохранён'); onClose(); },
      onError: (err) => setError(toApiError(err).title ?? 'Не удалось сохранить результат'),
    });
  }

  const round = tournament.rounds.find(r => r.matches.some(m => m.id === match.id));

  return (
    <Modal
      onClose={onClose}
      eyebrow={`Матч #${match.matchNumber} · Раунд ${round?.number ?? '?'}`}
      title="Результат матча"
      footer={
        <>
          <button className="btn" onClick={onClose}>Отмена</button>
          <button className="btn btn-primary" onClick={onSubmit} disabled={complete.isPending}>
            {complete.isPending ? 'Сохраняем…' : 'Сохранить результат'}
          </button>
        </>
      }
    >
      <form className="col" style={{ gap: 14 }} onSubmit={onSubmit}>
        <Field label="Победитель">
          <select className="select" value={winnerId} onChange={(e) => setWinnerId(e.target.value)}>
            {candidates.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </Field>
        {!isTechnical && (
          <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <Field label="Счёт победителя">
              <input
                className="input" type="number" min={0}
                value={winnerScore} onChange={(e) => setWinnerScore(e.target.value)}
                style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
              />
            </Field>
            <Field label="Счёт проигравшего">
              <input
                className="input" type="number" min={0}
                value={loserScore} onChange={(e) => setLoserScore(e.target.value)}
                style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
              />
            </Field>
          </div>
        )}
        <label className="row" style={{ gap: 8, fontSize: 12.5, color: 'var(--text-dim)', cursor: 'pointer' }}>
          <span className={`check ${isTechnical ? 'on' : ''}`} onClick={() => setIsTechnical(v => !v)} />
          <span onClick={() => setIsTechnical(v => !v)}>Техническое поражение</span>
        </label>
        <Alert kind="warn" icon="flag">
          Результат необратим. Изменение возможно только через администратора.
        </Alert>
        {error && <div className="helper hint-error">{error}</div>}
      </form>
    </Modal>
  );
}
