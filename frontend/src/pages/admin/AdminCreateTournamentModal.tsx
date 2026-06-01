import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { Modal } from '@/shared/ui/Modal';
import { Field } from '@/shared/ui/Field';
import { Icon } from '@/shared/ui/Icon';
import { DISCIPLINES } from '@/shared/lib/disciplines';
import { useAdminCreateTournament, useAdminUsers } from '@/features/admin/hooks';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';

const schema = z.object({
  organizerId: z.string().uuid('Выберите организатора'),
  title: z.string().min(5, 'Минимум 5 символов').max(50, 'Максимум 50 символов'),
  description: z.string().max(150, 'Максимум 150 символов').optional(),
  disciplineCode: z.string().min(1, 'Выберите дисциплину'),
  format: z.enum(['Swiss', 'SingleElimination', 'DoubleElimination']),
  swissRounds: z.coerce.number().int().min(1).max(20).optional(),
  teamSize: z.coerce
    .number({ invalid_type_error: 'Введите размер команды' })
    .int('Должно быть целым числом')
    .refine((v) => v === 1 || v === 2 || v === 5, 'Размер команды должен быть 1, 2 или 5'),
  maxPlayers: z.coerce.number().int().min(2, 'Минимум 2 участника').max(120, 'Максимум 120 участников'),
});
type FormValues = z.infer<typeof schema>;

interface Props {
  onClose: () => void;
}

export function AdminCreateTournamentModal({ onClose }: Props) {
  const navigate = useNavigate();
  const create = useAdminCreateTournament();
  const { data: organizers, isLoading: orgLoading } = useAdminUsers({
    role: 'Organizer',
    status: 'Active',
    pageSize: 200,
  });
  const [error, setError] = useState<string | null>(null);

  const organizerOptions = useMemo(() => organizers?.items ?? [], [organizers]);

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      organizerId: '',
      title: '', description: '', disciplineCode: 'CS2',
      format: 'Swiss', swissRounds: 5, teamSize: 5, maxPlayers: 16,
    },
  });
  const format = watch('format');

  const onSubmit = handleSubmit((values) => {
    setError(null);
    const payload = {
      ...values,
      swissRounds: values.format === 'Swiss' ? (values.swissRounds ?? 5) : null,
      description: values.description?.trim() || null,
    };
    create.mutate(payload, {
      onSuccess: (data) => {
        showToast('success', 'Турнир создан');
        onClose();
        navigate(`/tournaments/${data.id}`);
      },
      onError: (err) => setError(toApiError(err).title ?? 'Не удалось создать турнир'),
    });
  });

  return (
    <Modal
      eyebrow="Администратор"
      title="Создать турнир"
      onClose={onClose}
      width={620}
    >
      <form onSubmit={onSubmit} className="col" style={{ gap: 14 }}>
        <Field label="Организатор" error={errors.organizerId?.message} hint="Турнир будет принадлежать выбранному организатору">
          <select className="select" {...register('organizerId')} disabled={orgLoading}>
            <option value="">{orgLoading ? 'Загрузка…' : '— выберите —'}</option>
            {organizerOptions.map(u => (
              <option key={u.id} value={u.id}>
                {u.organizerName || u.nickname || u.email}
              </option>
            ))}
          </select>
        </Field>

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

        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Формат" error={errors.format?.message}>
            <select className="select" {...register('format')} onChange={(e) => setValue('format', e.target.value as FormValues['format'], { shouldValidate: true })}>
              <option value="Swiss">Швейцарская</option>
              <option value="DoubleElimination">Double Elim</option>
              <option value="SingleElimination">Single Elim</option>
            </select>
          </Field>
          <Field label="Игроков в команде" error={errors.teamSize?.message} hint="1, 2 или 5">
            <input className="input" type="number" {...register('teamSize')} />
          </Field>
        </div>

        {format === 'Swiss' && (
          <Field label="Количество раундов" hint="От 1 до 20" error={errors.swissRounds?.message}>
            <input className="input" type="number" min={1} max={20} {...register('swissRounds')} />
          </Field>
        )}

        {error && <div className="helper hint-error">{error}</div>}

        <div className="row" style={{ gap: 10, justifyContent: 'flex-end', marginTop: 6 }}>
          <button type="button" className="btn" onClick={onClose}>Отмена</button>
          <button className="btn btn-primary" disabled={create.isPending}>
            <Icon name="check" size={12} /> {create.isPending ? 'Создаём…' : 'Создать'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
