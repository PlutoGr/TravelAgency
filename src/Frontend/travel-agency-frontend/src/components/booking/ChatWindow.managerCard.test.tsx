import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ChatMessage } from '@/types';
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

const MARINA = 'marina-id';

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

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
  message({
    id: 'foreign-manager',
    senderId: MARINA,
    senderName: 'Марина Иванова',
    senderRole: 'manager',
    text: 'Здравствуйте',
  }),
  message({
    id: 'foreign-admin',
    senderId: 'admin-1',
    senderName: 'Админ',
    senderRole: 'admin',
  }),
  message({
    id: 'foreign-client',
    senderId: 'client-2',
    senderName: 'Илья',
    senderRole: 'client',
  }),
  message({
    id: 'system',
    senderId: 'system',
    senderName: 'Система',
    senderRole: 'system',
  }),
  message({
    id: 'own-manager',
    senderId: 'self',
    senderName: 'Я',
    senderRole: 'manager',
  }),
];

function renderChat(role: 'client' | 'manager', path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <Routes>
        <Route
          path="*"
          element={
            <div className="h-[600px]">
              <ChatWindow
                bookingId="booking-1"
                currentUserId="self"
                currentUserName={role === 'manager' ? 'Я' : 'Илья'}
                currentUserRole={role}
              />
            </div>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ChatWindow manager card', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue(feed);
    vi.mocked(getManagerCard).mockResolvedValue({
      id: MARINA,
      displayName: 'Марина Иванова',
      avatarUrl: '/api/v1/media/files/file-1/a256',
      about: 'О себе',
    });
  });

  it.each([
    ['client', '/dashboard/bookings/booking-1'],
    ['manager', '/manager/bookings/booking-1'],
  ] as const)('кнопка только у чужого менеджера и админа, зритель %s', async (role, path) => {
    const user = userEvent.setup();
    renderChat(role, path);

    const managerButton = await screen.findByRole('button', {
      name: 'Карточка менеджера, Марина Иванова',
    });
    expect(screen.getByRole('button', { name: 'Карточка менеджера, Админ' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Карточка менеджера, Илья/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Карточка менеджера, Система/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Карточка менеджера, Я/ })).not.toBeInTheDocument();

    const list = screen.getByTestId('chat-message-list');
    expect(within(list).queryByRole('img')).not.toBeInTheDocument();
    expect(within(list).getByText('МИ')).toBeInTheDocument();

    await user.click(managerButton);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute(
      'src',
      '/api/v1/media/files/file-1/a256',
    );
    expect(getManagerCard).toHaveBeenCalledTimes(1);
    expect(getManagerCard).toHaveBeenCalledWith(MARINA);

    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(getManagerCard).toHaveBeenCalledTimes(1);

    await user.click(screen.getByText('Илья'));
    expect(getManagerCard).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('location')).toHaveTextContent(path);
    expect(screen.getByTestId('location').textContent).not.toContain(MARINA);
  });
});
