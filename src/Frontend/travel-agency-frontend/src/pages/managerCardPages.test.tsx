import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import MyBookingsPage from './MyBookingsPage';
import BookingDetailPage from './BookingDetailPage';
import ManagerBookingDetailPage from './ManagerBookingDetailPage';
import { getManagerCard } from '@/api/managerCard';
import { getMessages } from '@/api/chat';
import { useAuthStore } from '@/store/authStore';
import type { ChatMessage } from '@/types';
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
    vi.mocked(getMessages).mockResolvedValue([]);
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });
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

  it('кружок, строка и окно берут один src, свой кружок берёт аватар сессии', async () => {
    const user = userEvent.setup();
    const photo = '/api/v1/media/files/file-1/a256';
    const selfPhoto = '/api/v1/media/files/self/a256';
    useAuthStore.setState({
      user: {
        id: 'self-client',
        email: 'ilya@example.com',
        firstName: 'Илья',
        lastName: 'Петров',
        phone: '',
        avatar: selfPhoto,
        role: 'client',
        createdAt: '2026-01-01T00:00:00Z',
      },
      isAuthenticated: true,
      isLoading: false,
    });
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });
    vi.mocked(getMessages).mockResolvedValue([
      {
        id: 'own',
        bookingId: String(booking.id),
        senderId: 'self-client',
        senderName: 'ilya@example.com',
        senderRole: 'client',
        text: 'Я на месте',
        createdAt: '2026-04-02T08:00:00Z',
      },
      {
        id: 'marina',
        bookingId: String(booking.id),
        senderId: MANAGER_ID,
        senderName: 'marina@example.com',
        senderRole: 'manager',
        text: 'Здравствуйте',
        createdAt: '2026-04-02T08:01:00Z',
      },
    ] satisfies ChatMessage[]);

    renderAt(
      `/dashboard/bookings/${booking.id}`,
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    expect(await screen.findByRole('heading', { name: 'Чат с менеджером' })).toBeInTheDocument();
    const row = await screen.findByRole('button', { name: /Менеджер/ });
    const list = await screen.findByTestId('chat-message-list');
    expect(await within(row).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', photo);
    expect(await within(list).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', photo);
    expect(within(list).getByRole('img', { name: 'Илья Петров' })).toHaveAttribute('src', selfPhoto);
    expect(screen.getByText('Здравствуйте')).not.toHaveTextContent(photo);
    expect(within(list).queryByText('ilya@example.com')).not.toBeInTheDocument();
    expect(within(list).queryByText('marina@example.com')).not.toBeInTheDocument();

    await user.click(screen.getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' })[0]);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', photo);
  });
});

describe('session avatar on the manager booking chat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue([]);
    vi.mocked(getManagerCard).mockResolvedValue({
      id: MANAGER_ID,
      displayName: 'Марина Иванова',
      avatarUrl: '/api/v1/media/files/file-1/a256',
    });
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });
  });

  it('передаёт аватар сессии в свой кружок, не запрашивая карточку менеджера', async () => {
    const selfPhoto = '/api/v1/media/files/self/a256';
    useAuthStore.setState({
      user: {
        id: 'self-admin',
        email: 'admin@example.com',
        firstName: 'Админ',
        lastName: 'Админов',
        phone: '',
        avatar: selfPhoto,
        role: 'admin',
        createdAt: '2026-01-01T00:00:00Z',
      },
      isAuthenticated: true,
      isLoading: false,
    });
    installBooking({
      ...booking,
      assignedManagerId: MANAGER_ID,
      assignedManagerName: 'Марина Иванова',
    });
    vi.mocked(getMessages).mockResolvedValue([
      {
        id: 'own',
        bookingId: String(booking.id),
        senderId: 'self-admin',
        senderName: 'admin@example.com',
        senderRole: 'admin',
        text: 'Проверяю заявку',
        createdAt: '2026-04-02T08:00:00Z',
      },
    ] satisfies ChatMessage[]);

    renderAt(
      `/manager/bookings/${booking.id}`,
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );

    expect(await screen.findByRole('heading', { name: 'Чат с клиентом' })).toBeInTheDocument();
    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Админ Админов' })).toHaveAttribute('src', selfPhoto);
    expect(within(list).queryByText('admin@example.com')).not.toBeInTheDocument();
    expect(getManagerCard).not.toHaveBeenCalled();
  });
});
