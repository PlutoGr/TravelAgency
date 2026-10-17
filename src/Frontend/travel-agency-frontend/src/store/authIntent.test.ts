import { beforeEach, describe, expect, it, vi } from 'vitest';
import toast from 'react-hot-toast';
import { addFavorite, getFavorites, removeFavorite } from '@/api/favorites';
import { resumeFavoriteIntent } from './authIntent';
import { useFavoritesStore } from './favoritesStore';
import { useUIStore } from './uiStore';
import { sampleTour } from '@/test/sampleTour';

vi.mock('@/api/favorites', () => ({
  addFavorite: vi.fn().mockResolvedValue(undefined),
  removeFavorite: vi.fn().mockResolvedValue(undefined),
  getFavorites: vi.fn().mockResolvedValue([]),
}));

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

describe('auth intent', () => {
  beforeEach(() => {
    vi.mocked(addFavorite).mockClear();
    vi.mocked(removeFavorite).mockClear();
    vi.mocked(toast.error).mockClear();
    vi.mocked(toast.success).mockClear();
    vi.mocked(getFavorites).mockReset();
    vi.mocked(getFavorites).mockResolvedValue([]);
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: null,
      isMobileMenuOpen: false,
    });
    useFavoritesStore.setState({
      favoriteIds: [],
      favoriteTours: [],
      isLoading: false,
      error: null,
      count: 0,
    });
  });

  it('keeps the intent while the login window switches tabs and after a successful close', () => {
    useUIStore.getState().openAuthModal('login', {
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-2',
    });
    useUIStore.getState().openAuthModal('register');

    expect(useUIStore.getState().authModalTab).toBe('register');
    expect(useUIStore.getState().authIntent).toEqual({
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-2',
    });

    useUIStore.getState().closeAuthModal();
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(useUIStore.getState().authIntent?.type).toBe('request');
  });

  it('drops the intent when the guest closes the window or opens a fresh login', () => {
    useUIStore.getState().openAuthModal('login', { type: 'favorite', tourId: 'tour-1' });
    useUIStore.getState().dismissAuthModal();
    expect(useUIStore.getState().authIntent).toBeNull();

    useUIStore.getState().openAuthModal('login', { type: 'favorite', tourId: 'tour-1' });
    useUIStore.getState().closeAuthModal();
    useUIStore.getState().openAuthModal('login');
    expect(useUIStore.getState().authIntent).toBeNull();
  });

  it('adds the tour after login when the guest clicked the heart, and leaves a request intent', () => {
    const tour = sampleTour();
    useUIStore.setState({
      authIntent: { type: 'favorite', tourId: tour.id, tour },
    });

    resumeFavoriteIntent();

    expect(addFavorite).toHaveBeenCalledWith(tour.id);
    expect(useFavoritesStore.getState().isFavorite(tour.id)).toBe(true);
    expect(useUIStore.getState().authIntent).toBeNull();

    useUIStore.setState({
      authIntent: { type: 'request', tourId: tour.id, offerId: 'offer-1' },
    });
    resumeFavoriteIntent();
    expect(useUIStore.getState().authIntent?.type).toBe('request');
  });

  it('does not remove a tour that is already in favorites', () => {
    const tour = sampleTour();
    useFavoritesStore.setState({ favoriteIds: [tour.id], count: 1 });
    useUIStore.setState({ authIntent: { type: 'favorite', tourId: tour.id, tour } });

    resumeFavoriteIntent();

    expect(addFavorite).not.toHaveBeenCalled();
    expect(removeFavorite).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
    expect(useFavoritesStore.getState().isFavorite(tour.id)).toBe(true);
    expect(useUIStore.getState().authIntent).toBeNull();
  });

  it('clears a favorite intent and does not toggle when favorites failed to load', async () => {
    const tour = sampleTour();
    vi.mocked(getFavorites).mockRejectedValueOnce(new Error('network'));
    useUIStore.setState({
      authIntent: { type: 'favorite', tourId: tour.id, tour },
    });

    await useFavoritesStore.getState().loadFavorites();
    resumeFavoriteIntent();

    expect(useFavoritesStore.getState().error).toBe('Не удалось загрузить избранное');
    expect(addFavorite).not.toHaveBeenCalled();
    expect(removeFavorite).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalledWith('Не удалось добавить тур в избранное. Попробуйте ещё раз');
    expect(useFavoritesStore.getState().isFavorite(tour.id)).toBe(false);
    expect(useUIStore.getState().authIntent).toBeNull();
  });
});
