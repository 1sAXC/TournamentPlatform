import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '@/shared/api/adminApi';
import type {
  AdminCreateTournamentRequest,
  AdminUsersQuery,
  CreateAdminUserRequest,
  OrganizerApplicationsQuery,
  ResetPasswordRequest,
  UpdateUserRoleRequest,
} from '@/shared/api/types';

export const adminKeys = {
  apps: (q: OrganizerApplicationsQuery) => ['admin', 'applications', q] as const,
  appsHistory: (q: OrganizerApplicationsQuery) => ['admin', 'applications-history', q] as const,
  users: (q: AdminUsersQuery) => ['admin', 'users', q] as const,
};

export const useOrganizerApplications = (q: OrganizerApplicationsQuery = {}) =>
  useQuery({ queryKey: adminKeys.apps(q), queryFn: () => adminApi.listApplications(q) });

export const useOrganizerApplicationsHistory = (q: OrganizerApplicationsQuery = {}) =>
  useQuery({ queryKey: adminKeys.appsHistory(q), queryFn: () => adminApi.listApplicationsHistory(q) });

export const useAdminUsers = (q: AdminUsersQuery = {}) =>
  useQuery({ queryKey: adminKeys.users(q), queryFn: () => adminApi.listUsers(q) });

export function useApproveApplication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.approveApplication(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'applications'] });
      qc.invalidateQueries({ queryKey: ['admin', 'applications-history'] });
      qc.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });
}

export function useRejectApplication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.rejectApplication(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'applications'] });
      qc.invalidateQueries({ queryKey: ['admin', 'applications-history'] });
      qc.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });
}

export function useCreateAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateAdminUserRequest) => adminApi.createUser(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  });
}

export function useBlockAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.blockUser(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  });
}

export function useUnblockAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.unblockUser(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  });
}

export function useResetUserPassword() {
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req?: ResetPasswordRequest }) =>
      adminApi.resetPassword(id, req),
  });
}

export function useUpdateUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateUserRoleRequest }) =>
      adminApi.updateRole(id, req),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  });
}

export function useAdminCreateTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: AdminCreateTournamentRequest) => adminApi.createTournament(req),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tournaments'] }),
  });
}

export function useAdminDeleteTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.deleteTournament(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tournaments'] }),
  });
}
