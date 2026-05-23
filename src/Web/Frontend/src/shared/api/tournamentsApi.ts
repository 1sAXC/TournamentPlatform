import { http } from './http';
import type {
  CompleteMatchRequest, CreateTournamentRequest,
  TournamentDetailsResponse, TournamentListItemResponse,
} from './types';

export const tournamentsApi = {
  list: () =>
    http.get<TournamentListItemResponse[]>('/api/tournaments').then(r => r.data),
  getById: (id: string) =>
    http.get<TournamentDetailsResponse>(`/api/tournaments/${id}`).then(r => r.data),
  available: () =>
    http.get<TournamentListItemResponse[]>('/api/tournaments/available').then(r => r.data),
  active: () =>
    http.get<TournamentListItemResponse[]>('/api/tournaments/active').then(r => r.data),
  completed: () =>
    http.get<TournamentListItemResponse[]>('/api/tournaments/completed').then(r => r.data),
  my: () =>
    http.get<TournamentListItemResponse[]>('/api/tournaments/my').then(r => r.data),
  create: (req: CreateTournamentRequest) =>
    http.post<TournamentDetailsResponse>('/api/tournaments', req).then(r => r.data),
  register: (id: string) =>
    http.post<TournamentDetailsResponse>(`/api/tournaments/${id}/registration`).then(r => r.data),
  unregister: (id: string) =>
    http.delete<TournamentDetailsResponse>(`/api/tournaments/${id}/registration`).then(r => r.data),
  cancel: (id: string) =>
    http.post<TournamentDetailsResponse>(`/api/tournaments/${id}/cancel`).then(r => r.data),
  nextSwissRound: (id: string) =>
    http.post<void>(`/api/tournaments/${id}/swiss/next-round`).then(r => r.data),
  completeMatch: (tournamentId: string, matchId: string, req: CompleteMatchRequest) =>
    http.post<TournamentDetailsResponse>(
      `/api/tournaments/${tournamentId}/matches/${matchId}/complete`, req,
    ).then(r => r.data),

  organizerMine: () =>
    http.get<TournamentListItemResponse[]>('/api/organizer/tournaments').then(r => r.data),
};
