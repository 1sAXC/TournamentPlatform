import { http } from './http';
import type { PlayerRatingResponse, RatingHistoryResponse } from './types';

export const ratingsApi = {
  byPlayer: (playerId: string) =>
    http.get<PlayerRatingResponse[]>(`/api/ratings/players/${playerId}`).then(r => r.data),
  byPlayerDiscipline: (playerId: string, disciplineCode: string) =>
    http.get<PlayerRatingResponse>(
      `/api/ratings/players/${playerId}/disciplines/${encodeURIComponent(disciplineCode)}`,
    ).then(r => r.data),
  history: (playerId: string) =>
    http.get<RatingHistoryResponse[]>(`/api/ratings/players/${playerId}/history`).then(r => r.data),
};
