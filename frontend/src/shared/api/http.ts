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
  'MaxPlayers must be divisible by TeamSize.': 'Максимальное число игроков должно делиться на размер команды',
  'Format is invalid.': 'Некорректный формат турнира',
  'TeamSize must be 1, 2 or 5.': 'Размер команды должен быть 1, 2 или 5',
};

// Maps backend error codes (ProblemDetails.type) to localized titles.
// Keys mirror the Error.Code values in *Errors.cs on the backend.
const errorCodeMap: Record<string, string> = {
  'Auth.DuplicateEmail': 'Пользователь с таким e-mail уже зарегистрирован',
  'Auth.DuplicateNickname': 'Этот никнейм уже занят',
  'Auth.InvalidCredentials': 'Неверный логин или пароль',
  'Auth.InvalidCurrentPassword': 'Текущий пароль неверен',
  'Auth.AccessDenied': 'Аккаунту запрещён вход',
  'Auth.UserNotFound': 'Пользователь не найден',
  'Auth.InvalidContactHandle': 'Укажите контакт (1–64 символа)',
  'Admin.InvalidRole': 'Некорректная роль пользователя',
  'Admin.InvalidStatus': 'Некорректный статус аккаунта',
  'Admin.UserNotFound': 'Пользователь не найден',
  'Admin.DuplicateEmail': 'Пользователь с таким e-mail уже зарегистрирован',
  'Admin.DuplicateNickname': 'Этот никнейм уже занят',
  'Admin.OrganizerApprovalNotAllowed': 'Одобрить можно только заявку в статусе «На рассмотрении»',
  'Admin.OrganizerRejectNotAllowed': 'Отклонить можно только заявку в статусе «На рассмотрении»',
  'Admin.LastAdminDeleteNotAllowed': 'Нельзя удалить последнего активного администратора',
  'Admin.PlayerNicknameRequired': 'Необходимо указать никнейм игрока',
  'Admin.OrganizerNameRequired': 'Необходимо указать название организации',
  'Admin.ContactHandleRequired': 'Контакт обязателен для игроков и организаторов',
  'Tournaments.AccessDenied': 'Создавать турниры могут только активные организаторы',
  'Tournaments.NotFound': 'Турнир не найден',
  'Tournaments.DuplicateTitle': 'Турнир с таким названием уже существует',
  'Tournaments.InvalidTitle': 'Некорректное название турнира',
  'Tournaments.InvalidFormat': 'Некорректный формат турнира',
  'Tournaments.InvalidMaxPlayers': 'Максимальное число участников должно быть не больше 120',
  'Tournaments.InvalidTeamSize': 'Размер команды недопустим для этой дисциплины',
  'Tournaments.InvalidWinnerTeam': 'Победитель должен быть одной из команд матча',
  'Tournaments.InvalidMatchScore': 'Счёт победителя должен быть больше счёта проигравшего',
  'Tournaments.MaxPlayersNotMultipleOfTeamSize': 'Максимальное число игроков должно делиться на размер команды',
  'Tournaments.DisciplineNotFound': 'Дисциплина не существует или отключена',
  'Tournaments.InvalidSwissRounds': 'Количество раундов задаётся только для швейцарской системы',
  'Tournaments.PlayerAccessDenied': 'Регистрироваться на турниры могут только игроки',
  'Tournaments.MissingNickname': 'У игрока должен быть указан никнейм',
  'Tournaments.RegistrationClosed': 'Регистрация на турнир закрыта',
  'Tournaments.DuplicateRegistration': 'Вы уже зарегистрированы в этом турнире',
  'Tournaments.Full': 'Достигнут лимит участников турнира',
  'Tournaments.ParticipantNotFound': 'Вы не зарегистрированы в этом турнире',
  'Tournaments.AlreadyStarted': 'Изменить регистрацию можно только до старта турнира',
  'Tournaments.RegistrationConflict': 'Регистрация изменилась параллельно, попробуйте ещё раз',
  'Tournaments.CannotCancelCompleted': 'Завершённый турнир нельзя отменить',
  'Tournaments.AdminAccessDenied': 'Это действие доступно только администраторам',
  'Tournaments.MatchAlreadyCompleted': 'Матч уже завершён',
  'Tournaments.EditNotAllowed': 'Редактировать турнир можно только до его старта',
  'Tournaments.OrganizerNotFound': 'Организатор не найден',
  'Tournaments.OrganizerRoleRequired': 'Пользователь должен иметь роль организатора',
  'Tournaments.OrganizerInactive': 'Организатор должен быть активным',
  'Tournaments.CurrentRoundNotCompleted': 'Текущий раунд ещё не завершён',
};

function translateValidationMessage(message: string): string {
  return validationMessageMap[message] ?? message;
}

function translateErrorCode(code: string | undefined): string | undefined {
  if (!code) return undefined;
  return errorCodeMap[code];
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

    const localizedTitle = translateErrorCode(data?.type);
    return {
      status: err.response?.status,
      title: validationTitle ?? localizedTitle ?? data?.detail ?? data?.title ?? err.message,
      detail: data?.detail,
      code: data?.type,
      validationErrors,
    };
  }
  return { title: err instanceof Error ? err.message : 'Неизвестная ошибка' };
}
