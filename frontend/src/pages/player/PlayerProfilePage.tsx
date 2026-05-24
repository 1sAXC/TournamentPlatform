import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { playerNav } from '@/features/navigation';
import { useAuth } from '@/shared/auth/useAuth';
import { usePlayerRatings, useRatingHistory } from '@/features/ratings/hooks';
import { useChangePasswordMutation } from '@/features/auth/hooks';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { RoleBadge } from '@/shared/ui/RoleBadge';
import { PStat } from '@/shared/ui/PStat';
import { Card } from '@/shared/ui/Card';
import { Field } from '@/shared/ui/Field';
import { EloChart } from '@/shared/ui/EloChart';
import { EmptyEloChart } from '@/shared/ui/EmptyEloChart';
import { disciplineLabel, DISCIPLINES } from '@/shared/lib/disciplines';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';

const pwdSchema = z.object({
  currentPassword: z.string().min(1, 'Введите текущий пароль'),
  newPassword: z.string().min(8, 'Минимум 8 символов'),
  confirm: z.string(),
}).refine((d) => d.newPassword === d.confirm, { path: ['confirm'], message: 'Пароли не совпадают' });
type PwdValues = z.infer<typeof pwdSchema>;

export function PlayerProfilePage() {
  const { user } = useAuth();
  const ratings = usePlayerRatings(user?.userId);
  const history = useRatingHistory(user?.userId);
  const changePwd = useChangePasswordMutation();
  const [pwdError, setPwdError] = useState<string | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<PwdValues>({
    resolver: zodResolver(pwdSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirm: '' },
  });

  const totalWins = (ratings.data ?? []).reduce((s, r) => s + r.wins, 0);
  const totalMatches = (ratings.data ?? []).reduce((s, r) => s + r.matchesPlayed, 0);
  const bestElo = (ratings.data ?? []).reduce((m, r) => Math.max(m, r.elo), 0);
  const winrate = totalMatches > 0 ? Math.round((totalWins / totalMatches) * 100) : 0;

  // Always render a card for every supported discipline. If the player has a
  // rating entry — show ELO. If they have history — render the chart.
  // Otherwise show the empty-axes placeholder with a "play first match" hint.
  const byDiscipline = useMemo(() => {
    return DISCIPLINES.map(({ code }) => {
      const rating = (ratings.data ?? []).find(r => r.disciplineCode === code);
      const events = (history.data ?? [])
        .filter(h => h.disciplineCode === code)
        .sort((a, b) => new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime());
      const data = events.map(h => h.newElo);
      const labels = events.map(h => formatDate(h.createdAtUtc));
      const delta = data.length >= 2 ? data[data.length - 1] - data[0] : 0;
      const trend: 'up' | 'down' | 'flat' = delta > 0 ? 'up' : delta < 0 ? 'down' : 'flat';
      return {
        code,
        current: rating?.elo ?? null,
        delta,
        trend,
        data,
        labels,
        hasHistory: data.length >= 2,
      };
    });
  }, [ratings.data, history.data]);

  const onSubmitPwd = handleSubmit(({ currentPassword, newPassword }) => {
    setPwdError(null);
    changePwd.mutate({ currentPassword, newPassword }, {
      onSuccess: () => { showToast('success', 'Пароль обновлён'); reset(); },
      onError: (err) => setPwdError(toApiError(err).title ?? 'Не удалось сменить пароль'),
    });
  });

  const displayName = user?.nickname ?? user?.email ?? '—';

  return (
    <ScreenFrame nav={playerNav}>
      <div className="page-head"><h1>Профиль</h1></div>

      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div className="row" style={{ gap: 18, marginBottom: 18 }}>
          <Avatar name={displayName} size="lg" variant="plr" />
          <div className="col" style={{ gap: 6, flex: 1 }}>
            <div style={{ fontSize: 20, fontWeight: 700 }}>{displayName}</div>
            <div className="row" style={{ gap: 8 }}>
              <RoleBadge role="Player" />
              {user?.accountStatus === 'Active' && <Badge tone="success">Активен</Badge>}
            </div>
            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{user?.email}</div>
          </div>
        </div>
        <div className="pstat-row">
          <PStat value={bestElo || '—'} label="Лучший ELO" tone="accent" />
          <PStat value={totalMatches} label="Матчей" />
          <PStat value={`${winrate}%`} label="Винрейт" tone="success" />
        </div>
      </div>

      <div className="profile-split">
        <Card title="Рейтинг по дисциплинам">
          <div className="col" style={{ gap: 10 }}>
            {byDiscipline.map((info) => (
              <div key={info.code} style={{ padding: 12, background: 'var(--surface-2)', borderRadius: 6 }}>
                <div className="row" style={{ justifyContent: 'space-between', marginBottom: 8 }}>
                  <div className="row" style={{ gap: 10 }}>
                    <span className="t-tag">{disciplineLabel(info.code)}</span>
                    <span className="mono" style={{ fontSize: 16, fontWeight: 600 }}>
                      {info.current ?? '—'}
                    </span>
                  </div>
                  {info.hasHistory ? (
                    <span style={{
                      fontSize: 11,
                      color: info.trend === 'up' ? 'var(--success)'
                        : info.trend === 'down' ? 'var(--danger)' : 'var(--muted)',
                    }}>
                      {info.trend === 'up' ? '↑ ' : info.trend === 'down' ? '↓ ' : '— '}
                      {info.delta >= 0 ? '+' : ''}{info.delta}
                    </span>
                  ) : (
                    <span style={{ fontSize: 11, color: 'var(--muted)' }}>нет матчей</span>
                  )}
                </div>
                {info.hasHistory ? (
                  <EloChart data={info.data} labels={info.labels} trend={info.trend} />
                ) : (
                  <EmptyEloChart message="Сыграйте первый матч, чтобы увидеть динамику рейтинга" />
                )}
              </div>
            ))}
          </div>
        </Card>

        <div className="col" style={{ gap: 14 }}>
          <Card title="Изменить пароль">
            <form className="col" style={{ gap: 12 }} onSubmit={onSubmitPwd}>
              <Field label="Текущий пароль" error={errors.currentPassword?.message}>
                <input className="input" type="password" autoComplete="current-password" {...register('currentPassword')} />
              </Field>
              <Field label="Новый пароль" hint="Минимум 8 символов" error={errors.newPassword?.message}>
                <input className="input" type="password" autoComplete="new-password" {...register('newPassword')} />
              </Field>
              <Field label="Повторите пароль" error={errors.confirm?.message}>
                <input className="input" type="password" autoComplete="new-password" {...register('confirm')} />
              </Field>
              {pwdError && <div className="helper hint-error">{pwdError}</div>}
              <div className="row" style={{ gap: 8, marginTop: 4 }}>
                <button className="btn btn-primary" disabled={changePwd.isPending}>
                  {changePwd.isPending ? 'Сохраняем…' : 'Сохранить пароль'}
                </button>
                <button type="button" className="btn" onClick={() => reset()}>Отмена</button>
              </div>
            </form>
          </Card>

          <Card title="Аккаунт">
            <div className="col" style={{ gap: 10, fontSize: 12.5 }}>
              <Row label="Никнейм" value={user?.nickname ?? '—'} />
              <Row label="E-mail" value={user?.email ?? '—'} />
              <Row label="Роль" value={<RoleBadge role="Player" />} />
              <Row label="Статус" value={<Badge tone={user?.accountStatus === 'Active' ? 'success' : 'pending'}>{user?.accountStatus ?? '—'}</Badge>} />
            </div>
          </Card>
        </div>
      </div>
    </ScreenFrame>
  );
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="row" style={{ justifyContent: 'space-between' }}>
      <span style={{ color: 'var(--muted)' }}>{label}</span>
      <span className="mono">{value}</span>
    </div>
  );
}
