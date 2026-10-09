import { beforeEach, describe, expect, it, vi } from 'vitest';
import { type ReactElement } from 'react';
import { cleanup, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import MyBookingsPage from './MyBookingsPage';
import BookingDetailPage from './BookingDetailPage';
import ManagerBookingsPage from './ManagerBookingsPage';
import ManagerBookingDetailPage from './ManagerBookingDetailPage';
import {
  BOOKING_MINIMAL_JSON,
  BOOKING_WITH_PROPOSAL_JSON,
  TOUR_CARD_JSON,
  parseBookingJson,
} from '@/test/bookingDtoJson';
import { mediaImageUrl } from '@/utils/media';

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

const bookingWithProposal = parseBookingJson(BOOKING_WITH_PROPOSAL_JSON);
const bookingMinimal = parseBookingJson(BOOKING_MINIMAL_JSON);
const tourCard = parseBookingJson(TOUR_CARD_JSON);

function renderAt(path: string, routePath: string, element: ReactElement) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path={routePath} element={element} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('booking pages with BookingDto JSON', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiClient.get.mockImplementation((url: string) => {
      const path = String(url);
      if (path.startsWith('/catalog/tours/cards')) {
        return Promise.resolve({ data: [tourCard] });
      }
      if (path === '/bookings/my' || path === '/bookings') {
        return Promise.resolve({ data: [bookingWithProposal, bookingMinimal] });
      }
      if (path.startsWith('/bookings/')) {
        return Promise.resolve({ data: bookingWithProposal });
      }
      return Promise.resolve({ data: [] });
    });
  });

  it('renders the client list from real booking JSON', async () => {
    renderAt('/dashboard/bookings', '/dashboard/bookings', <MyBookingsPage />);

    expect(await screen.findByText('Солнечная Греция — Санторини')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Солнечная Греция — Санторини' })).toHaveAttribute(
      'src',
      mediaImageUrl('55555555-5555-5555-5555-555555555555', 'w200'),
    );
    expect(screen.getByText('Тур недоступен')).toBeInTheDocument();
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.getByText('Предложение отправлено')).toBeInTheDocument();
    expect(screen.getByText('Обновлено: —')).toBeInTheDocument();
  });

  it('renders the client booking detail from real booking JSON', async () => {
    renderAt(
      '/dashboard/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    expect(await screen.findByRole('heading', { name: /Заявка #/ })).toBeInTheDocument();
    expect(screen.getAllByText('Солнечная Греция — Санторини').length).toBeGreaterThan(0);
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.getByText('Санторини')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Подтвердить предложение' })).toBeInTheDocument();
    expect(screen.getByText('Чат с менеджером')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Солнечная Греция — Санторини' })).toHaveAttribute(
      'src',
      mediaImageUrl('55555555-5555-5555-5555-555555555555', 'w800'),
    );
  });

  it('renders the manager list from real booking JSON', async () => {
    renderAt('/manager/bookings', '/manager/bookings', <ManagerBookingsPage />);

    expect(await screen.findAllByText('Анна Смирнова')).not.toHaveLength(0);
    expect(screen.getAllByText('Солнечная Греция — Санторини').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Предложение отправлено').length).toBeGreaterThan(0);
  });

  it('renders the manager booking detail from real booking JSON', async () => {
    renderAt(
      '/manager/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );

    expect(await screen.findByText('Анна Смирнова')).toBeInTheDocument();
    expect(screen.getAllByText('Солнечная Греция — Санторини').length).toBeGreaterThan(0);
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.getByText('anna@example.com')).toBeInTheDocument();
    expect(screen.getByText('Чат с клиентом')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Солнечная Греция — Санторини' })).toHaveAttribute(
      'src',
      mediaImageUrl('55555555-5555-5555-5555-555555555555', 'w800'),
    );
  });
});

describe('bookings stay visible when catalog cards fail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  function bookingsOkCardsFail() {
    mockApiClient.get.mockImplementation((url: string) => {
      const path = String(url);
      if (path.startsWith('/catalog/tours/cards')) {
        return Promise.reject(new Error('catalog down'));
      }
      if (path === '/bookings/my' || path === '/bookings') {
        return Promise.resolve({ data: [bookingWithProposal, bookingMinimal] });
      }
      if (path.startsWith('/bookings/')) {
        return Promise.resolve({ data: bookingWithProposal });
      }
      return Promise.resolve({ data: [] });
    });
  }

  it('renders the client list with «Тур недоступен» when the catalog rejects', async () => {
    bookingsOkCardsFail();
    renderAt('/dashboard/bookings', '/dashboard/bookings', <MyBookingsPage />);

    expect(await screen.findAllByText('Тур недоступен')).toHaveLength(2);
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.queryByText('Солнечная Греция — Санторини')).not.toBeInTheDocument();
  });

  it('renders the client detail with «Тур недоступен» when the catalog rejects', async () => {
    bookingsOkCardsFail();
    renderAt(
      '/dashboard/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    expect(await screen.findByRole('heading', { name: /Заявка #/ })).toBeInTheDocument();
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.queryByText('Солнечная Греция — Санторини')).not.toBeInTheDocument();
  });

  it('renders the manager list with «Тур недоступен» when the catalog rejects', async () => {
    bookingsOkCardsFail();
    renderAt('/manager/bookings', '/manager/bookings', <ManagerBookingsPage />);

    expect(await screen.findAllByText('Анна Смирнова')).not.toHaveLength(0);
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
    expect(screen.queryByText('Солнечная Греция — Санторини')).not.toBeInTheDocument();
  });

  it('renders the manager detail with «Тур недоступен» when the catalog rejects', async () => {
    bookingsOkCardsFail();
    renderAt(
      '/manager/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );

    expect(await screen.findByText('Анна Смирнова')).toBeInTheDocument();
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
    expect(screen.getByText('Хочу поехать в мае, двое взрослых')).toBeInTheDocument();
    expect(screen.queryByText('Солнечная Греция — Санторини')).not.toBeInTheDocument();
  });

  it('shows «Тур недоступен» only for ids missing from a partial catalog result', async () => {
    mockApiClient.get.mockImplementation((url: string) => {
      const path = String(url);
      if (path.startsWith('/catalog/tours/cards')) {
        return Promise.resolve({ data: [tourCard] });
      }
      if (path === '/bookings/my' || path === '/bookings') {
        return Promise.resolve({ data: [bookingWithProposal, bookingMinimal] });
      }
      return Promise.resolve({ data: [] });
    });

    renderAt('/dashboard/bookings', '/dashboard/bookings', <MyBookingsPage />);
    expect(await screen.findByText('Солнечная Греция — Санторини')).toBeInTheDocument();
    expect(screen.getByText('Тур недоступен')).toBeInTheDocument();

    cleanup();

    renderAt('/manager/bookings', '/manager/bookings', <ManagerBookingsPage />);
    expect(await screen.findAllByText('Солнечная Греция — Санторини')).not.toHaveLength(0);
    expect(screen.getAllByText('Тур недоступен').length).toBeGreaterThan(0);
  });
});
