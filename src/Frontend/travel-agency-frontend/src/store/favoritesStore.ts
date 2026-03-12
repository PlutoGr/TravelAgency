import { create } from 'zustand';
import * as favoritesApi from '@/api/favorites';

const STORAGE_KEY = 'favorite_tours';

function readPersistedIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

type FavoritesState = {
  favoriteIds: string[];
  isLoading: boolean;
  count: number;
  toggleFavorite: (tourId: string) => void;
  isFavorite: (tourId: string) => boolean;
  loadFavorites: () => void;
};

export const useFavoritesStore = create<FavoritesState>((set, get) => ({
  favoriteIds: readPersistedIds(),
  isLoading: false,
  count: readPersistedIds().length,

  toggleFavorite: (tourId) => {
    const { favoriteIds } = get();
    const isCurrent = favoriteIds.includes(tourId);

    let next: string[];
    if (isCurrent) {
      next = favoriteIds.filter((id) => id !== tourId);
      favoritesApi.removeFavorite(tourId);
    } else {
      next = [...favoriteIds, tourId];
      favoritesApi.addFavorite(tourId);
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    set({ favoriteIds: next, count: next.length });
  },

  isFavorite: (tourId) => get().favoriteIds.includes(tourId),

  loadFavorites: () => {
    const ids = readPersistedIds();
    set({ favoriteIds: ids, count: ids.length });
  },
}));
