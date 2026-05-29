import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '@/shared/api/notificationsApi';
import { useAuthStore } from '@/shared/auth/authStore';

// Mirrors the auth-status polling cadence (see useMeSync). 30 s is a
// good trade-off for a diploma-prototype: short enough that captains
// see fresh match notifications within half a minute, long enough that
// the bell doesn't generate excessive traffic when nothing new happens.
const POLL_INTERVAL_MS = 30_000;

const NOTIFICATIONS_QUERY_KEY = ['notifications'] as const;

export function useNotifications() {
  const enabled = useAuthStore((s) => !!s.token && s.isHydrated);

  return useQuery({
    queryKey: NOTIFICATIONS_QUERY_KEY,
    queryFn: () => notificationsApi.list({ pageSize: 20 }),
    enabled,
    refetchInterval: enabled ? POLL_INTERVAL_MS : false,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_QUERY_KEY }),
  });
}
