import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { authApi } from '@/shared/api/authApi';
import { useAuthStore } from './authStore';

const POLL_INTERVAL_MS = 30_000;

/**
 * Keeps the local auth store in sync with the backend `/api/auth/me` endpoint.
 *
 * - Polls every 30 s while authenticated.
 * - If the response indicates a status change (organizer approved, deleted,
 *   rejected) we either update the cached user or rely on the 401 interceptor
 *   to log the user out (deleted/rejected → /me returns 401 → auto-logout).
 */
export function useMeSync() {
  const token = useAuthStore((s) => s.token);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  const setUser = useAuthStore((s) => s.setUser);

  const enabled = isHydrated && !!token;

  const query = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: authApi.me,
    enabled,
    refetchInterval: enabled ? POLL_INTERVAL_MS : false,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
    retry: false,
  });

  useEffect(() => {
    if (query.data) setUser(query.data);
  }, [query.data, setUser]);
}
