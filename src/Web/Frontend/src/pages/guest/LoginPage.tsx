import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useNavigate } from 'react-router-dom';
import { AuthShell } from './AuthShell';
import { Field } from '@/shared/ui/Field';
import { useLoginMutation } from '@/features/auth/hooks';
import { toApiError } from '@/shared/api/http';
import { Alert } from '@/shared/ui/Alert';
import { useState } from 'react';

const schema = z.object({
  login: z.string().min(1, 'Введите e-mail, никнейм или название'),
  password: z.string().min(1, 'Введите пароль'),
});
type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { login: '', password: '' },
  });
  const mutation = useLoginMutation();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const onSubmit = handleSubmit((data) => {
    setError(null);
    mutation.mutate(data, {
      onSuccess: () => navigate('/', { replace: true }),
      onError: (err) => {
        const e = toApiError(err);
        setError(e.status === 401 ? 'Неверный логин или пароль' : e.title ?? 'Не удалось войти');
      },
    });
  });

  return (
    <AuthShell
      title="Добро пожаловать"
      sub="Войдите в свой аккаунт"
      alert={error ? <Alert kind="error" icon="flag">{error}</Alert> : undefined}
      footer={<>
        Нет аккаунта?{' '}
        <Link to="/register/player" className="link">Зарегистрироваться</Link>
      </>}
    >
      <form className="col" style={{ gap: 14 }} onSubmit={onSubmit}>
        <Field label="E-mail / Никнейм / Название" error={errors.login?.message}>
          <input className="input" autoComplete="username" {...register('login')} />
        </Field>
        <Field label="Пароль" error={errors.password?.message}>
          <input className="input" type="password" autoComplete="current-password" {...register('password')} />
        </Field>
        <button className="btn btn-primary btn-lg btn-block" disabled={mutation.isPending}>
          {mutation.isPending ? 'Входим…' : 'Войти'}
        </button>
      </form>
    </AuthShell>
  );
}
