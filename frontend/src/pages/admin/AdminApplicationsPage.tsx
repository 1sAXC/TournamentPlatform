import { useState } from 'react';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { adminNav } from '@/features/navigation';
import {
  useApproveApplication, useOrganizerApplications, useRejectApplication,
} from '@/features/admin/hooks';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { Icon } from '@/shared/ui/Icon';
import { Modal } from '@/shared/ui/Modal';
import { Alert } from '@/shared/ui/Alert';
import { Field } from '@/shared/ui/Field';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import type { OrganizerApplicationResponse } from '@/shared/api/types';

export function AdminApplicationsPage() {
  const pending = useOrganizerApplications({ pageNumber: 1, pageSize: 50 });
  const approve = useApproveApplication();
  const reject = useRejectApplication();
  const [rejectFor, setRejectFor] = useState<OrganizerApplicationResponse | null>(null);

  const items = pending.data?.items ?? [];
  const onReview = items.filter(a => a.status === 'PendingApproval' || a.status === 'Pending');
  const reviewed = items.filter(a => a.status !== 'PendingApproval' && a.status !== 'Pending');

  function onApprove(id: string) {
    approve.mutate(id, {
      onSuccess: () => showToast('success', 'Заявка одобрена'),
      onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
    });
  }

  return (
    <ScreenFrame nav={adminNav}>
      <div className="page-head">
        <div>
          <h1>Заявки организаторов</h1>
          <div className="sub">Проверка и одобрение новых организаторов</div>
        </div>
        <div className="actions">
          <Badge tone="full">{onReview.length} на рассмотрении</Badge>
        </div>
      </div>

      <h3 style={{ fontSize: 13, marginBottom: 12 }}>На рассмотрении</h3>
      <div className="col" style={{ gap: 10, marginBottom: 24 }}>
        {pending.isLoading ? (
          <EmptyState title="Загрузка…" />
        ) : onReview.length === 0 ? (
          <div className="card"><EmptyState title="Нет заявок на рассмотрении" /></div>
        ) : (
          onReview.map((a) => (
            <div key={a.id} className="card card-pad">
              <div className="row" style={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div className="row" style={{ gap: 14, flex: 1, alignItems: 'flex-start' }}>
                  <Avatar name={a.organizerName} size="lg" variant="org" />
                  <div className="col" style={{ gap: 4, flex: 1 }}>
                    <div style={{ fontWeight: 600, fontSize: 15 }}>{a.organizerName}</div>
                    <div className="row" style={{ gap: 8 }}>
                      <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>{a.email}</span>
                      <span style={{ color: 'var(--muted-2)' }}>·</span>
                      <span style={{ fontSize: 11.5, color: 'var(--muted)' }}>подана {formatDate(a.createdAtUtc)}</span>
                    </div>
                  </div>
                </div>
                <div className="row" style={{ gap: 6 }}>
                  <button className="btn btn-success" onClick={() => onApprove(a.id)} disabled={approve.isPending}>
                    <Icon name="check" size={13} /> Одобрить
                  </button>
                  <button className="btn btn-danger" onClick={() => setRejectFor(a)}>
                    <Icon name="x" size={13} /> Отклонить
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      <h3 style={{ fontSize: 13, marginBottom: 12 }}>Рассмотренные</h3>
      <div className="card" style={{ padding: 0 }}>
        {reviewed.length === 0 ? (
          <EmptyState title="Пока пусто" />
        ) : (
          <table className="tbl">
            <thead>
              <tr><th>Организация</th><th>E-mail</th><th>Подана</th><th>Решение</th><th>Дата решения</th></tr>
            </thead>
            <tbody>
              {reviewed.map((a) => {
                const approved = a.status === 'Active' || !!a.approvedAtUtc;
                return (
                  <tr key={a.id}>
                    <td className="strong">{a.organizerName}</td>
                    <td className="mono">{a.email}</td>
                    <td className="mono">{formatDate(a.createdAtUtc)}</td>
                    <td>
                      <Badge tone={approved ? 'success' : 'cancelled'}>
                        {approved ? 'Одобрена' : 'Отклонена'}
                      </Badge>
                    </td>
                    <td className="mono">{formatDate(a.approvedAtUtc ?? a.rejectedAtUtc)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {rejectFor && (
        <RejectModal
          application={rejectFor}
          isPending={reject.isPending}
          onConfirm={() => {
            reject.mutate(rejectFor.id, {
              onSuccess: () => { showToast('info', 'Заявка отклонена'); setRejectFor(null); },
              onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
            });
          }}
          onCancel={() => setRejectFor(null)}
        />
      )}
    </ScreenFrame>
  );
}

function RejectModal({
  application, onConfirm, onCancel, isPending,
}: {
  application: OrganizerApplicationResponse;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const [reason, setReason] = useState('');
  return (
    <Modal
      onClose={onCancel}
      eyebrow={application.organizerName}
      title="Отклонить заявку"
      footer={<>
        <button className="btn" onClick={onCancel}>Отмена</button>
        <button className="btn btn-danger" onClick={onConfirm} disabled={isPending}>
          {isPending ? 'Отклоняем…' : 'Отклонить заявку'}
        </button>
      </>}
    >
      <div className="col" style={{ gap: 12 }}>
        <Alert kind="warn" icon="flag">
          Организатор получит уведомление с причиной. Заявку можно подать снова.
        </Alert>
        <Field label="Причина отклонения" hint="Необязательно (пока хранится локально, в бэк не отправляется)">
          <textarea
            className="textarea" value={reason} onChange={(e) => setReason(e.target.value)}
            placeholder="Например: не предоставлена информация о проводимых ранее турнирах…"
          />
        </Field>
      </div>
    </Modal>
  );
}
