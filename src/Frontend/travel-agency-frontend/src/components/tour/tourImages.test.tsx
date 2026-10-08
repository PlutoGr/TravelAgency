import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import TourCard from './TourCard';
import TourGallery from './TourGallery';
import FavoritesPage from '@/pages/FavoritesPage';
import { getFavorites } from '@/api/favorites';
import { sampleTour } from '@/test/sampleTour';
import { renderPage } from '@/test/render';
import { mediaImageUrl } from '@/utils/media';

vi.mock('@/api/favorites', () => ({
  getFavorites: vi.fn(),
  addFavorite: vi.fn(),
  removeFavorite: vi.fn(),
}));

const FILE = '11111111-1111-1111-1111-111111111111';

function poisonedTour() {
  return sampleTour({
    photos: [
      'http://minio:9000/bucket/a.jpg?X-Amz-Signature=abc',
      'https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800',
    ],
    images: [
      {
        mediaFileId: FILE,
        alt: 'Вилла',
        isCover: true,
        sortOrder: 0,
      },
    ],
  });
}

describe('tour images come from file ids', () => {
  it('renders a catalog card from coverMediaFileId and ignores url, photos and imageUrl', () => {
    const tour = poisonedTour();
    renderPage(<TourCard tour={{ ...tour, imageUrl: tour.photos[0], url: tour.photos[0] } as typeof tour} />);

    const cover = screen.getByRole('img', { name: 'Вилла' });
    expect(cover).toHaveAttribute('src', mediaImageUrl(FILE, 'w800'));
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('X-Amz-');
    expect(document.body.innerHTML).not.toContain('unsplash');
  });

  it('shows a placeholder when the tour has no media file', () => {
    renderPage(
      <TourCard
        tour={sampleTour({
          coverMediaFileId: null,
          images: [],
          photos: ['https://images.unsplash.com/photo-1'],
        })}
      />,
    );

    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(document.body.innerHTML).not.toContain('unsplash');
  });

  it('renders the tour page gallery from media file ids', () => {
    const tour = poisonedTour();
    renderPage(<TourGallery images={tour.images} title={tour.title} />);

    expect(screen.getByRole('img', { name: 'Вилла' })).toHaveAttribute(
      'src',
      mediaImageUrl(FILE, 'w1600'),
    );
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('unsplash');
  });

  it('renders a favorite card from the file id', async () => {
    vi.mocked(getFavorites).mockResolvedValue([poisonedTour()]);
    renderPage(<FavoritesPage />);

    const cover = await screen.findByRole('img', { name: 'Вилла' });
    expect(cover).toHaveAttribute('src', mediaImageUrl(FILE, 'w800'));
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('X-Amz-');
    expect(document.body.innerHTML).not.toContain('unsplash');
  });
});
