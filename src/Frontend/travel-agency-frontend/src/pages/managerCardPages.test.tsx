import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import MyBookingsPage from './MyBookingsPage';
import BookingDetailPage from './BookingDetailPage';
import ManagerBookingDetailPage from './ManagerBookingDetailPage';
import { getManagerCard } from '@/api/managerCard';
import { BOOKING_WITH_PROPOSAL_JSON, TOUR_CARD_JSON, parseBookingJson } from '@/test/bookingDtoJson';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}));

vi.mock('@/api/client', () => ({
  apiClient: mockApiClient,
}));

vi.mock('@/api/chat', () => ({
  getMessages: vi.fn().mockResolvedValue([]),
  sendMessage: vi.fn(),
  subscribeToMessages: vi.fn(() => () => {}),
  disconnectChat: vi.fn(),
}));

vi.mock('@/api/managerCard', () => ({
  getManagerCard: vi.fn(),
}));

const booking = parseBookingJson(BOOKING_WITH_PROPOSAL_JSON) as Record<string, unknown>;
const tourCard = parseBookingJson(TOUR_CARD_JSON);
const MANAGER_ID = '44444444-4444-4444-4444-444444444444';

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function renderAt(path: string, routePath: string, element: ReactElement) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <Routes>
        <Route path={routePath} element={element} />
      </Routes>
    </MemoryRouter>,
  );
}

function installBooking(body: Record<string, unknown>) {
  mockApiClient.get.mockImplementation((url: string) => {
    const path = String(url);
    if (path.startsWith('/catalog/tours/cards')) return Promise.resolve({ data: [tourCard] });
    if (path === '/bookings/my' || path === '/bookings') return Promise.resolve({ data: [body] });
    if (path.startsWith('/bookings/')) return Promise.resolve({ data: body });
    return Promise.resolve({ data: [] });
  });
}

describe('manager row on booking pages', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getManagerCard).mockResolvedValue({
      id: MANAGER_ID,
      displayName: 'Марина Иванова',
      avatarUrl: '/api/v1/media/files/file-1/a256',
    });
  });

  it('строка менеджера есть, пока есть id, и повторный клик не шлёт новый GET', async () => {
    const user = userEvent.setup();
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });
    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const row = await screen.findByRole('button', { name: /Менеджер/ });
    expect(within(row).getByText('Марина Иванова')).toBeInTheDocument();
    expect(await within(row).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute(
      'src',
      '/api/v1/media/files/file-1/a256',
    );
    expect(getManagerCard).toHaveBeenCalledTimes(1);
    expect(getManagerCard).toHaveBeenCalledWith(MANAGER_ID);

    await user.click(row);
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(within(screen.getByRole('dialog')).getByText('Марина Иванова')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Закрыть' }));
    await user.click(screen.getByRole('button', { name: /Менеджер/ }));
    expect(getManagerCard).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('location').textContent).toBe(`/dashboard/bookings/${booking.id}`);
  });

  it('без имени в заявке строка остаётся, имя берётся из карточки', async () => {
    const user = userEvent.setup();
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: null,
    });
    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const row = await screen.findByRole('button', { name: /Менеджер/ });
    expect(within(row).queryByText('Марина Иванова')).not.toBeInTheDocument();
    await user.click(row);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByText('Марина Иванова')).toBeInTheDocument();
  });

  it('пустой id не даёт строки и не вызывает GET', async () => {
    installBooking({ ...booking, assignedManagerId: null, assignedManagerName: null });
    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    expect(await screen.findByText('Чат с менеджером')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Менеджер/ })).not.toBeInTheDocument();
    expect(getManagerCard).not.toHaveBeenCalled();
  });

  it('упавший предварительный запрос не блокирует заявку', async () => {
    const user = userEvent.setup();
    vi.mocked(getManagerCard).mockRejectedValue(new Error('network'));
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });
    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const row = await screen.findByRole('button', { name: /Менеджер/ });
    expect(screen.getByRole('heading', { name: /Заявка #/ })).toBeInTheDocument();
    expect(within(row).getByText('Марина Иванова')).toBeInTheDocument();
    expect(within(row).getByText('МИ')).toBeInTheDocument();
    expect(within(row).queryByRole('img')).not.toBeInTheDocument();

    await waitFor(() => expect(getManagerCard).toHaveBeenCalledTimes(1));
    await user.click(row);
    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    expect(getManagerCard).toHaveBeenCalledTimes(2);
    expect(screen.getByRole('heading', { name: /Заявка #/ })).toBeInTheDocument();
  });

  it('после ошибки предварительного запроса следующее открытие повторяет GET', async () => {
    const user = userEvent.setup();
    vi.mocked(getManagerCard)
      .mockRejectedValueOnce(new Error('network'))
      .mockResolvedValue({
        id: MANAGER_ID,
        displayName: 'Марина Иванова',
        avatarUrl: '/api/v1/media/files/file-1/a256',
      });
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });
    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const row = await screen.findByRole('button', { name: /Менеджер/ });
    await waitFor(() => expect(getManagerCard).toHaveBeenCalledTimes(1));
    await user.click(row);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByText('Марина Иванова')).toBeInTheDocument();
    expect(getManagerCard).toHaveBeenCalledTimes(2);

    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    await user.click(row);
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(getManagerCard).toHaveBeenCalledTimes(2);
  });

  it('в списке и на заявке менеджера отдельной строки нет', async () => {
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });

    const list = renderAt('/dashboard/bookings', '/dashboard/bookings', <MyBookingsPage />);
    expect(await screen.findByText('Солнечная Греция — Санторини')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Менеджер/ })).not.toBeInTheDocument();
    expect(getManagerCard).not.toHaveBeenCalled();
    const calls = mockApiClient.get.mock.calls.map((call) => String(call[0]));
    expect(calls.some((url) => url.includes('manager-card'))).toBe(false);
    list.unmount();

    renderAt(
      `/manager/bookings/${booking.id}`,
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );
    expect(await screen.findByText('Чат с клиентом')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Менеджер/ })).not.toBeInTheDocument();
    expect(getManagerCard).not.toHaveBeenCalled();
  });
});
