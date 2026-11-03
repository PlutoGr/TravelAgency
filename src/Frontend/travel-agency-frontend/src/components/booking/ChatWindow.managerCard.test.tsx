import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ChatMessage } from '@/types';
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

const MARINA = 'marina-id';
const PHOTO = '/api/v1/media/files/file-1/a256';

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
      avatarUrl: PHOTO,
    });
    vi.mocked(getClientCard).mockResolvedValue({
      id: 'client-2',
      displayName: 'Илья',
      email: 'ilya@example.com',
      bookings: [],
      notes: [],
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

    await waitFor(() => {
      expect(getManagerCard).toHaveBeenCalledWith(MARINA);
      expect(getManagerCard).toHaveBeenCalledWith('admin-1');
    });
    expect(getManagerCard).toHaveBeenCalledTimes(2);
    expect(within(managerButton).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', PHOTO);
    expect(screen.queryByRole('img', { name: 'Система' })).not.toBeInTheDocument();
    expect(screen.queryByRole('img', { name: 'Я' })).not.toBeInTheDocument();
    if (role === 'manager') {
      expect(getClientCard).toHaveBeenCalledWith('client-2');
    } else {
      expect(getClientCard).not.toHaveBeenCalled();
    }

    await user.click(managerButton);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', PHOTO);
    expect(getManagerCard).toHaveBeenCalledTimes(2);

    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(getManagerCard).toHaveBeenCalledTimes(2);

    await user.click(screen.getByText('Илья'));
    expect(getManagerCard).toHaveBeenCalledTimes(2);
    expect(screen.getByTestId('location')).toHaveTextContent(path);
    expect(screen.getByTestId('location').textContent).not.toContain(MARINA);
  });

  it('после ошибки повторное открытие снова запрашивает карточку', async () => {
    const user = userEvent.setup();
    let marinaCalls = 0;
    let rejectFirst: (error: Error) => void = () => {};
    const first = new Promise<never>((_resolve, reject) => {
      rejectFirst = reject;
    });
    vi.mocked(getManagerCard).mockImplementation((id: string) => {
      if (id !== MARINA) return Promise.resolve({ id, displayName: 'Админ' });
      marinaCalls += 1;
      if (marinaCalls === 1) return first;
      return Promise.resolve({
        id: MARINA,
        displayName: 'Марина Иванова',
        avatarUrl: PHOTO,
      });
    });
    renderChat('client', '/dashboard/bookings/booking-1');

    const managerButton = await screen.findByRole('button', {
      name: 'Карточка менеджера, Марина Иванова',
    });
    await waitFor(() => expect(marinaCalls).toBe(1));
    await user.click(managerButton);
    rejectFirst(new Error('network'));
    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    expect(marinaCalls).toBe(1);
    expect(within(managerButton).getByText('МИ')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Закрыть' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByText('Марина Иванова')).toBeInTheDocument();
    expect(marinaCalls).toBe(2);

    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(marinaCalls).toBe(2);
  });
});
