import { http } from './http';
import type {
  AuthResponse, ChangePasswordRequest, CurrentUserResponse,
  LoginRequest, RegisterOrganizerRequest, RegisterPlayerRequest,
  UpdateContactHandleRequest,
} from './types';

export const authApi = {
  login: (req: LoginRequest) =>
    http.post<AuthResponse>('/api/auth/login', req).then(r => r.data),
  registerPlayer: (req: RegisterPlayerRequest) =>
    http.post<AuthResponse>('/api/auth/register/player', req).then(r => r.data),
  registerOrganizer: (req: RegisterOrganizerRequest) =>
    http.post<AuthResponse>('/api/auth/register/organizer', req).then(r => r.data),
  me: () =>
    http.get<CurrentUserResponse>('/api/auth/me').then(r => r.data),
  changePassword: (req: ChangePasswordRequest) =>
    http.post<void>('/api/auth/change-password', req).then(r => r.data),
  updateContactHandle: (req: UpdateContactHandleRequest) =>
    http.put<CurrentUserResponse>('/api/auth/contact-handle', req).then(r => r.data),
};
