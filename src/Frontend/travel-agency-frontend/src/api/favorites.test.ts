import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getFavorites } from './favorites';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    get: vi.fn(),
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
}));

describe('getFavorites', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('keeps favorite order and marks a missing card as unavailable', async () => {
    mockApiClient.get
      .mockResolvedValueOnce({
        data: [
          { id: 'f1', userId: 'u', tourId: 'tour-published', addedAt: '2026-01-01' },
          { id: 'f2', userId: 'u', tourId: 'tour-gone', addedAt: '2026-01-02' },
        ],
      })
      .mockResolvedValueOnce({
        data: [
          {
            id: 'tour-published',
            title: 'Пхукет',
            available: true,
            priceFrom: 124000,
            currency: 'RUB',
            country: 'Таиланд',
            durationDays: 11,
          },
        ],
      });

    const result = await getFavorites();

    expect(mockApiClient.get).toHaveBeenNthCalledWith(2, '/catalog/tours/cards?ids=tour-published&ids=tour-gone');
    expect(result.map((tour) => tour.id)).toEqual(['tour-published', 'tour-gone']);
    expect(result[0]?.available).toBe(true);
    expect(result[0]?.priceFrom).toBe(124000);
    expect(result[1]?.available).toBe(false);
    expect(result[1]?.title).toBe('Тур недоступен');
  });
});
