import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import TourCard from './TourCard';
import TourGallery from './TourGallery';
import ResponsiveTourImage from './ResponsiveTourImage';
import FavoritesPage from '@/pages/FavoritesPage';
import { getFavorites } from '@/api/favorites';
import { sampleTour } from '@/test/sampleTour';
import { renderPage } from '@/test/render';
import { mediaImageUrl } from '@/utils/media';
import type { TourImage } from '@/types';

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

  it('shows the departure day from a calendar date', () => {
    renderPage(<TourCard tour={sampleTour({ nearestDate: '2026-11-02' })} />);
    expect(screen.getByText(/ближайший выезд 2 ноя/)).toBeInTheDocument();
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

const GALLERY_FILES = ['a', 'b', 'c', 'd'].map(
  (suffix, index) => `22222222-2222-4222-8222-00000000000${suffix}`,
);

function galleryImages(): TourImage[] {
  return GALLERY_FILES.map((mediaFileId, index) => ({
    mediaFileId,
    alt: `Кадр ${index + 1}`,
    isCover: index === 0,
    sortOrder: index,
  }));
}

function thumbButtons() {
  return GALLERY_FILES.map((_, index) => screen.getByRole('button', { name: `Кадр ${index + 1}` }));
}

describe('tour gallery frame', () => {
  it('fits the main photo in a 16:9 frame without covering the edges', () => {
    renderPage(<TourGallery images={galleryImages()} title="Дубай" />);

    const photo = screen.getByRole('img', { name: 'Кадр 1' });
    expect(photo).toHaveAttribute('width', '1600');
    expect(photo).toHaveAttribute('height', '900');
    expect(photo).toHaveClass('object-contain');
    expect(photo).not.toHaveClass('object-cover');

    const frame = photo.parentElement?.parentElement;
    expect(frame).toHaveClass('aspect-[16/9]');
    expect(frame).not.toHaveClass('aspect-[16/10]');
  });

  it('uses the same 16:9 frame when the tour has no photos', () => {
    renderPage(<TourGallery images={[]} title="Пусто" />);
    expect(screen.getByText('Фото пока нет')).toHaveClass('aspect-[16/9]');
  });

  it('keeps the selected thumbnail ring inside the strip on every thumb', () => {
    renderPage(<TourGallery images={galleryImages()} title="Дубай" />);

    const thumbs = thumbButtons();
    expect(thumbs).toHaveLength(4);
    thumbs.forEach((thumb) => {
      expect(thumb.querySelector('img')).toBeTruthy();
      expect(thumb).toHaveClass('h-16', 'w-20', 'flex-shrink-0');
      expect(thumb.className).not.toMatch(/\bborder(?:-\d)?\b/);
    });

    const strip = thumbs[0].parentElement;
    expect(strip).toHaveClass('overflow-x-auto', 'p-1');
    expect(strip).not.toHaveClass('pb-1');

    const selected = screen.getByRole('button', { current: true });
    expect(selected).toHaveClass('ring-2', 'ring-primary', 'ring-offset-2');
    expect(selected).toBe(thumbs[0]);

    fireEvent.click(thumbs[1]);

    expect(thumbs[1]).toHaveAttribute('aria-current', 'true');
    expect(thumbs[0]).not.toHaveAttribute('aria-current', 'true');
    expect(thumbs[1]).toHaveClass('h-16', 'w-20', 'ring-2', 'ring-offset-2');
    expect(thumbs[0]).not.toHaveClass('ring-2');
    expect(screen.getByRole('button', { name: /2\s*\/\s*4/ })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Кадр 2' })).toHaveClass('object-contain');
  });

  it('shows a sand placeholder when a gallery image fails to load', () => {
    renderPage(<TourGallery images={galleryImages()} title="Дубай" />);

    const thumbs = thumbButtons();
    fireEvent.error(thumbs[1].querySelector('img')!);

    expect(thumbs[1].querySelector('img')).toBeNull();
    expect(thumbs[1].querySelector('div.bg-sand')).toBeTruthy();
    expect(thumbs[0].querySelector('img')).toBeTruthy();
    expect(thumbs[2].querySelector('img')).toBeTruthy();
    expect(thumbs[3].querySelector('img')).toBeTruthy();
    expect(screen.getByRole('img', { name: 'Кадр 1' })).toBeInTheDocument();
  });
});

describe('ResponsiveTourImage load errors', () => {
  it('replaces a broken file with a placeholder and retries the next file', () => {
    const nextFile = '33333333-3333-4333-8333-333333333333';
    const view = render(
      <ResponsiveTourImage mediaFileId={FILE} alt="Вилла" variant="gallery" className="h-full w-full object-contain" />,
    );

    fireEvent.error(screen.getByRole('img', { name: 'Вилла' }));

    expect(view.container.querySelector('img')).toBeNull();
    const placeholder = view.container.querySelector('div.bg-sand');
    expect(placeholder).toHaveClass('h-full', 'w-full', 'bg-sand');
    expect(placeholder).toHaveAttribute('aria-hidden', 'true');

    view.rerender(
      <ResponsiveTourImage mediaFileId={nextFile} alt="Вилла" variant="gallery" className="h-full w-full object-contain" />,
    );

    expect(view.container.querySelector('img')).toHaveAttribute('src', mediaImageUrl(nextFile, 'w1600'));
  });
});
