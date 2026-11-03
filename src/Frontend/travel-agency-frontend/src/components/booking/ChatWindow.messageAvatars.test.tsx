import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import toast from 'react-hot-toast';
import type { ChatMessage } from '@/types';
import { getMe } from '@/api/auth';
import { getMessages, sendMessage, subscribeToMessages } from '@/api/chat';
import { getClientCard } from '@/api/clientCards';
import { getManagerCard } from '@/api/managerCard';
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

vi.mock('@/api/auth', () => ({
  getMe: vi.fn(),
}));

const SELF = 'self';
const MARINA = 'marina-id';
const ADMIN = 'admin-id';
const ANNA = 'anna-id';
const MARINA_PHOTO = '/api/v1/media/files/marina/a256';
const ADMIN_PHOTO = '/api/v1/media/files/admin/a256';
const ANNA_PHOTO = '/api/v1/media/files/anna/a256';
const SELF_PHOTO = '/api/v1/media/files/self/a256';
const MARINA_PHOTO_NEXT = '/api/v1/media/files/marina-next/a256';

let push: (msg: ChatMessage) => void = () => {};

function message(partial: Partial<ChatMessage> & Pick<ChatMessage, 'id' | 'senderId' | 'senderRole'>): ChatMessage {
  return {
    bookingId: 'booking-1',
    senderName: 'Имя',
    text: partial.id,
    createdAt: '2026-04-02T08:00:00Z',
    ...partial,
  };
}

function renderChat(
  role: 'client' | 'manager' | 'admin',
  feed: ChatMessage[],
  avatar?: string,
) {
  vi.mocked(getMessages).mockResolvedValue(feed);
  const path = role === 'client' ? '/dashboard/bookings/booking-1' : '/manager/bookings/booking-1';
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/manager/clients/:id" element={<div>полная карточка</div>} />
        <Route
          path="*"
          element={
            <div className="h-[600px]">
              <ChatWindow
                bookingId="booking-1"
                currentUserId={SELF}
                currentUserName="Илья Петров"
                currentUserRole={role}
                currentUserAvatar={avatar}
              />
            </div>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

function callsFor(mock: { mock: { calls: unknown[][] } }, id: string) {
  return mock.mock.calls.filter((call) => call[0] === id).length;
}

describe('ChatWindow message avatars', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    push = () => {};
    vi.mocked(subscribeToMessages).mockImplementation((_id, onMessage) => {
      push = onMessage;
      return () => {};
    });
    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA) return { id, displayName: 'Марина Иванова', avatarUrl: MARINA_PHOTO };
      if (id === ADMIN) return { id, displayName: 'Админ Сергеев', avatarUrl: ADMIN_PHOTO };
      return { id, displayName: 'Сотрудник' };
    });
    vi.mocked(getClientCard).mockImplementation(async (id: string) => ({
      id,
      displayName: id === ANNA ? 'Анна Соколова' : 'Клиент',
      email: 'anna@example.com',
      ...(id === ANNA ? { avatarUrl: ANNA_PHOTO } : {}),
      bookings: [],
      notes: [],
    }));
    vi.mocked(getMessages).mockResolvedValue([]);
  });

  it('ТК-1 кружок менеджера и окно показывают один src, в тексте сообщения адреса нет', async () => {
    const user = userEvent.setup();
    const feed = [
      message({
        id: 'marina',
        senderId: MARINA,
        senderName: 'Марина Иванова',
        senderRole: 'manager',
        text: 'Здравствуйте',
      }),
    ];
    renderChat('client', feed);

    const button = await screen.findByRole('button', { name: 'Карточка менеджера, Марина Иванова' });
    expect(await within(button).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(screen.getByText('Здравствуйте')).not.toHaveTextContent(MARINA_PHOTO);
    expect(feed[0]).not.toHaveProperty('avatarUrl');
    expect(screen.queryByText(/нет фото/i)).not.toBeInTheDocument();

    await user.click(button);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(getMe).not.toHaveBeenCalled();
  });

  it.each(['manager', 'admin'] as const)(
    'ТК-2 кружок клиента и окно показывают один src, зритель %s',
    async (role) => {
      const user = userEvent.setup();
      renderChat(role, [
        message({ id: 'anna', senderId: ANNA, senderName: 'Анна Соколова', senderRole: 'client', text: 'Добрый день' }),
      ]);

      const button = await screen.findByRole('button', { name: 'Карточка клиента, Анна Соколова' });
      expect(await within(button).findByRole('img', { name: 'Анна Соколова' })).toHaveAttribute('src', ANNA_PHOTO);
      expect(callsFor(vi.mocked(getManagerCard), ANNA)).toBe(0);

      await user.click(button);
      const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
      expect(within(dialog).getByRole('img', { name: 'Анна Соколова' })).toHaveAttribute('src', ANNA_PHOTO);
      expect(screen.queryByText(/нет фото/i)).not.toBeInTheDocument();
    },
  );

  it.each(['client', 'manager'] as const)(
    'ТК-3 без адреса остаются инициалы, зритель %s',
    async (role) => {
      vi.mocked(getManagerCard).mockResolvedValue({ id: MARINA, displayName: 'Марина Иванова', avatarUrl: '' });
      vi.mocked(getClientCard).mockResolvedValue({
        id: ANNA,
        displayName: 'Анна',
        email: 'anna@example.com',
        bookings: [],
        notes: [],
      });
      renderChat(role, [
        message({ id: 'marina', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
        message({ id: 'anna', senderId: ANNA, senderName: 'Анна', senderRole: 'client' }),
        message({ id: 'own', senderId: SELF, senderName: 'Илья Петров', senderRole: role === 'client' ? 'client' : 'manager' }),
      ]);

      const list = await screen.findByTestId('chat-message-list');
      await waitFor(() => expect(getManagerCard).toHaveBeenCalledWith(MARINA));
      expect(within(list).getByText('МИ')).toBeInTheDocument();
      expect(within(list).getByText('ИП')).toBeInTheDocument();
      expect(within(list).queryByRole('img')).not.toBeInTheDocument();
      expect(screen.queryByText(/нет фото/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/не указано/i)).not.toBeInTheDocument();
      if (role === 'client') {
        expect(getClientCard).not.toHaveBeenCalled();
        expect(within(list).getByText('А')).toBeInTheDocument();
      } else {
        await waitFor(() => expect(getClientCard).toHaveBeenCalledWith(ANNA));
        expect(within(list).getByText('А')).toBeInTheDocument();
      }
    },
  );

  it.each(['client', 'manager', 'admin'] as const)(
    'ТК-4 у каждого свой src или свои инициалы, зритель %s',
    async (role) => {
      vi.mocked(getClientCard).mockResolvedValue({
        id: ANNA,
        displayName: 'Анна',
        email: 'anna@example.com',
        bookings: [],
        notes: [],
      });
      renderChat(
        role,
        [
          message({ id: 'own', senderId: SELF, senderName: 'Илья Петров', senderRole: 'client' }),
          message({ id: 'marina', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
          message({ id: 'admin', senderId: ADMIN, senderName: 'Админ Сергеев', senderRole: 'admin' }),
          message({ id: 'anna', senderId: ANNA, senderName: 'Анна', senderRole: 'client' }),
        ],
        SELF_PHOTO,
      );

      const list = await screen.findByTestId('chat-message-list');
      expect(await within(list).findByRole('img', { name: 'Илья Петров' })).toHaveAttribute('src', SELF_PHOTO);
      expect(within(list).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
      expect(within(list).getByRole('img', { name: 'Админ Сергеев' })).toHaveAttribute('src', ADMIN_PHOTO);
      expect(within(list).getByText('А')).toBeInTheDocument();
      expect(within(list).queryByRole('img', { name: 'Анна' })).not.toBeInTheDocument();

      expect(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Карточка менеджера, Админ Сергеев' })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /Карточка менеджера, Илья Петров/ })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /Карточка менеджера, Анна/ })).not.toBeInTheDocument();
      if (role === 'client') {
        expect(screen.queryByRole('button', { name: /Карточка клиента/ })).not.toBeInTheDocument();
        expect(getClientCard).not.toHaveBeenCalled();
      } else {
        expect(screen.getByRole('button', { name: 'Карточка клиента, Анна' })).toBeInTheDocument();
        expect(getClientCard).toHaveBeenCalledWith(ANNA);
      }
    },
  );

  it('ТК-5 новое сообщение сотрудника обновляет все его кружки и кэш окна', async () => {
    const user = userEvent.setup();
    renderChat('client', [
      message({ id: 'm1', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager', createdAt: '2026-04-02T08:00:00Z' }),
      message({ id: 'm2', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager', createdAt: '2026-04-02T08:01:00Z' }),
      message({ id: 'admin', senderId: ADMIN, senderName: 'Админ Сергеев', senderRole: 'admin', createdAt: '2026-04-02T08:02:00Z' }),
    ]);

    const list = await screen.findByTestId('chat-message-list');
    await waitFor(() => expect(within(list).getAllByRole('img', { name: 'Марина Иванова' })).toHaveLength(2));
    expect(callsFor(vi.mocked(getManagerCard), MARINA)).toBe(1);
    expect(callsFor(vi.mocked(getManagerCard), ADMIN)).toBe(1);

    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA) return { id, displayName: 'Марина Иванова', avatarUrl: MARINA_PHOTO_NEXT };
      return { id, displayName: 'Админ Сергеев', avatarUrl: ADMIN_PHOTO };
    });
    push(message({
      id: 'm3',
      senderId: MARINA,
      senderName: 'Марина Иванова',
      senderRole: 'manager',
      createdAt: '2026-04-02T08:03:00Z',
    }));

    await waitFor(() => {
      const imgs = within(list).getAllByRole('img', { name: 'Марина Иванова' });
      expect(imgs).toHaveLength(3);
      for (const img of imgs) expect(img).toHaveAttribute('src', MARINA_PHOTO_NEXT);
    });
    expect(within(list).getByRole('img', { name: 'Админ Сергеев' })).toHaveAttribute('src', ADMIN_PHOTO);
    expect(callsFor(vi.mocked(getManagerCard), MARINA)).toBe(2);
    expect(callsFor(vi.mocked(getManagerCard), ADMIN)).toBe(1);

    await user.click(within(list).getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' })[0]);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO_NEXT);
    expect(callsFor(vi.mocked(getManagerCard), MARINA)).toBe(2);
  });

  it('ТК-5 новое сообщение клиента обновляет кружки без кэша карточки', async () => {
    const user = userEvent.setup();
    renderChat('manager', [
      message({ id: 'a1', senderId: ANNA, senderName: 'Анна Соколова', senderRole: 'client', createdAt: '2026-04-02T08:00:00Z' }),
      message({ id: 'a2', senderId: ANNA, senderName: 'Анна Соколова', senderRole: 'client', createdAt: '2026-04-02T08:01:00Z' }),
      message({ id: 'marina', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager', createdAt: '2026-04-02T08:02:00Z' }),
    ]);

    const list = await screen.findByTestId('chat-message-list');
    await waitFor(() => expect(within(list).getAllByRole('img', { name: 'Анна Соколова' })).toHaveLength(2));
    expect(callsFor(vi.mocked(getClientCard), ANNA)).toBe(1);

    vi.mocked(getClientCard).mockResolvedValue({
      id: ANNA,
      displayName: 'Анна Соколова',
      email: 'anna@example.com',
      avatarUrl: '/api/v1/media/files/anna-next/a256',
      bookings: [],
      notes: [],
    });
    push(message({
      id: 'a3',
      senderId: ANNA,
      senderName: 'Анна Соколова',
      senderRole: 'client',
      createdAt: '2026-04-02T08:03:00Z',
    }));

    await waitFor(() => {
      const imgs = within(list).getAllByRole('img', { name: 'Анна Соколова' });
      expect(imgs).toHaveLength(3);
      for (const img of imgs) expect(img).toHaveAttribute('src', '/api/v1/media/files/anna-next/a256');
    });
    expect(within(list).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(callsFor(vi.mocked(getClientCard), ANNA)).toBe(2);

    await user.click(within(list).getAllByRole('button', { name: 'Карточка клиента, Анна Соколова' })[0]);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    expect(within(dialog).getByRole('img', { name: 'Анна Соколова' })).toHaveAttribute(
      'src',
      '/api/v1/media/files/anna-next/a256',
    );
    expect(callsFor(vi.mocked(getClientCard), ANNA)).toBe(3);
  });

  it('ТК-5 удаление фото возвращает инициалы только этому человеку', async () => {
    renderChat('client', [
      message({ id: 'm1', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
      message({ id: 'admin', senderId: ADMIN, senderName: 'Админ Сергеев', senderRole: 'admin' }),
    ]);

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Марина Иванова' })).toBeInTheDocument();

    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA) return { id, displayName: 'Марина Иванова' };
      return { id, displayName: 'Админ Сергеев', avatarUrl: ADMIN_PHOTO };
    });
    push(message({
      id: 'm2',
      senderId: MARINA,
      senderName: 'Марина Иванова',
      senderRole: 'manager',
      createdAt: '2026-04-02T08:05:00Z',
    }));

    await waitFor(() => {
      expect(within(list).getAllByText('МИ')).toHaveLength(2);
      expect(within(list).queryByRole('img', { name: 'Марина Иванова' })).not.toBeInTheDocument();
    });
    expect(within(list).getByRole('img', { name: 'Админ Сергеев' })).toHaveAttribute('src', ADMIN_PHOTO);
  });

  it('ТК-6 сбой одного чтения оставляет инициалы и не блокирует ленту', async () => {
    const user = userEvent.setup();
    const errorSpy = vi.spyOn(toast, 'error').mockImplementation(() => 'id');
    vi.mocked(getManagerCard).mockRejectedValue(new Error('403'));
    vi.mocked(sendMessage).mockImplementation(async (bookingId, text, senderId) => ({
      id: 'saved-send',
      bookingId,
      senderId,
      senderName: 'Илья Петров',
      senderRole: 'manager',
      text,
      createdAt: '2026-04-02T09:00:00Z',
    }));
    renderChat('manager', [
      message({ id: 'marina', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
      message({ id: 'anna', senderId: ANNA, senderName: 'Анна Соколова', senderRole: 'client', text: 'Добрый день' }),
    ], SELF_PHOTO);

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Анна Соколова' })).toHaveAttribute('src', ANNA_PHOTO);
    await waitFor(() => expect(getManagerCard).toHaveBeenCalledWith(MARINA));
    expect(within(list).getByText('МИ')).toBeInTheDocument();
    expect(screen.queryByText('Не удалось открыть карточку.')).not.toBeInTheDocument();
    expect(errorSpy).not.toHaveBeenCalled();

    await user.type(screen.getByPlaceholderText('Введите сообщение...'), 'Ответ');
    await user.keyboard('{Enter}');
    expect(await screen.findByText('Ответ')).toBeInTheDocument();
    expect(within(list).getByRole('img', { name: 'Илья Петров' })).toHaveAttribute('src', SELF_PHOTO);
    expect(screen.queryByText('Не удалось открыть карточку.')).not.toBeInTheDocument();
    expect(errorSpy).not.toHaveBeenCalled();
    errorSpy.mockRestore();
  });

  it('ТК-7 служебное сообщение остаётся инициалами и не открывает карточку', async () => {
    const user = userEvent.setup();
    renderChat('client', [
      message({ id: 'system', senderId: 'system', senderName: 'Система', senderRole: 'system', text: 'Заявка создана' }),
      message({ id: 'marina', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
    ], SELF_PHOTO);

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(within(list).getByText('С')).toBeInTheDocument();
    expect(within(list).queryByRole('img', { name: 'Система' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Система/ })).not.toBeInTheDocument();
    expect(callsFor(vi.mocked(getManagerCard), 'system')).toBe(0);
    expect(callsFor(vi.mocked(getClientCard), 'system')).toBe(0);

    await user.click(within(list).getByText('С'));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(within(list).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
  });

  it('ТК-8 один GET на сотрудника, не на каждое сообщение и не на себя', async () => {
    renderChat('client', [
      message({ id: 'm1', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager' }),
      message({ id: 'm2', senderId: MARINA, senderName: 'Марина Иванова', senderRole: 'manager', createdAt: '2026-04-02T08:01:00Z' }),
      message({ id: 'admin', senderId: ADMIN, senderName: 'Админ Сергеев', senderRole: 'admin' }),
      message({ id: 'own', senderId: SELF, senderName: 'Илья Петров', senderRole: 'client' }),
    ], SELF_PHOTO);

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Илья Петров' })).toHaveAttribute('src', SELF_PHOTO);
    await waitFor(() => {
      expect(callsFor(vi.mocked(getManagerCard), MARINA)).toBe(1);
      expect(callsFor(vi.mocked(getManagerCard), ADMIN)).toBe(1);
    });
    expect(callsFor(vi.mocked(getManagerCard), SELF)).toBe(0);
    expect(getClientCard).not.toHaveBeenCalled();
    expect(getMe).not.toHaveBeenCalled();
  });

  it('ТК-9 свой кружок берёт аватар сессии до ответа сервера и после', async () => {
    let resolveSend: (msg: ChatMessage) => void = () => {};
    vi.mocked(sendMessage).mockImplementation(
      () => new Promise<ChatMessage>((resolve) => {
        resolveSend = resolve;
      }),
    );
    renderChat('client', [], SELF_PHOTO);

    await screen.findByPlaceholderText('Введите сообщение...');
    const user = userEvent.setup();
    await user.type(screen.getByPlaceholderText('Введите сообщение...'), 'Привет');
    const attachment = screen.getByRole('button', { name: 'Появится позже' });
    const send = screen.getAllByRole('button').find((button) => button !== attachment);
    await user.click(send!);

    const list = screen.getByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Илья Петров' })).toHaveAttribute('src', SELF_PHOTO);
    expect(getMe).not.toHaveBeenCalled();
    expect(getManagerCard).not.toHaveBeenCalled();
    expect(getClientCard).not.toHaveBeenCalled();

    resolveSend(message({
      id: 'saved-1',
      senderId: SELF,
      senderName: 'Илья Петров',
      senderRole: 'client',
      text: 'Привет',
      createdAt: '2026-04-02T09:00:00Z',
    }));
    await waitFor(() => {
      const imgs = within(list).getAllByRole('img', { name: 'Илья Петров' });
      expect(imgs).toHaveLength(1);
      expect(imgs[0]).toHaveAttribute('src', SELF_PHOTO);
    });
    expect(sendMessage).toHaveBeenCalledTimes(1);
    expect(getMe).not.toHaveBeenCalled();
  });

  it('ТК-9 без аватара сессии свой кружок остаётся инициалами', async () => {
    vi.mocked(sendMessage).mockImplementation(async (bookingId, text, senderId) => message({
      id: 'saved-1',
      bookingId,
      senderId,
      senderName: 'Илья Петров',
      senderRole: 'client',
      text,
      createdAt: '2026-04-02T09:00:00Z',
    }));
    renderChat('client', []);

    await screen.findByPlaceholderText('Введите сообщение...');
    const user = userEvent.setup();
    await user.type(screen.getByPlaceholderText('Введите сообщение...'), 'Привет');
    const attachment = screen.getByRole('button', { name: 'Появится позже' });
    const send = screen.getAllByRole('button').find((button) => button !== attachment);
    await user.click(send!);

    const list = screen.getByTestId('chat-message-list');
    expect(await within(list).findByText('ИП')).toBeInTheDocument();
    expect(within(list).queryByRole('img')).not.toBeInTheDocument();
    expect(sendMessage).toHaveBeenCalledTimes(1);
    expect(getMe).not.toHaveBeenCalled();
    expect(getManagerCard).not.toHaveBeenCalled();
  });

  it('ранний ответ после загрузки истории не затирает фото и не расходится с окном', async () => {
    const user = userEvent.setup();
    let resolveHistory: (feed: ChatMessage[]) => void = () => {};
    vi.mocked(getMessages).mockImplementation(
      () => new Promise((resolve) => {
        resolveHistory = resolve;
      }),
    );
    let rejectEarly: (error: Error) => void = () => {};
    const early = new Promise<never>((_resolve, reject) => {
      rejectEarly = reject;
    });
    let managerCalls = 0;
    vi.mocked(getManagerCard).mockImplementation((id: string) => {
      managerCalls += 1;
      if (managerCalls === 1) return early;
      return Promise.resolve({
        id,
        displayName: 'Марина Иванова',
        avatarUrl: MARINA_PHOTO,
      });
    });

    render(
      <MemoryRouter initialEntries={['/dashboard/bookings/booking-1']}>
        <div className="h-[600px]">
          <ChatWindow
            bookingId="booking-1"
            currentUserId={SELF}
            currentUserName="Илья Петров"
            currentUserRole="client"
          />
        </div>
      </MemoryRouter>,
    );

    push(message({
      id: 'live',
      senderId: MARINA,
      senderName: 'Марина Иванова',
      senderRole: 'manager',
      text: 'уже в ленте',
      createdAt: '2026-04-02T08:05:00Z',
    }));
    expect(managerCalls).toBe(1);

    resolveHistory([
      message({
        id: 'marina',
        senderId: MARINA,
        senderName: 'Марина Иванова',
        senderRole: 'manager',
        text: 'Здравствуйте',
      }),
    ]);

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);

    await act(async () => {
      rejectEarly(new Error('network'));
    });
    expect(within(list).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(within(list).queryByText('МИ')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Карточка менеджера, Марина Иванова' }));
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(within(dialog).getByRole('img', { name: 'Марина Иванова' })).toHaveAttribute('src', MARINA_PHOTO);
    expect(managerCalls).toBe(2);
  });
});
