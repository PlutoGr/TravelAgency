import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import TourCatalogPage from './TourCatalogPage';
import { getDestinations, getTours } from '@/api/catalog';
import { sampleTour } from '@/test/sampleTour';
import { renderPage } from '@/test/render';
import { mediaImageUrl } from '@/utils/media';
import type { Destination } from '@/types';

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
          coverMediaFileId: '22222222-2222-2222-2222-222222222222',
          images: [
            {
              mediaFileId: '22222222-2222-2222-2222-222222222222',
              alt: 'Террасы',
              isCover: true,
              sortOrder: 0,
            },
          ],
          photos: ['http://minio:9000/bucket/bali.jpg?X-Amz-Signature=abc'],
        }),
      ],
      total: 2,
    });

    renderPage(<TourCatalogPage />);

    expect(await screen.findByRole('heading', { name: 'Мальдивы — рай на земле' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Бали — остров богов' })).toBeInTheDocument();
    expect(screen.getAllByText(/от\s*289/).length).toBeGreaterThan(0);

    const cover = screen.getByRole('img', { name: 'Террасы' });
    expect(cover).toHaveAttribute('src', mediaImageUrl('22222222-2222-2222-2222-222222222222', 'w800'));
    expect(cover.getAttribute('srcset')).toContain('w200');
    expect(cover.getAttribute('srcset')).toContain('w1600');
    expect(cover).toHaveAttribute('loading', 'lazy');
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('X-Amz-');
    expect(document.body.innerHTML).not.toContain('unsplash');
  });

  it('shows an error when the catalog API fails', async () => {
    vi.mocked(getTours).mockRejectedValue(new Error('network'));

    renderPage(<TourCatalogPage />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Не удалось загрузить каталог');
    expect(alert).not.toHaveTextContent('network');
    expect(within(alert).getByRole('button', { name: 'Сбросить фильтры' })).toBeInTheDocument();
    expect(screen.queryByText('Туры не найдены')).not.toBeInTheDocument();
  });

  it('applies address filters, shows chips, and restores them after back from a tour', async () => {
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);
    vi.mocked(getTours).mockResolvedValue({
      ...emptyPage,
      items: [sampleTour()],
      total: 1,
      totalPages: 1,
    });

    renderCatalog('/tours?direction=dir-bali&dateFrom=2026-11-01');

    expect(await screen.findByRole('heading', { name: 'Мальдивы — рай на земле' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Бали' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'с 01.11.2026' })).toBeInTheDocument();
    await waitFor(() => {
      expect(getTours).toHaveBeenCalledWith(
        expect.objectContaining({ directionId: 'dir-bali', dateFrom: '2026-11-01' }),
        1,
      );
    });

    await user.click(screen.getByRole('button', { name: 'Сначала новые' }));
    await user.click(screen.getByRole('button', { name: 'Сначала дешевле' }));

    await waitFor(() => {
      expect(screen.getByTestId('url').textContent).toContain('sort=price_asc');
      expect(screen.getByTestId('url').textContent).toContain('direction=dir-bali');
      expect(screen.getByTestId('url').textContent).toContain('dateFrom=2026-11-01');
    });

    await user.click(screen.getByRole('link', { name: 'Подробнее' }));
    expect(await screen.findByRole('heading', { name: 'Карточка тура' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'История назад' }));

    expect(await screen.findByRole('heading', { name: 'Мальдивы — рай на земле' })).toBeInTheDocument();
    const url = screen.getByTestId('url').textContent ?? '';
    expect(url).toContain('direction=dir-bali');
    expect(url).toContain('dateFrom=2026-11-01');
    expect(url).toContain('sort=price_asc');
    await waitFor(() => {
      expect(getTours).toHaveBeenLastCalledWith(
        expect.objectContaining({
          directionId: 'dir-bali',
          dateFrom: '2026-11-01',
          sortBy: 'price_asc',
        }),
        1,
      );
    });
  });

  it('offers reset and a manager contact when the filtered catalog is empty', async () => {
    const user = userEvent.setup();
    vi.mocked(getTours).mockResolvedValue(emptyPage);

    renderCatalog('/tours?country=Атлантида');

    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('По вашему запросу туров нет');
    expect(within(status).getByRole('link', { name: 'Написать менеджеру' })).toHaveAttribute(
      'href',
      '/#contacts',
    );

    await user.click(within(status).getByRole('button', { name: 'Сбросить фильтры' }));

    await waitFor(() => {
      expect(screen.getByTestId('url').textContent).toBe('/tours');
    });
    expect(await screen.findByRole('heading', { name: 'Туры не найдены' })).toBeInTheDocument();
    expect(getTours).toHaveBeenLastCalledWith({}, 1);
  });

  it('shows date chips as dd.mm.yyyy and keeps the iso day in the address', async () => {
    vi.mocked(getTours).mockResolvedValue(emptyPage);

    renderCatalog('/tours?dateFrom=2026-11-01&dateTo=2026-11-15');

    expect(await screen.findByRole('button', { name: 'с 01.11.2026' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'по 15.11.2026' })).toBeInTheDocument();
    const url = screen.getByTestId('url').textContent ?? '';
    expect(url).toContain('dateFrom=2026-11-01');
    expect(url).toContain('dateTo=2026-11-15');
    expect(url).not.toContain('01.11.2026');
    await waitFor(() => {
      expect(getTours).toHaveBeenCalledWith(
        expect.objectContaining({ dateFrom: '2026-11-01', dateTo: '2026-11-15' }),
        1,
      );
    });
  });

  it('shows a hint and does not request tours when date from is after date to', async () => {
    vi.mocked(getTours).mockResolvedValue(emptyPage);

    renderCatalog('/tours?dateFrom=2026-12-01&dateTo=2026-11-01');

    expect(await screen.findAllByText('Дата «по» должна быть не раньше даты «с»')).not.toHaveLength(0);
    expect(screen.getAllByLabelText('По').some((input) => input.getAttribute('aria-invalid') === 'true')).toBe(true);
    expect(screen.getByRole('button', { name: 'с 01.12.2026' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'по 01.11.2026' })).toBeInTheDocument();
    const url = screen.getByTestId('url').textContent ?? '';
    expect(url).toContain('dateFrom=2026-12-01');
    expect(url).toContain('dateTo=2026-11-01');
    expect(getTours).not.toHaveBeenCalled();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});

const bali: Destination = {
  id: 'dir-bali',
  name: 'Бали',
  country: 'Индонезия',
  photo: '',
  tourCount: 2,
  description: '',
};

function HistoryProbe() {
  const location = useLocation();
  const navigate = useNavigate();
  return (
    <>
      <div data-testid="url">{`${location.pathname}${location.search}`}</div>
      <button type="button" onClick={() => navigate(-1)}>
        История назад
      </button>
    </>
  );
}

function renderCatalog(entry: string) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[entry]}>
        <HistoryProbe />
        <Routes>
          <Route path="/tours" element={<TourCatalogPage />} />
          <Route path="/tours/:id" element={<h1>Карточка тура</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}
