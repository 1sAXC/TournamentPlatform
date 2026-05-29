import { useMutation } from '@tanstack/react-query';
import { authApi } from '@/shared/api/authApi';
import { useAuthStore } from '@/shared/auth/authStore';
import type {
  ChangePasswordRequest,
  LoginRequest,
  RegisterOrganizerRequest,
  RegisterPlayerRequest,
  UpdateContactHandleRequest,
} from '@/shared/api/types';

export function useLoginMutation() {
  const login = useAuthStore((s) => s.login);
  return useMutation({
    mutationFn: (req: LoginRequest) => authApi.login(req),
    onSuccess: (data) => login(data),
  });
}

export function useRegisterPlayerMutation() {
  const login = useAuthStore((s) => s.login);
  return useMutation({
    mutationFn: (req: RegisterPlayerRequest) => authApi.registerPlayer(req),
    onSuccess: (data) => login(data),
  });
}

export function useRegisterOrganizerMutation() {
  const login = useAuthStore((s) => s.login);
  return useMutation({
    mutationFn: (req: RegisterOrganizerRequest) => authApi.registerOrganizer(req),
    onSuccess: (data) => login(data),
  });
}

export function useChangePasswordMutation() {
  return useMutation({
    mutationFn: (req: ChangePasswordRequest) => authApi.changePassword(req),
  });
}

export function useUpdateContactHandleMutation() {
  const setUser = useAuthStore((s) => s.setUser);
  return useMutation({
    mutationFn: (req: UpdateContactHandleRequest) => authApi.updateContactHandle(req),
    onSuccess: (data) => setUser(data),
  });
}
