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
  // Score by maps (display): e.g. 2-1 for Bo3. Defaults reflect the most
  // common Bo3 outcome.
  const [winnerMaps, setWinnerMaps] = useState<string>('2');
  const [loserMaps, setLoserMaps] = useState<string>('1');
  // Score by rounds (sum across all maps). Feeds the Rating service's
  // margin-of-victory coefficient. Defaults assume two ~13-9 maps.
  const [winnerRounds, setWinnerRounds] = useState<string>('26');
  const [loserRounds, setLoserRounds] = useState<string>('20');
  const [isTechnical, setIsTechnical] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const complete = useCompleteMatch(tournament.id);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!winnerId) { setError('Выберите победителя'); return; }
    if (isTechnical) {
      complete.mutate({
        matchId: match.id,
        req: {
          winnerTeamId: winnerId,
          winnerScore: null, loserScore: null,
          winnerMaps: null, loserMaps: null,
          isTechnicalDefeat: true,
        },
      }, {
        onSuccess: () => { showToast('success', 'Результат сохранён'); onClose(); },
        onError: (err) => setError(toApiError(err).title ?? 'Не удалось сохранить результат'),
      });
      return;
    }

    const wMaps = Number(winnerMaps);
    const lMaps = Number(loserMaps);
    const wRounds = Number(winnerRounds);
    const lRounds = Number(loserRounds);
    if (Number.isNaN(wMaps) || Number.isNaN(lMaps) || Number.isNaN(wRounds) || Number.isNaN(lRounds)) {
      setError('Введите числовой счёт');
      return;
    }
    if (wMaps <= lMaps) {
      setError('У победителя должно быть больше выигранных карт');
      return;
    }
    if (wRounds <= lRounds) {
      setError('У победителя серии должно быть больше суммарных раундов');
      return;
    }
    complete.mutate({
      matchId: match.id,
      req: {
        winnerTeamId: winnerId,
        winnerScore: wRounds, loserScore: lRounds,
        winnerMaps: wMaps, loserMaps: lMaps,
        isTechnicalDefeat: false,
      },
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
          <>
            <Field label="Счёт по картам" hint="Например, 2-1 для Bo3. Отображается в карточке матча.">
              <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <input
                  className="input" type="number" min={0}
                  value={winnerMaps} onChange={(e) => setWinnerMaps(e.target.value)}
                  style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
                  aria-label="Карты победителя"
                />
                <input
                  className="input" type="number" min={0}
                  value={loserMaps} onChange={(e) => setLoserMaps(e.target.value)}
                  style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
                  aria-label="Карты проигравшего"
                />
              </div>
            </Field>
            <Field label="Счёт по раундам (сумма по всем картам)" hint="Используется для расчёта изменения ELO. Например, 13-9 + 13-11 = 26-20.">
              <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <input
                  className="input" type="number" min={0}
                  value={winnerRounds} onChange={(e) => setWinnerRounds(e.target.value)}
                  style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
                  aria-label="Раунды победителя"
                />
                <input
                  className="input" type="number" min={0}
                  value={loserRounds} onChange={(e) => setLoserRounds(e.target.value)}
                  style={{ fontSize: 20, fontFamily: 'var(--font-mono)', textAlign: 'center' }}
                  aria-label="Раунды проигравшего"
                />
              </div>
            </Field>
          </>
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
