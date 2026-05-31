import { http } from './http';
import type { NotificationListResponse } from './types';

export const notificationsApi = {
  list: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    http.get<NotificationListResponse>('/api/notifications', { params })
      .then(r => r.data),
  markRead: (id: string) =>
    http.post<void>(`/api/notifications/${id}/read`).then(r => r.data),
};
