import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '@/shared/api/notificationsApi';
import { useAuthStore } from '@/shared/auth/authStore';

// Mirrors the auth-status polling cadence (see useMeSync). 30 s is a
// good trade-off for a diploma-prototype: short enough that captains
// see fresh match notifications within half a minute, long enough that
// the bell doesn't generate excessive traffic when nothing new happens.
const POLL_INTERVAL_MS = 30_000;

const NOTIFICATIONS_ROOT_KEY = ['notifications'] as const;

export function useNotifications() {
  const enabled = useAuthStore((s) => !!s.token && s.isHydrated);

  return useQuery({
    queryKey: [...NOTIFICATIONS_ROOT_KEY, 'bell'] as const,
    queryFn: () => notificationsApi.list({ pageSize: 20 }),
    enabled,
    refetchInterval: enabled ? POLL_INTERVAL_MS : false,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
  });
}

export function useAllNotifications() {
  const enabled = useAuthStore((s) => !!s.token && s.isHydrated);

  return useQuery({
    queryKey: [...NOTIFICATIONS_ROOT_KEY, 'all'] as const,
    queryFn: () => notificationsApi.list({ pageSize: 100 }),
    enabled,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    // Prefix invalidation: both the bell and the full-list queries live
    // under ['notifications'] so a single invalidate refreshes both.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_ROOT_KEY }),
  });
}
