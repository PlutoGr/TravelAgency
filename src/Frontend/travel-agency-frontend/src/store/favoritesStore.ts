import type { Tour } from '@/types';
import { create } from 'zustand';
import toast from 'react-hot-toast';
import * as favoritesApi from '@/api/favorites';

type FavoritesState = {
  favoriteIds: string[];
  favoriteTours: Tour[];
  isLoading: boolean;
  count: number;
  toggleFavorite: (tourId: string, tour?: Tour) => void;
  isFavorite: (tourId: string) => boolean;
  loadFavorites: () => Promise<void>;
};

export const useFavoritesStore = create<FavoritesState>((set, get) => ({
  favoriteIds: [],
  favoriteTours: [],
  isLoading: false,
  count: 0,

  toggleFavorite: (tourId, tour) => {
    const { favoriteIds, favoriteTours } = get();
    const isCurrent = favoriteIds.includes(tourId);

    if (isCurrent) {
      const nextIds = favoriteIds.filter((id) => id !== tourId);
      const nextTours = favoriteTours.filter((t) => t.id !== tourId);
      set({ favoriteIds: nextIds, favoriteTours: nextTours, count: nextIds.length });
      favoritesApi.removeFavorite(tourId).catch(() => {
        set({ favoriteIds, favoriteTours, count: favoriteIds.length });
        toast.error('Не удалось удалить из избранного');
      });
    } else {
      const nextIds = [...favoriteIds, tourId];
      const nextTours = tour ? [...favoriteTours, tour] : favoriteTours;
      set({ favoriteIds: nextIds, favoriteTours: nextTours, count: nextIds.length });
      favoritesApi.addFavorite(tourId).catch(() => {
        set({ favoriteIds, favoriteTours, count: favoriteIds.length });
        toast.error('Не удалось добавить в избранное');
      });
    }
  },

  isFavorite: (tourId) => get().favoriteIds.includes(tourId),

  loadFavorites: async () => {
    set({ isLoading: true });
    try {
      const tours = await favoritesApi.getFavorites();
      const ids = tours.map((t) => t.id);
      set({ favoriteIds: ids, favoriteTours: tours, count: ids.length });
    } catch {
      set({ favoriteIds: [], favoriteTours: [], count: 0 });
      toast.error('Не удалось загрузить избранное');
    } finally {
      set({ isLoading: false });
    }
  },
}));
