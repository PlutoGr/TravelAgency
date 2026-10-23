import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
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
  afterEach(() => {
    vi.useRealTimers();
  });

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

  it('marks empty reviews as not ready and keeps the reviews anchor', async () => {
    renderPage(<HomePage />);

    const section = document.getElementById('reviews');
    expect(section).not.toBeNull();
    expect(await screen.findByRole('heading', { name: 'Отзывы наших клиентов' })).toBeInTheDocument();
    expect(section).toHaveTextContent('Появится позже');
    expect(screen.queryByText('Скоро здесь появятся отзывы')).not.toBeInTheDocument();
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
    expect(screen.getByText('Проверенные турагентства')).toBeInTheDocument();
    expect(screen.getByText('Сравнение без лишних вкладок')).toBeInTheDocument();
    expect(screen.getByText('Связь с менеджером на сайте')).toBeInTheDocument();

    const text = document.body.textContent ?? '';
    const whyUs = document.getElementById('why-us')?.textContent ?? '';
    expect(whyUs).not.toMatch(/\d/);
    expect(text).not.toMatch(/Лучшие цены|Гарантия лучшей цены|лучшей цене/);
    expect(text).not.toMatch(/рейтинг/i);
    expect(text).not.toMatch(/лет на рынке/);
    expect(text).not.toMatch(/застрахованы/);
    expect(text).not.toMatch(/довольных клиентов/);
    expect(text).not.toMatch(/24\/7|круглосуточно/);
    expect(text).not.toMatch(/Бесплатная отмена/);
  });

  it('opens the catalog with a direction and departure date, without a travelers field', async () => {
    freezeCalendar('2026-10-23T09:00:00Z');
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);

    renderHome();

    expect(await screen.findByRole('link', { name: /Бали/ })).toHaveAttribute(
      'href',
      '/tours?direction=dir-bali',
    );
    expect(screen.queryByLabelText('Сколько?')).not.toBeInTheDocument();
    expect(document.querySelector('input[type="date"]')).toBeNull();

    await user.type(screen.getByLabelText('Куда?'), 'Бали');
    await pickHomeDay(user, '1 ноября 2026');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByRole('heading', { name: 'Каталог' })).toBeInTheDocument();
    expect(screen.getByTestId('url').textContent).toBe('/tours?direction=dir-bali&dateFrom=2026-11-01');
  });

  it('opens /tours without a query when both fields are empty', async () => {
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);
    renderHome();

    await screen.findByRole('button', { name: 'Найти' });
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByRole('heading', { name: 'Каталог' })).toBeInTheDocument();
    expect(screen.getByTestId('url').textContent).toBe('/tours');
  });

  it('adds dateTo for a departure range', async () => {
    freezeCalendar('2026-10-23T09:00:00Z');
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);
    renderHome();
    await screen.findByRole('link', { name: /Бали/ });

    await user.type(screen.getByLabelText('Куда?'), 'Бали');
    await pickHomeDay(user, '12 ноября 2026');
    await pickHomeDay(user, '20 ноября 2026');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByTestId('url')).toHaveTextContent(
      '/tours?direction=dir-bali&dateFrom=2026-11-12&dateTo=2026-11-20',
    );
  });

  it('writes equal dateFrom and dateTo when the same day is chosen twice', async () => {
    freezeCalendar('2026-10-23T09:00:00Z');
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);
    renderHome();
    await screen.findByRole('link', { name: /Бали/ });

    await pickHomeDay(user, '1 ноября 2026');
    await pickHomeDay(user, '1 ноября 2026');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByTestId('url')).toHaveTextContent(
      '/tours?dateFrom=2026-11-01&dateTo=2026-11-01',
    );
  });

  it('drops direction and country when the place is cleared', async () => {
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockResolvedValue([bali]);
    renderHome();
    await screen.findByRole('link', { name: /Бали/ });

    await user.type(screen.getByLabelText('Куда?'), 'Бали');
    await user.click(screen.getByRole('button', { name: /^Очистить$/ }));
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByRole('heading', { name: 'Каталог' })).toBeInTheDocument();
    expect(screen.getByTestId('url').textContent).toBe('/tours');
  });

  it('keeps a typed place when directions fail to load', async () => {
    const user = userEvent.setup();
    vi.mocked(getDestinations).mockRejectedValue(new Error('offline'));
    renderHome();

    await user.type(await screen.findByLabelText('Куда?'), 'Атлантида');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByTestId('url')).toHaveTextContent(
      `/tours?country=${encodeURIComponent('Атлантида')}`,
    );
  });
});

function freezeCalendar(isoUtc: string) {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(isoUtc));
}

async function pickHomeDay(user: ReturnType<typeof userEvent.setup>, name: string) {
  if (!screen.queryByRole('dialog', { name: 'Выбор дат' })) {
    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
  }
  await user.click(screen.getByRole('gridcell', { name }));
}

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