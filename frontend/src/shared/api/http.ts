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
  validationErrors?: string[];
}

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  type?: string;
  errors?: Record<string, string[]>;
};

const validationMessageMap: Record<string, string> = {
  'Password must contain at least one letter.': 'Пароль должен содержать хотя бы одну латинскую букву',
  'Password must contain at least one digit.': 'Пароль должен содержать хотя бы одну цифру',
  "'Password' must not be empty.": 'Введите пароль',
  "'Password' must be at least 8 characters. You entered 0 characters.": 'Минимум 8 символов',
  "'Email' is not a valid email address.": 'Некорректный e-mail',
  "'Email' must not be empty.": 'Введите e-mail',
  "'Nickname' must not be empty.": 'Введите никнейм',
  "'Organizer Name' must not be empty.": 'Введите название организации',
};

function translateValidationMessage(message: string): string {
  return validationMessageMap[message] ?? message;
}

function getValidationErrors(data?: ProblemDetailsResponse): string[] {
  if (!data?.errors) {
    return [];
  }

  return Object.values(data.errors)
    .flat()
    .filter((message): message is string => typeof message === 'string' && message.length > 0)
    .map(translateValidationMessage);
}

export function toApiError(err: unknown): ApiErrorShape {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as ProblemDetailsResponse | undefined;
    const validationErrors = getValidationErrors(data);
    const validationTitle = validationErrors.length > 0
      ? validationErrors.join('; ')
      : undefined;

    return {
      status: err.response?.status,
      title: validationTitle ?? data?.detail ?? data?.title ?? err.message,
      detail: data?.detail,
      code: data?.type,
      validationErrors,
    };
  }
  return { title: err instanceof Error ? err.message : 'Неизвестная ошибка' };
}
