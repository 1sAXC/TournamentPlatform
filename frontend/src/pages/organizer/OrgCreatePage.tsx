import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { organizerNav } from '@/features/navigation';
import { Field } from '@/shared/ui/Field';
import { Icon } from '@/shared/ui/Icon';
import { DISCIPLINES } from '@/shared/lib/disciplines';
import { useCreateTournament } from '@/features/tournaments/hooks';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';

const schema = z.object({
  title: z.string().min(5, 'Минимум 5 символов').max(50, 'Максимум 50 символов'),
  description: z.string().max(150, 'Максимум 150 символов').optional(),
  disciplineCode: z.string().min(1, 'Выберите дисциплину'),
  format: z.enum(['Swiss', 'SingleElimination', 'DoubleElimination']),
  swissRounds: z.coerce
    .number({ invalid_type_error: 'Введите число раундов' })
    .int('Должно быть целым числом')
    .min(1, 'Минимум 1 раунд')
    .max(20, 'Максимум 20 раундов')
    .optional(),
  teamSize: z.coerce
    .number({ invalid_type_error: 'Введите размер команды' })
    .int('Должно быть целым числом')
    .refine((v) => v === 1 || v === 2 || v === 5, 'Размер команды должен быть 1, 2 или 5'),
  maxPlayers: z.coerce
    .number({ invalid_type_error: 'Введите число участников' })
    .int('Должно быть целым числом')
    .min(2, 'Минимум 2 участника')
    .max(120, 'Максимум 120 участников'),
});
type FormValues = z.infer<typeof schema>;

const FORMAT_OPTIONS: { code: FormValues['format']; title: string; sub: string; icon: 'swap' | 'flag' | 'trophy' }[] = [
  { code: 'Swiss', title: 'Швейцарская', sub: 'все играют одинаковое число туров', icon: 'swap' },
  { code: 'DoubleElimination', title: 'Double Elim', sub: 'до двух поражений', icon: 'flag' },
  { code: 'SingleElimination', title: 'Single Elim', sub: 'плей-офф навылет', icon: 'trophy' },
];

export function OrgCreatePage() {
  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '', description: '', disciplineCode: 'CS2',
      format: 'Swiss', swissRounds: 5, teamSize: 5, maxPlayers: 16,
    },
  });
  const create = useCreateTournament();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const format = watch('format');

  const onSubmit = handleSubmit((values) => {
    setError(null);
    const payload = {
      ...values,
      swissRounds: values.format === 'Swiss' ? (values.swissRounds ?? 5) : null,
      description: values.description?.trim() || null,
    };
    create.mutate(payload, {
      onSuccess: (data) => { showToast('success', 'Турнир создан'); navigate(`/organizer/tournaments/${data.id}`); },
      onError: (err) => setError(toApiError(err).title ?? 'Не удалось создать турнир'),
    });
  });

  return (
    <ScreenFrame nav={organizerNav}>
      <div className="page-head">
        <div>
          <h1>Создать турнир</h1>
          <div className="sub">Заполните параметры нового турнира</div>
        </div>
      </div>

      <form className="card card-pad" style={{ maxWidth: 820 }} onSubmit={onSubmit}>
        <div className="eyebrow" style={{ marginBottom: 12 }}>Основная информация</div>
        <div className="col" style={{ gap: 14 }}>
          <Field label="Название турнира" error={errors.title?.message}>
            <input className="input" {...register('title')} />
          </Field>
          <Field label="Описание" error={errors.description?.message}>
            <textarea className="textarea" {...register('description')} placeholder="Условия участия, призы, формат…" />
          </Field>
          <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <Field label="Дисциплина" error={errors.disciplineCode?.message}>
              <select className="select" {...register('disciplineCode')}>
                {DISCIPLINES.map(d => <option key={d.code} value={d.code}>{d.label}</option>)}
              </select>
            </Field>
            <Field label="Макс. участников" error={errors.maxPlayers?.message}>
              <input className="input" type="number" {...register('maxPlayers')} />
            </Field>
          </div>
        </div>

        <div className="divider" />

        <div className="eyebrow" style={{ marginBottom: 12 }}>Формат</div>
        <Field label="Система проведения">
          <div className="radio-cards">
            {FORMAT_OPTIONS.map((opt) => (
              <button
                type="button"
                key={opt.code}
                className={`opt ${format === opt.code ? 'sel' : ''}`}
                onClick={() => setValue('format', opt.code, { shouldValidate: true })}
              >
                <span className="opt-title"><Icon name={opt.icon} size={14} style={{ verticalAlign: -2, marginRight: 6 }} />{opt.title}</span>
                <span className="opt-sub">{opt.sub}</span>
              </button>
            ))}
          </div>
        </Field>
        {format === 'Swiss' && (
          <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12, marginTop: 12 }}>
            <Field label="Количество раундов" hint="От 1 до 20" error={errors.swissRounds?.message}>
              <input className="input" type="number" min={1} max={20} {...register('swissRounds')} />
            </Field>
          </div>
        )}

        <div className="divider" />

        <div className="eyebrow" style={{ marginBottom: 12 }}>Формат команд</div>
        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Игроков в команде" hint="1, 2 или 5" error={errors.teamSize?.message}>
            <input className="input" type="number" {...register('teamSize')} />
          </Field>
        </div>

        <div className="divider" />

        {error && <div className="helper hint-error" style={{ marginBottom: 12 }}>{error}</div>}

        <div className="row" style={{ gap: 10 }}>
          <button className="btn btn-primary btn-lg" disabled={create.isPending}>
            <Icon name="check" size={14} /> {create.isPending ? 'Создаём…' : 'Создать турнир'}
          </button>
          <button type="button" className="btn btn-lg" onClick={() => navigate('/organizer')}>Отмена</button>
        </div>
      </form>
    </ScreenFrame>
  );
}
