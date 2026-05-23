import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/shared/auth/authStore';
import { isExpired } from '@/shared/lib/jwt';

export const http = axios.create({
  baseURL: '/',
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().token;
  if (token) {
    if (isExpired(token)) {
      useAuthStore.getState().logout();
    } else {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

http.interceptors.response.use(
  (r) => r,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      const store = useAuthStore.getState();
      if (store.token) {
        store.logout();
        if (!window.location.pathname.startsWith('/login')) {
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  },
);

export interface ApiErrorShape {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
}

export function toApiError(err: unknown): ApiErrorShape {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { title?: string; detail?: string; type?: string } | undefined;
    return {
      status: err.response?.status,
      title: data?.title ?? err.message,
      detail: data?.detail,
      code: data?.type,
    };
  }
  return { title: err instanceof Error ? err.message : 'Неизвестная ошибка' };
}
