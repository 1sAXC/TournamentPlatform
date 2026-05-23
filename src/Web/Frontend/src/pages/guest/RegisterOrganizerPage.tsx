import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { AuthShell } from './AuthShell';
import { Field } from '@/shared/ui/Field';
import { Alert } from '@/shared/ui/Alert';
import { useRegisterOrganizerMutation } from '@/features/auth/hooks';
import { toApiError } from '@/shared/api/http';

const schema = z.object({
  organizerName: z.string().min(3, 'Минимум 3 символа').max(64, 'Максимум 64'),
  email: z.string().email('Некорректный e-mail'),
  password: z.string().min(8, 'Минимум 8 символов'),
  confirm: z.string(),
}).refine((d) => d.password === d.confirm, { path: ['confirm'], message: 'Пароли не совпадают' });
type FormValues = z.infer<typeof schema>;

export function RegisterOrganizerPage() {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { organizerName: '', email: '', password: '', confirm: '' },
  });
  const mutation = useRegisterOrganizerMutation();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const onSubmit = handleSubmit(({ organizerName, email, password }) => {
    setError(null);
    mutation.mutate({ organizerName, email, password }, {
      onSuccess: () => navigate('/', { replace: true }),
      onError: (err) => {
        const e = toApiError(err);
        setError(e.status === 409 ? 'Такое название организации или e-mail уже заняты' : e.title ?? 'Не удалось отправить заявку');
      },
    });
  });

  return (
    <AuthShell
      title="Регистрация организатора"
      sub="Заявка будет рассмотрена администратором"
      alert={
        <Alert kind="warn" icon="flag">
          После регистрации аккаунт активируется только после проверки администратором.
        </Alert>
      }
      footer={<>
        Хотите участвовать?{' '}
        <Link to="/register/player" className="link">Регистрация игрока</Link><br />
        Уже есть аккаунт? <Link to="/login" className="link">Войти</Link>
      </>}
    >
      {error && <Alert kind="error" icon="flag">{error}</Alert>}
      <form className="col" style={{ gap: 14 }} onSubmit={onSubmit}>
        <Field label="Название организации" error={errors.organizerName?.message}>
          <input className="input" {...register('organizerName')} />
        </Field>
        <Field label="E-mail" error={errors.email?.message}>
          <input className="input" type="email" {...register('email')} />
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
          {mutation.isPending ? 'Отправляем…' : 'Отправить заявку'}
        </button>
      </form>
    </AuthShell>
  );
}
