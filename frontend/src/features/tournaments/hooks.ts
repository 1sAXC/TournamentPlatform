import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { tournamentsApi } from '@/shared/api/tournamentsApi';
import type {
  CompleteMatchRequest, CreateTournamentRequest, UpdateTournamentRequest,
  TournamentDetailsResponse,
} from '@/shared/api/types';

export const qk = {
  all: ['tournaments'] as const,
  list: () => [...qk.all, 'list'] as const,
  available: () => [...qk.all, 'available'] as const,
  active: () => [...qk.all, 'active'] as const,
  completed: () => [...qk.all, 'completed'] as const,
  my: () => [...qk.all, 'my'] as const,
  organizerMine: () => [...qk.all, 'organizer-mine'] as const,
  detail: (id: string) => [...qk.all, 'detail', id] as const,
  matchDetail: (tournamentId: string, matchId: string) =>
    [...qk.all, 'match-detail', tournamentId, matchId] as const,
};

export const useAllTournaments = () =>
  useQuery({ queryKey: qk.list(), queryFn: tournamentsApi.list });

export const useAvailableTournaments = () =>
  useQuery({ queryKey: qk.available(), queryFn: tournamentsApi.available });

export const useActiveTournaments = () =>
  useQuery({ queryKey: qk.active(), queryFn: tournamentsApi.active });

export const useCompletedTournaments = () =>
  useQuery({ queryKey: qk.completed(), queryFn: tournamentsApi.completed });

export const useMyTournaments = () =>
  useQuery({ queryKey: qk.my(), queryFn: tournamentsApi.my });

export const useOrganizerTournaments = (options: { enabled?: boolean } = {}) =>
  useQuery({
    queryKey: qk.organizerMine(),
    queryFn: tournamentsApi.organizerMine,
    // The backend endpoint requires RequireActiveOrganizer. Disable the query
    // for pending/rejected organizers so they don't hit a guaranteed 403
    // when landing on /organizer/profile.
    enabled: options.enabled ?? true,
  });

export const useTournament = (id: string | undefined) =>
  useQuery({
    queryKey: qk.detail(id ?? ''),
    queryFn: () => tournamentsApi.getById(id!),
    enabled: !!id,
  });

export const useMatchDetails = (tournamentId: string | undefined, matchId: string | undefined) =>
  useQuery({
    queryKey: qk.matchDetail(tournamentId ?? '', matchId ?? ''),
    queryFn: () => tournamentsApi.getMatchDetails(tournamentId!, matchId!),
    enabled: !!tournamentId && !!matchId,
  });

function invalidateLists(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: qk.all });
}

export function useCreateTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateTournamentRequest) => tournamentsApi.create(req),
    onSuccess: (data: TournamentDetailsResponse) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}

export function useRegisterForTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tournamentsApi.register(id),
    onSuccess: (data) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}

export function useUnregisterFromTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tournamentsApi.unregister(id),
    onSuccess: (data) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}

export function useCancelTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tournamentsApi.cancel(id),
    onSuccess: (data) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}

export function useUpdateTournament(tournamentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateTournamentRequest) => tournamentsApi.update(tournamentId, req),
    onSuccess: (data) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}

export function useNextSwissRound() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tournamentsApi.nextSwissRound(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: qk.detail(id) });
      invalidateLists(qc);
    },
  });
}

export function useCompleteMatch(tournamentId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ matchId, req }: { matchId: string; req: CompleteMatchRequest }) =>
      tournamentsApi.completeMatch(tournamentId, matchId, req),
    onSuccess: (data) => {
      qc.setQueryData(qk.detail(data.id), data);
      invalidateLists(qc);
    },
  });
}
