import { useMutation } from '@tanstack/react-query';
import { authApi } from '@/shared/api/authApi';
import { useAuthStore } from '@/shared/auth/authStore';
import type { ChangePasswordRequest, LoginRequest, RegisterOrganizerRequest, RegisterPlayerRequest } from '@/shared/api/types';

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
