import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import HomePage from './HomePage';
import { getDestinations, getTours } from '@/api/catalog';
import { renderPage } from '@/test/render';

vi.mock('@/api/catalog', () => ({
  getTours: vi.fn(),
  getDestinations: vi.fn(),
  getTourById: vi.fn(),
}));

describe('HomePage', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'IntersectionObserver',
      class {
        observe() {}
        unobserve() {}
        disconnect() {}
        takeRecords() {
          return [];
        }
      },
    );
    vi.mocked(getDestinations).mockResolvedValue([]);
    vi.mocked(getTours).mockReset();
  });

  it('does not show hot deals or request them', async () => {
    renderPage(<HomePage />);

    expect(await screen.findByRole('heading', { name: 'Отзывы наших клиентов' })).toBeInTheDocument();
    expect(screen.queryByText(/Горящие предложения/)).not.toBeInTheDocument();
    expect(document.getElementById('hot-deals')).toBeNull();

    await waitFor(() => {
      expect(getDestinations).toHaveBeenCalled();
    });
    expect(getTours).not.toHaveBeenCalled();
  });
});