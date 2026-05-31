import { useState } from 'react';
import { ScreenFrame } from '@/shared/ui/ScreenFrame';
import { adminNav } from '@/features/navigation';
import {
  useApproveApplication, useOrganizerApplications, useOrganizerApplicationsHistory, useRejectApplication,
} from '@/features/admin/hooks';
import { Avatar } from '@/shared/ui/Avatar';
import { Badge } from '@/shared/ui/Badge';
import { Icon } from '@/shared/ui/Icon';
import { Modal } from '@/shared/ui/Modal';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatDate } from '@/shared/lib/formatters';
import { showToast } from '@/shared/ui/Toast';
import { toApiError } from '@/shared/api/http';
import type { OrganizerApplicationResponse } from '@/shared/api/types';

export function AdminApplicationsPage() {
  const pending = useOrganizerApplications({ pageNumber: 1, pageSize: 50 });
  const history = useOrganizerApplicationsHistory({ pageNumber: 1, pageSize: 50 });
  const approve = useApproveApplication();
  const reject = useRejectApplication();
  const [rejectFor, setRejectFor] = useState<OrganizerApplicationResponse | null>(null);
  const [approveFor, setApproveFor] = useState<OrganizerApplicationResponse | null>(null);

  const onReview = pending.data?.items ?? [];
  const reviewed = history.data?.items ?? [];

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
                    <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
                      <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }}>{a.email}</span>
                      {a.contactHandle && (
                        <>
                          <span style={{ color: 'var(--muted-2)' }}>·</span>
                          <span className="mono" style={{ fontSize: 11, color: 'var(--muted)' }} title="Контакт заявителя">
                            {a.contactHandle}
                          </span>
                        </>
                      )}
                      <span style={{ color: 'var(--muted-2)' }}>·</span>
                      <span style={{ fontSize: 11.5, color: 'var(--muted)' }}>подана {formatDate(a.createdAtUtc)}</span>
                    </div>
                  </div>
                </div>
                <div className="row" style={{ gap: 6 }}>
                  <button className="btn btn-success" onClick={() => setApproveFor(a)}>
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
        {history.isLoading ? (
          <EmptyState title="Загрузка…" />
        ) : reviewed.length === 0 ? (
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

      {approveFor && (
        <Modal
          onClose={() => setApproveFor(null)}
          eyebrow={approveFor.organizerName}
          title="Одобрить заявку"
          footer={<>
            <button className="btn" onClick={() => setApproveFor(null)}>Отмена</button>
            <button
              className="btn btn-success"
              disabled={approve.isPending}
              onClick={() => {
                approve.mutate(approveFor.id, {
                  onSuccess: () => { showToast('success', 'Заявка одобрена'); setApproveFor(null); },
                  onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
                });
              }}
            >
              {approve.isPending ? 'Одобряем…' : 'Одобрить заявку'}
            </button>
          </>}
        >
          <div style={{ fontSize: 13.5 }}>
            Одобрить заявку организатора «{approveFor.organizerName}»?
          </div>
        </Modal>
      )}

      {rejectFor && (
        <Modal
          onClose={() => setRejectFor(null)}
          eyebrow={rejectFor.organizerName}
          title="Отклонить заявку"
          footer={<>
            <button className="btn" onClick={() => setRejectFor(null)}>Отмена</button>
            <button
              className="btn btn-danger"
              disabled={reject.isPending}
              onClick={() => {
                reject.mutate(rejectFor.id, {
                  onSuccess: () => { showToast('info', 'Заявка отклонена'); setRejectFor(null); },
                  onError: (err) => showToast('error', toApiError(err).title ?? 'Не удалось'),
                });
              }}
            >
              {reject.isPending ? 'Отклоняем…' : 'Отклонить заявку'}
            </button>
          </>}
        >
          <div style={{ fontSize: 13.5 }}>
            Отклонить заявку организатора «{rejectFor.organizerName}»?
          </div>
        </Modal>
      )}
    </ScreenFrame>
  );
}
