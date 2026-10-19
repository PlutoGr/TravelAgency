import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import HomePage from './HomePage';
import { getDestinations, getTours } from '@/api/catalog';
import { renderPage } from '@/test/render';
import type { Destination } from '@/types';

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

  it('does not promise best prices, 24/7 support or free cancellation', async () => {
    renderPage(<HomePage />);

    expect(await screen.findByRole('heading', { name: 'Почему выбирают нас' })).toBeInTheDocument();
    expect(screen.getByText('Проверенные отели')).toBeInTheDocument();
    expect(screen.getByText('Надёжность')).toBeInTheDocument();

    const text = document.body.textContent ?? '';
    expect(text).not.toMatch(/Лучшие цены|Гарантия лучшей цены/);
    expect(text).not.toMatch(/24\/7|круглосуточно/);
    expect(text).not.toMatch(/Бесплатная отмена/);
  });

  it('opens the catalog with a direction and departure date, without a travelers field', async () => {
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);

    renderHome();

    expect(await screen.findByRole('link', { name: /Бали/ })).toHaveAttribute(
      'href',
      '/tours?direction=dir-bali',
    );
    expect(screen.queryByLabelText('Сколько?')).not.toBeInTheDocument();

    await user.type(screen.getByLabelText('Куда?'), 'Бали');
    fireEvent.change(screen.getByLabelText('Когда?'), { target: { value: '2026-11-01' } });
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByRole('heading', { name: 'Каталог' })).toBeInTheDocument();
    expect(screen.getByTestId('url').textContent).toBe('/tours?direction=dir-bali&dateFrom=2026-11-01');
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

function UrlProbe() {
  const location = useLocation();
  return <div data-testid="url">{`${location.pathname}${location.search}`}</div>;
}

function renderHome() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/']}>
        <UrlProbe />
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/tours" element={<h1>Каталог</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}