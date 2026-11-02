import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ChatMessage } from '@/types';
import type { ClientCard } from '@/api/clientCards';
import { getClientCard } from '@/api/clientCards';
import { getManagerCard } from '@/api/managerCard';
import { getMessages } from '@/api/chat';
import ChatWindow from './ChatWindow';

vi.mock('@/api/chat', () => ({
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
  subscribeToMessages: vi.fn(() => () => {}),
  disconnectChat: vi.fn(),
  mapDtoToMessage: vi.fn(),
}));

vi.mock('@/api/managerCard', () => ({
  getManagerCard: vi.fn(),
}));

vi.mock('@/api/clientCards', () => ({
  getClientCard: vi.fn(),
}));

const ANNA = 'anna-id';

const card: ClientCard = {
  id: ANNA,
  displayName: 'Анна Соколова',
  email: 'anna@example.com',
  avatarUrl: '/api/v1/media/files/file-1/a256',
  phone: '+79991234567',
  bookings: [
    { id: 'b1', tourId: 't1', status: 'new', createdAt: '2026-11-02T09:00:00Z' },
    { id: 'b2', tourId: 't2', status: 'closed', createdAt: '2026-11-01T09:00:00Z' },
  ],
  notes: [
    {
      id: 'n1',
      text: 'просит вылет после 15 мая',
      authorName: 'Марина Иванова',
      createdAt: '2026-11-02T12:30:00Z',
    },
  ],
};

function message(partial: Partial<ChatMessage> & Pick<ChatMessage, 'id' | 'senderId' | 'senderRole'>): ChatMessage {
  return {
    bookingId: 'booking-1',
    senderName: 'Имя',
    text: partial.id,
    createdAt: '2026-04-02T08:00:00Z',
    ...partial,
  };
}

const feed: ChatMessage[] = [
  message({ id: 'anna', senderId: ANNA, senderName: 'Анна Соколова', senderRole: 'client', text: 'Добрый день' }),
  message({ id: 'manager', senderId: 'marina', senderName: 'Марина Иванова', senderRole: 'manager' }),
  message({ id: 'admin', senderId: 'admin-1', senderName: 'Админ', senderRole: 'admin' }),
  message({ id: 'own', senderId: 'self', senderName: 'Я', senderRole: 'client' }),
  message({ id: 'system', senderId: 'system', senderName: 'Система', senderRole: 'system' }),
];

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function renderChat(role: 'client' | 'manager' | 'admin') {
  return render(
    <MemoryRouter initialEntries={['/manager/bookings/booking-1']}>
      <LocationProbe />
      <Routes>
        <Route path="/manager/clients/:id" element={<div>полная карточка</div>} />
        <Route
          path="*"
          element={
            <div className="h-[600px]">
              <ChatWindow
                bookingId="booking-1"
                currentUserId="self"
                currentUserName="Я"
                currentUserRole={role}
              />
            </div>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ChatWindow client card', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue(feed);
    vi.mocked(getClientCard).mockResolvedValue(card);
    vi.mocked(getManagerCard).mockResolvedValue({
      id: 'marina',
      displayName: 'Марина Иванова',
      avatarUrl: '/api/v1/media/files/file-1/a256',
    });
  });

  it('opens the client glance from the client circle and keeps the chat address', async () => {
    const user = userEvent.setup();
    renderChat('manager');

    const button = await screen.findByRole('button', { name: 'Карточка клиента, Анна Соколова' });
    expect(within(button).getByText('АС')).toBeInTheDocument();
    expect(within(button).queryByRole('img')).not.toBeInTheDocument();
    await user.click(button);

    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    expect(getClientCard).toHaveBeenCalledWith(ANNA);
    expect(getClientCard).toHaveBeenCalledTimes(1);
    expect(dialog).not.toHaveTextContent('anna@example.com');
    expect(dialog).not.toHaveTextContent('просит вылет после 15 мая');
    expect(within(dialog).getByText('2 заявки')).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/booking-1');

    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/booking-1');
  });

  it('does not open a client card from a manager, own, system or client-viewer circle', async () => {
    const user = userEvent.setup();
    renderChat('client');

    expect(await screen.findByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Карточка менеджера, Админ' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Карточка клиента/ })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByText('Марина Иванова')).toBeInTheDocument();
    expect(within(dialog).queryByText('Телефон')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('anna@example.com')).not.toBeInTheDocument();
    expect(getClientCard).not.toHaveBeenCalled();
  });

  it('keeps the manager card for an admin viewer and still opens the client circle', async () => {
    renderChat('admin');
    expect(await screen.findByRole('button', { name: 'Карточка клиента, Анна Соколова' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Карточка клиента, Я' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Карточка клиента, Система' })).not.toBeInTheDocument();
  });
});
