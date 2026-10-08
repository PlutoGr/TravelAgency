import type { Tour } from '@/types';
import { apiClient } from './client';
import { getTourCards, unavailableTour } from './catalog';

interface FavoriteDto {
  id: string;
  userId: string;
  tourId: string;
  addedAt: string;
}

/**
 * Избранное: id из Booking, карточки из публичного каталога.
 * Снятый тур приходит с available: false и остаётся в списке.
 * Черновик и неизвестный id в cards не попадают — для них пометка «Тур недоступен».
 */
export async function getFavorites(): Promise<Tour[]> {
  const { data } = await apiClient.get<FavoriteDto[]>('/favorites');
  const ids = (data ?? []).map((favorite) => favorite.tourId).filter(Boolean);
  if (ids.length === 0) return [];

  const cards = await getTourCards(ids);
  const byId = new Map(cards.map((tour) => [tour.id, tour]));
  return ids.map((id) => byId.get(id) ?? unavailableTour(id));
}

export async function addFavorite(tourId: string): Promise<void> {
  await apiClient.post(`/favorites/${tourId}`);
}

export async function removeFavorite(tourId: string): Promise<void> {
  await apiClient.delete(`/favorites/${tourId}`);
}
