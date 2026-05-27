import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { adminNav } from '@/features/navigation';
import { useAuth } from '@/shared/auth/useAuth';
import { useChangePasswordMutation } from '@/features/auth/hooks';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { RoleBadge } from '@/shared/ui/RoleBadge';
import { Card } from '@/shared/ui/Card';
import { Field } from '@/shared/ui/Field';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { accountStatusLabel } from '@/shared/lib/disciplines';

const schema = z.object({
  currentPassword: z.string().min(1, 'Введите текущий пароль'),
  newPassword: z.string().min(8, 'Минимум 8 символов'),
  confirm: z.string(),
}).refine((d) => d.newPassword === d.confirm, { path: ['confirm'], message: 'Пароли не совпадают' });
type FormValues = z.infer<typeof schema>;

export function AdminProfilePage() {
  const { user } = useAuth();
  const changePwd = useChangePasswordMutation();
  const [pwdError, setPwdError] = useState<string | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirm: '' },
  });

  const displayName = user?.email ?? '—';

  const onSubmit = handleSubmit(({ currentPassword, newPassword }) => {
    setPwdError(null);
    changePwd.mutate({ currentPassword, newPassword }, {
      onSuccess: () => { showToast('success', 'Пароль обновлён'); reset(); },
      onError: (err) => setPwdError(toApiError(err).title ?? 'Не удалось сменить пароль'),
    });
  });

  return (
    <ScreenFrame nav={adminNav}>
      <div className="page-head"><h1>Профиль администратора</h1></div>

      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div className="row" style={{ gap: 18 }}>
          <Avatar name={displayName} size="lg" variant="adm" />
          <div className="col" style={{ gap: 6, flex: 1 }}>
            <div style={{ fontSize: 20, fontWeight: 700 }}>{displayName}</div>
            <div className="row" style={{ gap: 8 }}>
              <RoleBadge role="Admin" />
              <Badge tone={user?.accountStatus === 'Active' ? 'success' : 'pending'}>
                {accountStatusLabel(user?.accountStatus)}
              </Badge>
            </div>
            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{user?.email}</div>
          </div>
        </div>
      </div>

      <Card title="Изменить пароль" className="pwd-card">
        <form className="col" style={{ gap: 12 }} onSubmit={onSubmit}>
          <Field label="Текущий пароль" error={errors.currentPassword?.message}>
            <input className="input" type="password" autoComplete="current-password" {...register('currentPassword')} />
          </Field>
          <div className="pwd-fields">
            <Field label="Новый пароль" hint="Минимум 8 символов" error={errors.newPassword?.message}>
              <input className="input" type="password" autoComplete="new-password" {...register('newPassword')} />
            </Field>
            <Field label="Повторите пароль" error={errors.confirm?.message}>
              <input className="input" type="password" autoComplete="new-password" {...register('confirm')} />
            </Field>
          </div>
          {pwdError && <div className="helper hint-error">{pwdError}</div>}
          <div className="row" style={{ gap: 8, marginTop: 4 }}>
            <button className="btn btn-primary" disabled={changePwd.isPending}>
              {changePwd.isPending ? 'Сохраняем…' : 'Сохранить пароль'}
            </button>
            <button type="button" className="btn" onClick={() => reset()}>Отмена</button>
          </div>
        </form>
      </Card>
    </ScreenFrame>
  );
}
