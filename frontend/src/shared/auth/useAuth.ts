import { useAuthStore } from './authStore';
import type { Role } from '@/shared/api/types';

export function useAuth() {
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const isHydrated = useAuthStore((s) => s.isHydrated);
  return {
    token,
    user,
    logout,
    isHydrated,
    isAuthenticated: !!token && !!user,
    role: user?.role as Role | undefined,
    isActiveOrganizer: user?.role === 'Organizer' && user?.accountStatus === 'Active',
  };
}
