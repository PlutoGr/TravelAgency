import { beforeEach, describe, expect, it, vi } from 'vitest';
import { type ReactElement } from 'react';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
import { getMessages } from '@/api/chat';
import type { ChatMessage } from '@/types';

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

function catalogTourTitle(): string {
  return (tourCard as { title: string }).title;
}

async function tourPreview(selector: string) {
  const titles = await screen.findAllByText(catalogTourTitle());
  const card = titles
    .map((node) => node.closest(selector))
    .find((node): node is HTMLElement => node instanceof HTMLElement);
  expect(card).toBeTruthy();
  return card as HTMLElement;
}

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

  it('does not show a zero rating on the client booking', async () => {
    renderAt(
      '/dashboard/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const card = await tourPreview('div.p-4');
    expect(card).toHaveTextContent('Появится позже');
    expect(card).toHaveTextContent(catalogTourTitle());
    expect(card).toHaveTextContent('Кальдера и белые дома');
    expect(card).not.toHaveTextContent('(0 отзывов)');
    expect(card.querySelector('.text-amber-400, .fill-amber-400')).toBeNull();
  });

  it('does not show a filled star for an empty rating on the manager booking', async () => {
    renderAt(
      '/manager/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );

    const card = await tourPreview('div.p-3');
    expect(card).toHaveTextContent('Появится позже');
    expect(card).toHaveTextContent(catalogTourTitle());
    expect(card).toHaveTextContent('Греция');
    expect(card).toHaveTextContent(/148/);
    expect(card).not.toHaveTextContent('(0 отзывов)');
    expect(card.querySelector('.text-amber-400, .fill-amber-400')).toBeNull();
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

function longChatHistory(): ChatMessage[] {
  return Array.from({ length: 40 }, (_, index) => {
    const fromManager = index % 2 === 0;
    return {
      id: `msg-${index + 1}`,
      bookingId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      senderId: fromManager ? 'manager-1' : 'client-1',
      senderName: fromManager ? 'Марина' : 'Илья',
      senderRole: fromManager ? 'manager' : 'client',
      text: `Сообщение ${index + 1}`,
      createdAt: new Date(Date.UTC(2026, 3, 2, 8, 0, index)).toISOString(),
    };
  });
}

function expectPinnedChatInput(heading: HTMLElement) {
  const input = screen.getByPlaceholderText('Введите сообщение...');
  const list = screen.getByTestId('chat-message-list');
  const column = list.parentElement;
  const shell = column?.parentElement;

  expect(list).toHaveClass('min-h-0', 'flex-1', 'overflow-y-auto');
  expect(list).toContainElement(screen.getByText('Сообщение 1'));
  expect(list).toContainElement(screen.getByText('Сообщение 40'));
  expect(list).not.toContainElement(input);
  expect(column).toHaveClass('flex', 'min-h-0', 'flex-col');
  expect(column).toContainElement(input);
  expect(shell).toHaveClass('flex', 'min-h-0', 'flex-1', 'flex-col', 'overflow-hidden');
  expect(shell?.parentElement).toContainElement(heading);
  expect(shell?.parentElement).toHaveClass('flex', 'flex-col', 'overflow-hidden');
}

describe('chat input stays pinned under a long history', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue(longChatHistory());
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

  it('scrolls the client booking chat inside its list', async () => {
    renderAt(
      '/dashboard/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/dashboard/bookings/:id',
      <BookingDetailPage />,
    );

    const heading = await screen.findByRole('heading', { name: 'Чат с менеджером' });
    await screen.findByText('Сообщение 40');
    expectPinnedChatInput(heading);
  });

  it('scrolls the manager booking chat inside its list', async () => {
    renderAt(
      '/manager/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );

    const heading = await screen.findByRole('heading', { name: 'Чат с клиентом' });
    await screen.findByText('Сообщение 40');
    expectPinnedChatInput(heading);
  });
});

const MINIMAL_BOOKING_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const QUICK_REPLY = 'Добрый день! Мы подобрали для вас тур.';

describe('manager booking notes and invoice stubs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue([]);
    mockApiClient.get.mockImplementation((url: string) => {
      const path = String(url);
      if (path.startsWith('/catalog/tours/cards')) {
        return Promise.resolve({ data: [] });
      }
      if (path.startsWith('/bookings/')) {
        return Promise.resolve({ data: bookingMinimal });
      }
      return Promise.resolve({ data: [] });
    });
    mockApiClient.post.mockResolvedValue({ data: {} });
    mockApiClient.patch.mockImplementation((_url: string, body: { newStatus: number }) =>
      Promise.resolve({
        data: { ...JSON.parse(BOOKING_MINIMAL_JSON), status: body.newStatus },
      }),
    );
  });

  function renderManagerBooking() {
    return renderAt(
      `/manager/bookings/${MINIMAL_BOOKING_ID}`,
      '/manager/bookings/:id',
      <ManagerBookingDetailPage />,
    );
  }

  it('keeps notes and invoice inactive without a «Скоро» toast', async () => {
    const user = userEvent.setup();
    renderManagerBooking();

    const notesCard = (await screen.findByRole('heading', { name: 'Заметки менеджера' }))
      .parentElement;
    expect(notesCard).not.toBeNull();
    const notes = within(notesCard as HTMLElement).getByRole('textbox', {
      name: 'Заметки менеджера',
    });
    const saveLater = within(notesCard as HTMLElement).getByRole('button', {
      name: 'Появится позже',
    });
    expect(notes).toBeDisabled();
    expect(notes).toHaveValue('');
    expect(saveLater).toBeDisabled();
    expect(saveLater).toHaveTextContent('Появится позже');
    await waitFor(() => expect(saveLater).toBeVisible());
    expect(saveLater).not.toHaveAttribute('title');

    fireEvent.change(notes, { target: { value: 'внутренняя заметка' } });
    expect(notes).toHaveValue('');

    const actionsCard = screen.getByRole('heading', { name: 'Действия' }).parentElement;
    expect(actionsCard).not.toBeNull();
    const invoice = within(actionsCard as HTMLElement).getByRole('button', {
      name: 'Появится позже',
    });
    expect(invoice).toBeDisabled();
    expect(invoice).toHaveTextContent('Появится позже');
    await waitFor(() => expect(invoice).toBeVisible());
    expect(invoice).not.toHaveAttribute('title');

    fireEvent.click(saveLater);
    fireEvent.click(invoice);

    expect(screen.queryByText('Скоро', { exact: true })).not.toBeInTheDocument();
    expect(screen.queryByText('Сохранить (скоро)')).not.toBeInTheDocument();
    expect(screen.queryByText('Создать счёт (скоро)')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Скоро')).not.toBeInTheDocument();
    expect(mockApiClient.post).not.toHaveBeenCalled();

    const writeText = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue();
    await user.click(screen.getByRole('button', { name: QUICK_REPLY }));
    expect(writeText).toHaveBeenCalledWith(QUICK_REPLY);
    expect(await screen.findByText('Шаблон скопирован')).toBeInTheDocument();
    expect(screen.queryByText('Скоро', { exact: true })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Отправить предложение' }));
    expect(mockApiClient.post).toHaveBeenCalledWith(
      `/bookings/${MINIMAL_BOOKING_ID}/proposal`,
      { notes: '' },
    );
    expect(await screen.findByText('Предложение отправлено')).toBeInTheDocument();
    expect(screen.queryByText('Скоро', { exact: true })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Новая' }));
    await user.click(await screen.findByRole('button', { name: 'В работе' }));
    await user.click(screen.getByRole('button', { name: 'Применить' }));
    expect(mockApiClient.patch).toHaveBeenCalledWith(
      `/bookings/${MINIMAL_BOOKING_ID}/status`,
      { newStatus: 1 },
    );
    expect(await screen.findByText('Статус обновлён')).toBeInTheDocument();
    expect(screen.queryByText('Скоро', { exact: true })).not.toBeInTheDocument();
  });
});
