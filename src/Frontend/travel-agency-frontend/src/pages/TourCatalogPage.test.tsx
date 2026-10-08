import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import TourCatalogPage from './TourCatalogPage';
import { getDestinations, getTours } from '@/api/catalog';
import { sampleTour } from '@/test/sampleTour';
import { renderPage } from '@/test/render';

vi.mock('@/api/catalog', () => ({
  getTours: vi.fn(),
  getDestinations: vi.fn(),
  getTourById: vi.fn(),
}));

const emptyPage = {
  items: [],
  total: 0,
  page: 1,
  pageSize: 6,
  totalPages: 0,
};

describe('TourCatalogPage', () => {
  beforeEach(() => {
    vi.mocked(getDestinations).mockResolvedValue([]);
    vi.mocked(getTours).mockReset();
  });

  it('renders published tour cards with cover, price from and lazy image', async () => {
    vi.mocked(getTours).mockResolvedValue({
      ...emptyPage,
      items: [
        sampleTour(),
        sampleTour({
          id: 'tour-2',
          title: 'Бали — остров богов',
          images: [
            {
              mediaFileId: '22222222-2222-2222-2222-222222222222',
              alt: 'Террасы',
              isCover: true,
              sortOrder: 0,
            },
          ],
          photos: ['/api/v1/media/files/22222222-2222-2222-2222-222222222222/w800'],
        }),
      ],
      total: 2,
    });

    renderPage(<TourCatalogPage />);

    expect(await screen.findByRole('heading', { name: 'Мальдивы — рай на земле' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Бали — остров богов' })).toBeInTheDocument();
    expect(screen.getAllByText(/от\s*289/).length).toBeGreaterThan(0);

    const cover = screen.getByRole('img', { name: 'Террасы' });
    expect(cover).toHaveAttribute('src', expect.stringContaining('/w800'));
    expect(cover.getAttribute('srcset')).toContain('w200');
    expect(cover.getAttribute('srcset')).toContain('w1600');
    expect(cover).toHaveAttribute('loading', 'lazy');
    expect(cover.getAttribute('src')).not.toContain('minio');
  });

  it('shows an error when the catalog API fails', async () => {
    vi.mocked(getTours).mockRejectedValue(new Error('network'));

    renderPage(<TourCatalogPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить каталог');
    expect(screen.queryByText('Туры не найдены')).not.toBeInTheDocument();
  });
});
