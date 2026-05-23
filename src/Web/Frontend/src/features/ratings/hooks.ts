import { useQuery } from '@tanstack/react-query';
import { ratingsApi } from '@/shared/api/ratingsApi';

export const ratingKeys = {
  all: ['ratings'] as const,
  byPlayer: (playerId: string) => [...ratingKeys.all, 'player', playerId] as const,
  history: (playerId: string) => [...ratingKeys.all, 'history', playerId] as const,
};

export const usePlayerRatings = (playerId: string | undefined) =>
  useQuery({
    queryKey: ratingKeys.byPlayer(playerId ?? ''),
    queryFn: () => ratingsApi.byPlayer(playerId!),
    enabled: !!playerId,
  });

export const useRatingHistory = (playerId: string | undefined) =>
  useQuery({
    queryKey: ratingKeys.history(playerId ?? ''),
    queryFn: () => ratingsApi.history(playerId!),
    enabled: !!playerId,
  });
