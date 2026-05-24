import { http } from './http';
import type {
  AdminCreateTournamentRequest,
  AdminUserResponse, AdminUsersQuery,
  CreateAdminUserRequest,
  OrganizerApplicationResponse, OrganizerApplicationsQuery,
  PagedResult, ResetPasswordRequest, ResetPasswordResponse,
  TournamentDetailsResponse,
  UpdateUserRoleRequest,
} from './types';

export const adminApi = {
  // Organizer applications
  listApplications: (q: OrganizerApplicationsQuery = {}) =>
    http.get<PagedResult<OrganizerApplicationResponse>>(
      '/api/admin/organizer-applications', { params: q },
    ).then(r => r.data),
  listApplicationsHistory: (q: OrganizerApplicationsQuery = {}) =>
    http.get<PagedResult<OrganizerApplicationResponse>>(
      '/api/admin/organizer-applications/history', { params: q },
    ).then(r => r.data),
  approveApplication: (id: string) =>
    http.post<OrganizerApplicationResponse>(
      `/api/admin/organizer-applications/${id}/approve`,
    ).then(r => r.data),
  rejectApplication: (id: string) =>
    http.post<OrganizerApplicationResponse>(
      `/api/admin/organizer-applications/${id}/reject`,
    ).then(r => r.data),

  // Admin users
  listUsers: (q: AdminUsersQuery = {}) =>
    http.get<PagedResult<AdminUserResponse>>('/api/admin/users', { params: q }).then(r => r.data),
  getUser: (id: string) =>
    http.get<AdminUserResponse>(`/api/admin/users/${id}`).then(r => r.data),
  createUser: (req: CreateAdminUserRequest) =>
    http.post<AdminUserResponse>('/api/admin/users', req).then(r => r.data),
  deleteUser: (id: string) =>
    http.delete<void>(`/api/admin/users/${id}`).then(r => r.data),
  resetPassword: (id: string, req: ResetPasswordRequest = {}) =>
    http.post<ResetPasswordResponse>(`/api/admin/users/${id}/reset-password`, req).then(r => r.data),
  updateRole: (id: string, req: UpdateUserRoleRequest) =>
    http.patch<AdminUserResponse>(`/api/admin/users/${id}/role`, req).then(r => r.data),

  // Admin tournaments
  createTournament: (req: AdminCreateTournamentRequest) =>
    http.post<TournamentDetailsResponse>('/api/admin/tournaments', req).then(r => r.data),
  deleteTournament: (id: string) =>
    http.delete<void>(`/api/admin/tournaments/${id}`).then(r => r.data),
};
