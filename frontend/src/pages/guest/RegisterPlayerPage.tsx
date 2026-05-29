import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { AuthShell } from './AuthShell';
import { Field } from '@/shared/ui/Field';
import { Alert } from '@/shared/ui/Alert';
import { useRegisterPlayerMutation } from '@/features/auth/hooks';
import { toApiError } from '@/shared/api/http';

const schema = z.object({
  nickname: z.string().min(3, 'Минимум 3 символа').max(30, 'Максимум 30'),
  email: z.string().email('Некорректный e-mail'),
  contactHandle: z.string().min(1, 'Укажите контакт').max(64, 'Максимум 64 символа'),
  password: z.string()
    .min(8, 'Минимум 8 символов')
    .regex(/[A-Za-z]/, 'Нужна хотя бы одна латинская буква')
    .regex(/[0-9]/, 'Нужна хотя бы одна цифра'),
  confirm: z.string(),
}).refine((d) => d.password === d.confirm, { path: ['confirm'], message: 'Пароли не совпадают' });
type FormValues = z.infer<typeof schema>;

export function RegisterPlayerPage() {
  const { register, handleSubmit, setError: setFieldError, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { nickname: '', email: '', contactHandle: '', password: '', confirm: '' },
  });
  const mutation = useRegisterPlayerMutation();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const onSubmit = handleSubmit(({ nickname, email, contactHandle, password }) => {
    setError(null);
    mutation.mutate({ nickname, email, password, contactHandle: contactHandle.trim() }, {
      onSuccess: () => navigate('/', { replace: true }),
      onError: (err) => {
        const e = toApiError(err);
        if (e.code === 'Auth.DuplicateNickname') {
          setFieldError('nickname', { type: 'server', message: 'Этот никнейм уже занят' });
          return;
        }
        if (e.code === 'Auth.DuplicateEmail') {
          setFieldError('email', { type: 'server', message: 'Такой e-mail уже зарегистрирован' });
          return;
        }
        setError(e.title ?? 'Не удалось зарегистрироваться');
      },
    });
  });

  return (
    <AuthShell
      title="Регистрация игрока"
      sub="Создайте аккаунт для участия в турнирах"
      alert={error ? <Alert kind="error" icon="flag">{error}</Alert> : undefined}
      footer={<>
        Хотите проводить турниры?{' '}
        <Link to="/register/organizer" className="link">Регистрация организатора</Link><br />
        Уже есть аккаунт? <Link to="/login" className="link">Войти</Link>
      </>}
    >
      <form className="col" style={{ gap: 14 }} onSubmit={onSubmit}>
        <Field label="Никнейм" hint="Будет отображаться в турнирной таблице" error={errors.nickname?.message}>
          <input className="input" {...register('nickname')} />
        </Field>
        <Field label="E-mail" error={errors.email?.message}>
          <input className="input" type="email" {...register('email')} />
        </Field>
        <Field label="Контакт для связи" hint="Telegram, Discord и т.п. — увидят соперники в матчах" error={errors.contactHandle?.message}>
          <input className="input" placeholder="@your_handle" {...register('contactHandle')} />
        </Field>
        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Пароль" error={errors.password?.message}>
            <input className="input" type="password" autoComplete="new-password" {...register('password')} />
          </Field>
          <Field label="Повторите" error={errors.confirm?.message}>
            <input className="input" type="password" autoComplete="new-password" {...register('confirm')} />
          </Field>
        </div>
        <button className="btn btn-primary btn-lg btn-block" disabled={mutation.isPending}>
          {mutation.isPending ? 'Создаём…' : 'Создать аккаунт'}
        </button>
      </form>
    </AuthShell>
  );
}
