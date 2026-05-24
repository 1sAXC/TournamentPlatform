import { useMemo, useState } from 'react';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { adminNav } from '@/features/navigation';
import {
  useAdminUsers, useCreateAdminUser, useDeleteAdminUser, useResetUserPassword,
} from '@/features/admin/hooks';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { RoleBadge } from '@/shared/ui/RoleBadge';
import { Stat } from '@/shared/ui/Stat';
import { Icon } from '@/shared/ui/Icon';
import { Modal } from '@/shared/ui/Modal';
import { Field } from '@/shared/ui/Field';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const userSchema = z.object({
  role: z.enum(['Player', 'Organizer', 'Admin']),
  email: z.string().email('Некорректный e-mail'),
  password: z.string()
    .min(8, 'Минимум 8 символов')
    .regex(/[A-Za-z]/, 'Нужна хотя бы одна латинская буква')
    .regex(/[0-9]/, 'Нужна хотя бы одна цифра'),
  nickname: z.string().optional(),
  organizerName: z.string().optional(),
}).superRefine((d, ctx) => {
  if (d.role === 'Organizer') {
    if (!d.organizerName || d.organizerName.trim().length < 3) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['organizerName'], message: 'Минимум 3 символа' });
    }
  } else {
    if (!d.nickname || d.nickname.trim().length < 3) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['nickname'], message: 'Минимум 3 символа' });
    }
  }
});
type UserFormValues = z.infer<typeof userSchema>;

const ROLE_OPTS = [
  { value: 'all', label: 'Все роли' },
  { value: 'Player', label: 'Игрок' },
  { value: 'Organizer', label: 'Организатор' },
  { value: 'Admin', label: 'Администратор' },
];
const STATUS_OPTS = [
  { value: 'all', label: 'Все статусы' },
  { value: 'Active', label: 'Активен' },
  { value: 'PendingApproval', label: 'На проверке' },
  { value: 'Rejected', label: 'Отклонён' },
];

export function AdminUsersPage() {
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('all');
  const [status, setStatus] = useState('all');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const query = useMemo(() => ({
    pageNumber: page, pageSize: 20,
    role: role === 'all' ? undefined : role,
    status: status === 'all' ? undefined : status,
    search: search.trim() || undefined,
  }), [page, role, status, search]);

  const { data, isLoading } = useAdminUsers(query);
  const del = useDeleteAdminUser();
  const reset = useResetUserPassword();

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <ScreenFrame nav={adminNav}>
      <div className="page-head">
        <div>
          <h1>Пользователи</h1>
          <div className="sub">Управление аккаунтами платформы</div>
        </div>
        <div className="actions">
          <button className="btn btn-primary" onClick={() => setCreateOpen(true)}>
            <Icon name="plus" size={14} /> Создать пользователя
          </button>
        </div>
      </div>

      <div className="stat-row" style={{ marginBottom: 18 }}>
        <Stat label="Всего" value={data?.totalCount ?? '—'} />
        <Stat label="Игроков" value={items.filter(i => i.role === 'Player').length} />
        <Stat label="Организаторов" value={items.filter(i => i.role === 'Organizer').length} />
        <Stat label="Удалённых" value={items.filter(i => i.deletedAtUtc).length} />
      </div>

      <div className="filter-bar">
        <div className="search" style={{ width: 280 }}>
          <input
            className="input" placeholder="Поиск по никнейму или e-mail…"
            value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
        </div>
        <select className="input select-sm" value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }}>
          {ROLE_OPTS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <select className="input select-sm" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
          {STATUS_OPTS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>

      <div className="card" style={{ padding: 0 }}>
        {isLoading ? <EmptyState title="Загрузка…" /> :
          items.length === 0 ? <EmptyState title="Ничего не найдено" /> : (
            <table className="tbl">
              <thead>
                <tr><th>Пользователь</th><th>E-mail</th><th>Роль</th><th>Статус</th><th>Создан</th><th /></tr>
              </thead>
              <tbody>
                {items.map((u) => {
                  const name = u.nickname ?? u.organizerName ?? u.email;
                  const variant = u.role === 'Admin' ? 'adm' : u.role === 'Organizer' ? 'org' : 'plr';
                  const deleted = !!u.deletedAtUtc;
                  const statusTone =
                    u.status === 'Active' ? 'success'
                      : u.status === 'PendingApproval' ? 'pending'
                        : 'cancelled';
                  return (
                    <tr key={u.id}>
                      <td className="strong">
                        <div className="row" style={{ gap: 10 }}>
                          <Avatar name={name} size="sm" variant={variant} />
                          <span style={deleted ? { color: 'var(--muted)' } : {}}>{name}</span>
                        </div>
                      </td>
                      <td className="mono" style={{ fontSize: 11.5 }}>{u.email}</td>
                      <td><RoleBadge role={u.role} /></td>
                      <td>
                        <Badge tone={statusTone}>
                          {u.status === 'Active' ? 'Активен'
                            : u.status === 'PendingApproval' ? 'На проверке'
                              : u.status === 'Rejected' ? 'Отклонён'
                                : u.status}
                        </Badge>
                      </td>
                      <td className="mono">{formatDate(u.createdAtUtc)}</td>
                      <td style={{ textAlign: 'right' }}>
                        {deleted ? <span style={{ color: 'var(--muted-2)' }}>—</span> : (
                          <div className="row" style={{ gap: 6, justifyContent: 'flex-end' }}>
                            <button
                              className="btn btn-sm btn-ghost"
                              onClick={() => {
                                if (!confirm(`Сбросить пароль пользователя ${name}? Будет выдан временный пароль, который нужно сообщить пользователю.`)) return;
                                reset.mutate({ id: u.id }, {
                                  onSuccess: (r) => {
                                    showToast('success', r.temporaryPassword
                                      ? `Новый пароль: ${r.temporaryPassword}`
                                      : 'Пароль сброшен');
                                  },
                                  onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
                                });
                              }}
                              disabled={reset.isPending}
                            >Сброс пароля</button>
                            <button
                              className="btn btn-sm btn-danger"
                              onClick={() => {
                                if (!confirm(`Заблокировать пользователя ${name}? Он не сможет войти и зарегистрироваться на турниры.`)) return;
                                del.mutate(u.id, {
                                  onSuccess: () => showToast('info', 'Пользователь заблокирован'),
                                  onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
                                });
                              }}
                              disabled={del.isPending}
                            >Заблокировать</button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
      </div>

      {totalPages > 1 && (
        <div className="pagination">
          <button className="pg-btn" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>‹</button>
          <span className="pg-btn on">{page}</span>
          <span className="pg-btn">/ {totalPages}</span>
          <button className="pg-btn" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>›</button>
        </div>
      )}

      {createOpen && <CreateUserModal onClose={() => setCreateOpen(false)} />}
    </ScreenFrame>
  );
}

function CreateUserModal({ onClose }: { onClose: () => void }) {
  const create = useCreateAdminUser();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, watch, setValue, setError: setFieldError, formState: { errors } } = useForm<UserFormValues>({
    resolver: zodResolver(userSchema),
    defaultValues: { role: 'Player', email: '', password: '', nickname: '', organizerName: '' },
  });
  const role = watch('role');

  const onSubmit = handleSubmit((values) => {
    setError(null);
    const payload = {
      role: values.role,
      email: values.email,
      password: values.password,
      nickname: values.role === 'Organizer' ? null : values.nickname || null,
      organizerName: values.role === 'Organizer' ? values.organizerName || null : null,
    };
    create.mutate(payload, {
      onSuccess: () => { showToast('success', 'Пользователь создан'); onClose(); },
      onError: (err) => {
        const e = toApiError(err);
        if (e.code === 'Admin.DuplicateEmail' || e.code === 'Auth.DuplicateEmail') {
          setFieldError('email', { type: 'server', message: 'Такой e-mail уже зарегистрирован' });
          return;
        }
        if (e.code === 'Admin.DuplicateNickname' || e.code === 'Auth.DuplicateNickname') {
          const field = values.role === 'Organizer' ? 'organizerName' : 'nickname';
          setFieldError(field, { type: 'server', message: values.role === 'Organizer' ? 'Это название организации уже занято' : 'Этот никнейм уже занят' });
          return;
        }
        setError(e.title ?? 'Не удалось создать пользователя');
      },
    });
  });

  function generatePassword() {
    const arr = new Uint8Array(12);
    crypto.getRandomValues(arr);
    const pwd = Array.from(arr, b => 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!_-'[b % 64]).join('');
    setValue('password', pwd, { shouldValidate: true });
  }

  return (
    <Modal
      onClose={onClose}
      eyebrow="Администрирование"
      title="Создать пользователя"
      footer={<>
        <button className="btn" onClick={onClose}>Отмена</button>
        <button className="btn btn-primary" onClick={onSubmit} disabled={create.isPending}>
          {create.isPending ? 'Создаём…' : 'Создать аккаунт'}
        </button>
      </>}
    >
      <form className="col" style={{ gap: 14 }} onSubmit={onSubmit}>
        <Field label="Роль">
          <div className="radio-cards">
            {(['Player', 'Organizer', 'Admin'] as const).map(r => (
              <button
                type="button" key={r}
                className={`opt ${role === r ? 'sel' : ''}`}
                onClick={() => setValue('role', r, { shouldValidate: true })}
              >
                <span className="opt-title">
                  <Icon name={r === 'Admin' ? 'shield' : r === 'Organizer' ? 'trophy' : 'user'}
                    size={14} style={{ verticalAlign: -2, marginRight: 6 }} />
                  {r === 'Player' ? 'Игрок' : r === 'Organizer' ? 'Организатор' : 'Админ'}
                </span>
              </button>
            ))}
          </div>
        </Field>
        {role === 'Organizer' ? (
          <Field label="Название организации" error={errors.organizerName?.message}>
            <input className="input" {...register('organizerName')} />
          </Field>
        ) : (
          <Field label="Никнейм" hint="3–24 символа" error={errors.nickname?.message}>
            <input className="input" {...register('nickname')} />
          </Field>
        )}
        <Field label="E-mail" error={errors.email?.message}>
          <input className="input" type="email" {...register('email')} />
        </Field>
        <Field label="Пароль" error={errors.password?.message}>
          <div className="row" style={{ gap: 6 }}>
            <input className="input" type="text" style={{ flex: 1 }} {...register('password')} />
            <button type="button" className="btn" onClick={generatePassword}>Сгенерировать</button>
          </div>
        </Field>
        {error && <div className="helper hint-error">{error}</div>}
      </form>
    </Modal>
  );
}
