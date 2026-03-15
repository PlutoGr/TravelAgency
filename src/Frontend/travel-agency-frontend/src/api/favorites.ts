import type { Tour } from '@/types';
import { apiClient } from './client';
import { getTourById } from './catalog';

interface FavoriteDto {
  id: string;
  userId: string;
  tourId: string;
  addedAt: string;
}

/**
 * Fetches favorite tours for the current user.
 * Fetches favorite IDs from /favorites, then getTourById for each (N+1 pattern).
 * A batch endpoint would require a backend change to avoid N+1 requests.
 */
export async function getFavorites(): Promise<Tour[]> {
  const { data } = await apiClient.get<FavoriteDto[]>('/favorites');
  const results = await Promise.allSettled(
    (data ?? []).map((f) => getTourById(f.tourId)),
  );
  return results
    .filter((r): r is PromiseFulfilledResult<Tour> => r.status === 'fulfilled')
    .map((r) => r.value);
}

export async function addFavorite(tourId: string): Promise<void> {
  await apiClient.post(`/favorites/${tourId}`);
}

export async function removeFavorite(tourId: string): Promise<void> {
  await apiClient.delete(`/favorites/${tourId}`);
}
