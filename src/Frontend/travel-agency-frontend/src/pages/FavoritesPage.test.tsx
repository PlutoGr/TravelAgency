import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import FavoritesPage from './FavoritesPage';
import { getFavorites } from '@/api/favorites';
import { useFavoritesStore } from '@/store/favoritesStore';
import { sampleTour } from '@/test/sampleTour';
import { renderPage } from '@/test/render';

vi.mock('@/api/favorites', () => ({
  getFavorites: vi.fn(),
  addFavorite: vi.fn(),
  removeFavorite: vi.fn(),
}));

describe('FavoritesPage', () => {
  beforeEach(() => {
    useFavoritesStore.setState({
      favoriteIds: [],
      favoriteTours: [],
      isLoading: false,
      error: null,
      count: 0,
    });
    vi.mocked(getFavorites).mockReset();
  });

  it('keeps an unpublished tour and hides the request action', async () => {
    vi.mocked(getFavorites).mockResolvedValue([
      sampleTour({
        available: false,
        title: 'Снятый тур',
        price: 0,
        priceFrom: null,
        offers: [],
      }),
    ]);

    renderPage(<FavoritesPage />);

    expect(await screen.findByRole('heading', { name: 'Снятый тур' })).toBeInTheDocument();
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: /заявк/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Подробнее/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Убрать из избранного' })).toBeInTheDocument();
  });

  it('shows an error when favorites cannot be loaded', async () => {
    vi.mocked(getFavorites).mockRejectedValue(new Error('network'));

    renderPage(<FavoritesPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить избранное');
  });
});
