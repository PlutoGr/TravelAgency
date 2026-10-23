import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { getDestinations } from '@/api/catalog';
import { renderPage } from '@/test/render';
import type { Destination } from '@/types';
import DestinationsGrid from './DestinationsGrid';

vi.mock('@/api/catalog', () => ({
  getDestinations: vi.fn(),
}));

const emptyPhoto: Destination = {
  id: 'dir-bali',
  name: 'Бали',
  country: 'Индонезия',
  photo: '',
  tourCount: 0,
  description: '',
};

describe('DestinationsGrid', () => {
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
    vi.mocked(getDestinations).mockReset();
  });

  it('does not request an unsplash url when the direction photo is empty', async () => {
    vi.mocked(getDestinations).mockResolvedValue([emptyPhoto]);
    renderPage(<DestinationsGrid />);

    const link = await screen.findByRole('link', { name: /Бали/ });
    expect(link).toHaveAttribute('href', '/tours?direction=dir-bali');
    expect(screen.getByText('Индонезия')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Бали' })).toBeInTheDocument();
    expect(screen.queryByText(/туров/)).not.toBeInTheDocument();
    expect(screen.queryByText('Появится позже')).not.toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(document.body.innerHTML).not.toContain('unsplash');

    const placeholder = link.querySelector('div.bg-sand');
    expect(placeholder).toBeTruthy();
    expect(placeholder).toHaveClass('bg-sand');
    expect(link.querySelector('.from-dark\\/80')).toBeTruthy();
  });

  it('keeps a real photo and shows the tour count when both are set', async () => {
    vi.mocked(getDestinations).mockResolvedValue([
      {
        ...emptyPhoto,
        photo: 'https://cdn.example.com/bali.jpg',
        tourCount: 3,
      },
    ]);
    renderPage(<DestinationsGrid />);

    const img = await screen.findByRole('img', { name: 'Бали' });
    expect(img).toHaveAttribute('src', 'https://cdn.example.com/bali.jpg');
    expect(screen.getByRole('link', { name: /Бали/ })).toHaveAttribute(
      'href',
      '/tours?direction=dir-bali',
    );
    expect(screen.getByText('3 туров')).toBeInTheDocument();
    expect(document.body.innerHTML).not.toContain('unsplash');
  });
});
