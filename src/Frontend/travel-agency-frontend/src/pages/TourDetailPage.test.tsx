import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import TourDetailPage from './TourDetailPage';
import { createBooking } from '@/api/bookings';
import { getTourById, getTours } from '@/api/catalog';
import { addFavorite } from '@/api/favorites';
import { resumeFavoriteIntent } from '@/store/authIntent';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';
import { sampleTour } from '@/test/sampleTour';
import { formatDateRange, formatFromMoney, formatMoney } from '@/utils/format';

vi.mock('@/api/catalog', () => ({
  getTourById: vi.fn(),
  getTours: vi.fn(),
  getDestinations: vi.fn(),
}));

vi.mock('@/api/bookings', () => ({
  createBooking: vi.fn(),
}));

vi.mock('@/api/favorites', () => ({
  addFavorite: vi.fn().mockResolvedValue(undefined),
  removeFavorite: vi.fn().mockResolvedValue(undefined),
  getFavorites: vi.fn().mockResolvedValue([]),
}));

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

function renderDetail() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/tours/tour-1']}>
        <Routes>
          <Route path="/tours/:id" element={<TourDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function flat(value: string | null | undefined) {
  return (value ?? '').replace(/[\u00a0\u202f]/g, ' ');
}

describe('TourDetailPage', () => {
  beforeEach(() => {
    vi.mocked(getTours).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      pageSize: 6,
      totalPages: 0,
    });
    vi.mocked(createBooking).mockReset();
    vi.mocked(createBooking).mockResolvedValue({ id: 'booking-42' } as Awaited<ReturnType<typeof createBooking>>);
    vi.mocked(addFavorite).mockClear();
    useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: null,
      isMobileMenuOpen: false,
    });
    useFavoritesStore.setState({
      favoriteIds: [],
      favoriteTours: [],
      isLoading: false,
      error: null,
      count: 0,
    });
  });

  it('shows gallery, program, inclusions, stay and offers, then opens a request', async () => {
    vi.mocked(getTourById).mockResolvedValue(sampleTour());
    const user = userEvent.setup();

    renderDetail();

    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Прилёт' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Включено' })).toBeInTheDocument();
    expect(screen.getByText('Проживание и питание по программе')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Не включено' })).toBeInTheDocument();
    expect(screen.getByText('Личные расходы')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Проживание' })).toBeInTheDocument();
    expect(screen.getByText('Водная вилла, всё включено')).toBeInTheDocument();
    expect(screen.getByText('Всё включено')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Предложения' })).toBeInTheDocument();

    const gallery = screen.getByRole('img', { name: 'Вилла' });
    expect(gallery).toHaveAttribute('src', expect.stringContaining('/w1600'));
    expect(gallery.getAttribute('srcset')).toContain('w800');
    expect(gallery).toHaveAttribute('loading', 'eager');

    await user.click(screen.getAllByRole('button', { name: 'Оставить заявку' })[0]);
    expect(await screen.findByRole('heading', { name: 'Оформить заявку' })).toBeInTheDocument();
  });

  it('shows not found when the tour API returns 404', async () => {
    vi.mocked(getTourById).mockRejectedValue(new Error('Тур с ID "tour-1" не найден'));

    renderDetail();

    expect(await screen.findByRole('heading', { name: 'Тур не найден' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Оставить заявку' })).not.toBeInTheDocument();
  });

  it('shows an error when the tour API fails', async () => {
    vi.mocked(getTourById).mockRejectedValue(new Error('Network'));

    renderDetail();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить тур');
  });

  it('clears a deferred request when the tour fails to load', async () => {
    vi.mocked(getTourById).mockRejectedValue(new Error('Network'));
    useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: { type: 'request', tourId: 'tour-1', offerId: 'offer-1' },
      isMobileMenuOpen: false,
    });

    renderDetail();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить тур');
    await waitFor(() => {
      expect(useUIStore.getState().authIntent).toBeNull();
    });
    expect(screen.queryByRole('heading', { name: 'Оформить заявку' })).not.toBeInTheDocument();
  });

  it('opens the request on the selected date after login and links to the booking', async () => {
    const user = userEvent.setup();
    const first = sampleTour().offers![0];
    const second = {
      id: 'offer-2',
      start: '2026-12-01T00:00:00Z',
      end: '2026-12-10T00:00:00Z',
      price: 1500,
      currency: 'EUR',
      seats: 3,
    };
    const tour = sampleTour({ price: 0, priceFrom: 100, currency: 'RUB', offers: [first, second] });
    vi.mocked(getTourById).mockResolvedValue(tour);
    useAuthStore.setState({ isAuthenticated: false, user: null, isLoading: false });

    renderDetail();

    await user.click(
      await screen.findByRole('button', {
        name: (name) => flat(name).includes(flat(formatMoney(1500, 'EUR'))),
      }),
    );
    await user.click(screen.getAllByRole('button', { name: 'Оставить заявку' })[0]);

    expect(useUIStore.getState().isAuthModalOpen).toBe(true);
    expect(useUIStore.getState().authIntent).toEqual({
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-2',
    });
    expect(screen.queryByRole('heading', { name: 'Оформить заявку' })).not.toBeInTheDocument();

    await act(async () => {
      useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
      useUIStore.getState().closeAuthModal();
    });

    expect(await screen.findByRole('heading', { name: 'Оформить заявку' })).toBeInTheDocument();
    expect(screen.queryByText('Выбор тура')).not.toBeInTheDocument();
    expect(screen.getByText('Укажите удобные даты и сколько человек поедет.')).toBeInTheDocument();
    expect(screen.getByRole('textbox')).toHaveValue(
      `Хочу поехать ${formatDateRange(second.start, second.end)}, ${formatMoney(1500, 'EUR')} за человека.`,
    );

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    const priceText = flat(screen.getByText('Цена').parentElement?.textContent);
    expect(priceText).toContain(flat(formatMoney(1500, 'EUR')));
    expect(priceText).not.toContain('₽');
    expect(priceText).not.toContain('от');

    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));
    expect(await screen.findByRole('link', { name: 'Перейти к заявке' })).toHaveAttribute(
      'href',
      '/dashboard/bookings/booking-42',
    );
    expect(screen.getByText('Заявка отправлена. Менеджер ответит в чате заявки, обычно в течение рабочего дня')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Продолжить просмотр' }));
    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: 'Оформить заявку' })).not.toBeInTheDocument();
    });
  });

  it('shows «от» price when the tour has no dates', async () => {
    const user = userEvent.setup();
    vi.mocked(getTourById).mockResolvedValue(
      sampleTour({ offers: [], price: 0, priceFrom: 42000, currency: 'EUR' }),
    );

    renderDetail();
    await user.click((await screen.findAllByRole('button', { name: 'Оставить заявку' }))[0]);
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();

    expect(flat(screen.getByText('Цена').parentElement?.textContent)).toContain(
      flat(formatFromMoney(42000, 'EUR')),
    );
  });

  it('adds the tour to favorites after login when the guest clicked the heart', async () => {
    const user = userEvent.setup();
    vi.mocked(getTourById).mockResolvedValue(sampleTour());
    useAuthStore.setState({ isAuthenticated: false, user: null, isLoading: false });

    renderDetail();
    await user.click(await screen.findByRole('button', { name: 'Добавить в избранное' }));

    expect(useUIStore.getState().authIntent).toMatchObject({ type: 'favorite', tourId: 'tour-1' });
    expect(addFavorite).not.toHaveBeenCalled();

    await act(async () => {
      useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
      useUIStore.getState().closeAuthModal();
    });
    await act(async () => {
      resumeFavoriteIntent();
    });

    expect(addFavorite).toHaveBeenCalledWith('tour-1');
    expect(screen.getByRole('button', { name: 'В избранном' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Оформить заявку' })).not.toBeInTheDocument();
  });

  it('does not open the request if the guest closes the login window', async () => {
    const user = userEvent.setup();
    vi.mocked(getTourById).mockResolvedValue(sampleTour());
    useAuthStore.setState({ isAuthenticated: false, user: null, isLoading: false });

    renderDetail();
    await user.click((await screen.findAllByRole('button', { name: 'Оставить заявку' }))[0]);
    await act(async () => {
      useUIStore.getState().dismissAuthModal();
      useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
    });

    expect(screen.queryByRole('heading', { name: 'Оформить заявку' })).not.toBeInTheDocument();
  });
});
