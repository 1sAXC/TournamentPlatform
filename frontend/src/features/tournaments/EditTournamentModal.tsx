import { useState } from 'react';
import { Modal } from '@/shared/ui/Modal';
import { Field } from '@/shared/ui/Field';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import { useUpdateTournament } from '@/features/tournaments/hooks';

interface Props {
  tournamentId: string;
  initialTitle: string;
  initialDescription: string;
  onClose: () => void;
}

/**
 * Edit dialog for tournament title/description. Backed by PUT /api/tournaments/{id},
 * which is gated by RequireOrganizerOrAdmin on the server — both the tournament's
 * organizer and any admin can use it. Callers (OrgManagePage, AdminTournamentsPage)
 * are responsible for only opening it for tournaments in Open/Full status; otherwise
 * the server replies with Tournaments.EditNotAllowed.
 */
export function EditTournamentModal({ tournamentId, initialTitle, initialDescription, onClose }: Props) {
  const update = useUpdateTournament(tournamentId);
  const [title, setTitle] = useState(initialTitle);
  const [description, setDescription] = useState(initialDescription);
  const [error, setError] = useState<string | null>(null);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!title.trim()) { setError('Введите название турнира'); return; }
    update.mutate(
      { title: title.trim(), description: description.trim() || null },
      {
        onSuccess: () => { showToast('success', 'Турнир обновлён'); onClose(); },
        onError: (err) => setError(toApiError(err).title ?? 'Не удалось сохранить'),
      },
    );
  }

  return (
    <Modal
      onClose={onClose}
      title="Редактировать турнир"
      footer={<>
        <button className="btn" onClick={onClose}>Отмена</button>
        <button className="btn btn-primary" onClick={onSubmit} disabled={update.isPending}>
          {update.isPending ? 'Сохраняем…' : 'Сохранить'}
        </button>
      </>}
    >
      <form className="col" style={{ gap: 12 }} onSubmit={onSubmit}>
        <Field label="Название турнира">
          <input className="input" value={title} onChange={(e) => setTitle(e.target.value)} />
        </Field>
        <Field label="Описание">
          <textarea
            className="textarea" value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </Field>
        {error && <div className="helper hint-error">{error}</div>}
      </form>
    </Modal>
  );
}
