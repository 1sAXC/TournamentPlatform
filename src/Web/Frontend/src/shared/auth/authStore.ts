import { create } from 'zustand';
import type { AuthResponse, CurrentUserResponse } from '@/shared/api/types';
import { isExpired } from '@/shared/lib/jwt';

const TOKEN_KEY = 'tp.token';
const USER_KEY = 'tp.user';

interface AuthState {
  token: string | null;
  user: CurrentUserResponse | null;
  isHydrated: boolean;
  login: (response: AuthResponse) => void;
  setUser: (user: CurrentUserResponse) => void;
  logout: () => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  isHydrated: false,
  login: (response) => {
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    set({ token: response.accessToken, user: response.user });
  },
  setUser: (user) => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    set({ user });
  },
  logout: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    set({ token: null, user: null });
  },
  hydrate: () => {
    const token = localStorage.getItem(TOKEN_KEY);
    const userRaw = localStorage.getItem(USER_KEY);
    if (token && !isExpired(token) && userRaw) {
      try {
        const user = JSON.parse(userRaw) as CurrentUserResponse;
        set({ token, user, isHydrated: true });
        return;
      } catch {
        // fallthrough
      }
    }
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    set({ token: null, user: null, isHydrated: true });
  },
}));
