import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
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
const OTHER = 'other-id';
const PHOTO = '/api/v1/media/files/file-1/a256';
const MARINA_EMAIL = 'marina@example.com';
const ANNA_EMAIL = 'anna@example.com';
const ILYA_EMAIL = 'ilya@example.com';

let push: (msg: ChatMessage) => void = () => {};

function message(partial: Partial<ChatMessage> & Pick<ChatMessage, 'id' | 'senderId' | 'senderRole'>): ChatMessage {
  return {
    bookingId: 'booking-1',
    senderName: 'имя-не-показываем@example.com',
    text: partial.id,
    createdAt: '2026-04-02T08:00:00Z',
    ...partial,
  };
}

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function rowOf(text: string): HTMLElement {
  const node = screen.getByText(text);
  const row = node.closest('[class~="gap-2.5"]');
  if (!(row instanceof HTMLElement)) throw new Error('строка сообщения не найдена');
  return row;
}

function renderChat(
  role: 'client' | 'manager',
  name: string,
  feed: ChatMessage[],
  avatar?: string,
) {
  vi.mocked(getMessages).mockResolvedValue(feed);
  const path = role === 'client' ? '/dashboard/bookings/booking-1' : '/manager/bookings/booking-1';
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <div className="h-[600px]">
        <ChatWindow
          bookingId="booking-1"
          currentUserId={SELF}
          currentUserName={name}
          currentUserRole={role}
          currentUserAvatar={avatar}
        />
      </div>
    </MemoryRouter>,
  );
}

describe('ChatWindow names and pointer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    push = () => {};
    vi.mocked(subscribeToMessages).mockImplementation((_id, onMessage) => {
      push = onMessage;
      return () => {};
    });
    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === ADMIN) return { id, displayName: 'Админ Сергеев' };
      if (id === OTHER) return { id, displayName: 'Ольга Смирнова' };
      return { id, displayName: ' Марина Иванова ', avatarUrl: PHOTO };
    });
    vi.mocked(getClientCard).mockImplementation(async (id: string) => ({
      id,
      displayName: id === ANNA ? 'Анна Соколова' : 'Клиент',
      email: ANNA_EMAIL,
      phone: '+79991234567',
      bookings: [],
      notes: [],
    }));
    vi.mocked(getMessages).mockResolvedValue([]);
  });

  it('ТК-1 подпись менеджера из той же карточки, без почты и без второго запроса', async () => {
    renderChat('client', 'Илья Петров', [
      message({
        id: 'm1',
        senderId: MARINA,
        senderName: MARINA_EMAIL,
        senderRole: 'manager',
        text: 'Здравствуйте',
      }),
      message({
        id: 'm2',
        senderId: MARINA,
        senderName: MARINA_EMAIL,
        senderRole: 'manager',
        text: 'Жду ответ',
        createdAt: '2026-04-02T08:01:00Z',
      }),
    ]);

    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toHaveLength(4);
    });
    const names = screen.getAllByText('Марина Иванова');
    expect(names).toHaveLength(2);
    for (const name of names) expect(name.textContent).toBe('Марина Иванова');
    expect(screen.queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText('спам')).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Чат с менеджером' })).not.toBeInTheDocument();
    const images = screen.getAllByRole('img', { name: 'Марина Иванова' });
    expect(images).toHaveLength(2);
    for (const image of images) expect(image).toHaveAttribute('src', PHOTO);
    expect(getManagerCard).toHaveBeenCalledTimes(1);
    expect(getManagerCard).toHaveBeenCalledWith(MARINA);
    expect(getClientCard).not.toHaveBeenCalled();
    expect(getMe).not.toHaveBeenCalled();
  });

  it('ТК-1 заблокированный сотрудник подписан displayName, причины блокировки нет', async () => {
    vi.mocked(getManagerCard).mockResolvedValue({
      id: MARINA,
      displayName: ' Марина Иванова ',
      avatarUrl: PHOTO,
      blockedAt: '2026-01-01T00:00:00Z',
      blockReason: 'спам',
    } as never);
    renderChat('client', 'Илья Петров', [
      message({ id: 'm1', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Здравствуйте' }),
    ]);

    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toHaveLength(2);
    });
    expect(screen.getByText('Марина Иванова').textContent).toBe('Марина Иванова');
    expect(screen.queryByText('спам')).not.toBeInTheDocument();
    expect(screen.queryByText('2026-01-01T00:00:00Z')).not.toBeInTheDocument();
    expect(screen.queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
  });

  it('ТК-2 подпись клиента у сотрудника из карточки клиента, почта в ленте не появляется', async () => {
    for (const role of ['manager'] as const) {
      const view = renderChat(role, 'Марина Иванова', [
        message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'Добрый день' }),
      ]);
      await waitFor(() => {
        expect(screen.getAllByRole('button', { name: 'Карточка клиента, Анна Соколова' })).toHaveLength(2);
      });
      expect(screen.getByText('Анна Соколова').textContent).toBe('Анна Соколова');
      expect(screen.queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
      expect(getClientCard).toHaveBeenCalledTimes(1);
      expect(getClientCard).toHaveBeenCalledWith(ANNA);
      expect(vi.mocked(getManagerCard).mock.calls.map((call) => call[0])).not.toContain(ANNA);
      expect(screen.queryByRole('heading', { name: 'Чат с клиентом' })).not.toBeInTheDocument();
      view.unmount();
    }
  });

  it('ТК-3 одна текущая строка у своих и чужих, служебное и пустая лента без подписи', async () => {
    const feed = [
      message({ id: 'own', senderId: SELF, senderName: ILYA_EMAIL, senderRole: 'client', text: 'Я здесь' }),
      message({ id: 'm1', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'От Марины' }),
      message({ id: 'm2', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Ещё Марина', createdAt: '2026-04-02T08:01:00Z' }),
      message({ id: 'admin', senderId: ADMIN, senderName: 'admin@example.com', senderRole: 'admin', text: 'От админа' }),
      message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'От Анны' }),
      message({ id: 'system', senderId: 'system', senderName: 'Система', senderRole: 'system', text: 'Заявка создана' }),
    ];

    const clientView = renderChat('client', 'Илья Петров', feed);
    await waitFor(() => expect(screen.getAllByText('Марина Иванова')).toHaveLength(2));
    expect(screen.getByText('Илья Петров').tagName).toBe('P');
    expect(screen.getByText('Админ Сергеев')).toBeInTheDocument();
    expect(screen.queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText('Анна Соколова')).not.toBeInTheDocument();
    expect(screen.queryByText('Система')).not.toBeInTheDocument();
    expect(screen.getByText('С')).toBeInTheDocument();
    expect(screen.queryByText(ILYA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText('admin@example.com')).not.toBeInTheDocument();
    await waitFor(() => {
      expect(vi.mocked(getManagerCard).mock.calls.filter((call) => call[0] === MARINA)).toHaveLength(1);
      expect(vi.mocked(getManagerCard).mock.calls.filter((call) => call[0] === ADMIN)).toHaveLength(1);
    });
    expect(vi.mocked(getManagerCard).mock.calls.map((call) => call[0])).not.toContain(SELF);
    expect(getClientCard).not.toHaveBeenCalled();
    clientView.unmount();

    vi.clearAllMocks();
    const staffFeed = feed.map((item) =>
      item.senderId === SELF ? { ...item, senderRole: 'admin' as const, senderName: 'admin@example.com' } : item,
    );
    const staffView = renderChat('manager', 'Марина Иванова', staffFeed);
    await waitFor(() => expect(screen.getByText('Анна Соколова')).toBeInTheDocument());
    const marinaLines = screen.getAllByText('Марина Иванова');
    expect(marinaLines.filter((node) => node.tagName === 'P')).toHaveLength(1);
    expect(marinaLines.filter((node) => node.tagName === 'BUTTON')).toHaveLength(2);
    expect(screen.getByText('Админ Сергеев')).toBeInTheDocument();
    expect(screen.queryByText('admin@example.com')).not.toBeInTheDocument();
    expect(screen.queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText('Система')).not.toBeInTheDocument();
    expect(getClientCard).toHaveBeenCalledTimes(1);
    expect(getClientCard).toHaveBeenCalledWith(ANNA);
    expect(vi.mocked(getManagerCard).mock.calls.filter((call) => call[0] === MARINA)).toHaveLength(1);
    expect(vi.mocked(getManagerCard).mock.calls.map((call) => call[0])).not.toContain(ANNA);
    expect(vi.mocked(getManagerCard).mock.calls.map((call) => call[0])).not.toContain(SELF);
    staffView.unmount();

    const empty = renderChat('client', 'Илья Петров', []);
    expect(await screen.findByText('Начните общение с менеджером')).toBeInTheDocument();
    expect(screen.queryByText('Илья Петров')).not.toBeInTheDocument();
    empty.unmount();
  });

  it('ТК-4 своя подпись из сессии до ответа и после подмены senderName', async () => {
    let resolveSend: (msg: ChatMessage) => void = () => {};
    vi.mocked(sendMessage).mockImplementation(
      () => new Promise<ChatMessage>((resolve) => {
        resolveSend = resolve;
      }),
    );
    renderChat('client', '  Илья Петров  ', []);
    await screen.findByPlaceholderText('Введите сообщение...');
    const user = userEvent.setup();
    await user.type(screen.getByPlaceholderText('Введите сообщение...'), 'Привет');
    const attachment = screen.getByRole('button', { name: 'Появится позже' });
    const send = screen.getAllByRole('button').find((button) => button !== attachment);
    await user.click(send!);

    expect(await screen.findByText('Илья Петров')).toHaveTextContent('Илья Петров');
    expect(screen.queryByText(ILYA_EMAIL)).not.toBeInTheDocument();
    expect(getMe).not.toHaveBeenCalled();
    expect(getManagerCard).not.toHaveBeenCalled();

    resolveSend(message({
      id: 'saved-1',
      senderId: SELF,
      senderName: ILYA_EMAIL,
      senderRole: 'client',
      text: 'Привет',
      createdAt: '2026-04-02T09:00:00Z',
    }));
    await waitFor(() => {
      const names = screen.getAllByText('Илья Петров');
      expect(names).toHaveLength(1);
      expect(names[0].textContent).toBe('Илья Петров');
    });
    expect(screen.queryByText(ILYA_EMAIL)).not.toBeInTheDocument();
    expect(getMe).not.toHaveBeenCalled();
  });

  it('ТК-5 черновик не попадает в ленту, сохранение и новое сообщение обновляют строки', async () => {
    const feed = [
      message({ id: 'own1', senderId: SELF, senderName: MARINA_EMAIL, senderRole: 'client', text: 'Своё раннее' }),
      message({ id: 'own2', senderId: SELF, senderName: MARINA_EMAIL, senderRole: 'client', text: 'Своё позднее', createdAt: '2026-04-02T08:01:00Z' }),
      message({ id: 'm1', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Чужое раннее' }),
      message({ id: 'other', senderId: OTHER, senderName: 'olga@example.com', senderRole: 'manager', text: 'Другой человек' }),
    ];
    const view = renderChat('client', 'Марина Иванова', feed);
    await waitFor(() => expect(screen.getAllByText('Марина Иванова').length).toBeGreaterThan(0));
    const before = vi.mocked(getManagerCard).mock.calls.length;

    view.rerender(
      <MemoryRouter initialEntries={['/dashboard/bookings/booking-1']}>
        <LocationProbe />
        <div className="h-[600px]">
          <ChatWindow
            bookingId="booking-1"
            currentUserId={SELF}
            currentUserName="Марина Иванова"
            currentUserRole="client"
          />
        </div>
      </MemoryRouter>,
    );
    expect(screen.getAllByText('Своё раннее').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Марина Иванова').some((node) => node.tagName === 'P')).toBe(true);
    expect(screen.queryByText('Мария Иванова')).not.toBeInTheDocument();

    view.rerender(
      <MemoryRouter initialEntries={['/dashboard/bookings/booking-1']}>
        <LocationProbe />
        <div className="h-[600px]">
          <ChatWindow
            bookingId="booking-1"
            currentUserId={SELF}
            currentUserName="Мария Иванова"
            currentUserRole="client"
          />
        </div>
      </MemoryRouter>,
    );
    expect(screen.getAllByText('Мария Иванова')).toHaveLength(2);
    expect(screen.getAllByText('Марина Иванова')).toHaveLength(1);
    expect(vi.mocked(getManagerCard).mock.calls.length).toBe(before);
    expect(getMe).not.toHaveBeenCalled();

    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA) return { id, displayName: 'Мария Иванова', avatarUrl: PHOTO };
      return { id, displayName: 'Ольга Смирнова' };
    });
    push(message({
      id: 'm2',
      senderId: MARINA,
      senderName: MARINA_EMAIL,
      senderRole: 'manager',
      text: 'Чужое новое',
      createdAt: '2026-04-02T08:05:00Z',
    }));
    await waitFor(() => expect(screen.getAllByText('Мария Иванова').length).toBeGreaterThanOrEqual(4));
    expect(screen.getAllByText('Ольга Смирнова')).toHaveLength(1);
    expect(vi.mocked(getManagerCard).mock.calls.filter((call) => call[0] === OTHER)).toHaveLength(1);
    view.unmount();

    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA) return { id, displayName: 'Мария Иванова' };
      return { id, displayName: 'Ольга Смирнова' };
    });
    renderChat('client', 'Мария Иванова', [
      ...feed,
      message({ id: 'm2', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Чужое новое', createdAt: '2026-04-02T08:05:00Z' }),
    ]);
    await waitFor(() => expect(screen.getAllByText('Мария Иванова').length).toBeGreaterThanOrEqual(4));
    expect(screen.getByText('Ольга Смирнова')).toBeInTheDocument();
  });

  it.each(['network', '404', '403', '503'] as const)(
    'ТК-6 сбой %s не подставляет почту и не ломает ленту',
    async (reason) => {
      const errorSpy = vi.spyOn(toast, 'error').mockImplementation(() => 'id');
      vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
        if (id === MARINA) throw new Error(reason);
        return { id, displayName: 'Ольга Смирнова' };
      });
      vi.mocked(sendMessage).mockImplementation(async (bookingId, text, senderId) => message({
        id: 'saved-send',
        bookingId,
        senderId,
        senderName: ILYA_EMAIL,
        senderRole: 'client',
        text,
        createdAt: '2026-04-02T09:00:00Z',
      }));
      renderChat('client', 'Илья Петров', [
        message({ id: 'bad', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Без имени' }),
        message({ id: 'ok', senderId: OTHER, senderName: 'olga@example.com', senderRole: 'manager', text: 'С именем' }),
      ]);

      expect(await screen.findByText('Ольга Смирнова')).toBeInTheDocument();
      await waitFor(() => expect(getManagerCard).toHaveBeenCalledWith(MARINA));
      const broken = rowOf('Без имени');
      expect(within(broken).queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
      expect(within(broken).queryByText('M')).not.toBeInTheDocument();
      expect(within(broken).queryByText('Менеджер')).not.toBeInTheDocument();
      expect(within(broken).queryByText('Клиент')).not.toBeInTheDocument();
      expect(within(broken).queryByText('Админ')).not.toBeInTheDocument();
      expect(within(broken).queryByText('Сотрудник')).not.toBeInTheDocument();
      expect(within(broken).queryByText('не указано')).not.toBeInTheDocument();
      expect(within(broken).queryByText('Появится позже')).not.toBeInTheDocument();
      expect(screen.queryByText('Не удалось открыть карточку.')).not.toBeInTheDocument();
      expect(errorSpy).not.toHaveBeenCalled();

      const user = userEvent.setup();
      await user.type(screen.getByPlaceholderText('Введите сообщение...'), 'Ответ');
      await user.keyboard('{Enter}');
      expect(await screen.findByText('Ответ')).toBeInTheDocument();
      expect(screen.getByText('Илья Петров')).toBeInTheDocument();
      expect(screen.queryByText(ILYA_EMAIL)).not.toBeInTheDocument();
      errorSpy.mockRestore();
    },
  );

  it.each(['', '   ', undefined] as const)(
    'ТК-6 пустой displayName %j не становится подписью',
    async (displayName) => {
      vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
        if (id === MARINA) return { id, displayName: displayName as string };
        return { id, displayName: 'Ольга Смирнова' };
      });
      renderChat('client', 'Илья Петров', [
        message({ id: 'bad', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Без имени' }),
        message({ id: 'ok', senderId: OTHER, senderName: 'olga@example.com', senderRole: 'manager', text: 'С именем' }),
      ]);
      expect(await screen.findByText('Ольга Смирнова')).toBeInTheDocument();
      await waitFor(() => expect(getManagerCard).toHaveBeenCalledWith(MARINA));
      const broken = rowOf('Без имени');
      expect(within(broken).queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
      expect(within(broken).queryByText('M')).not.toBeInTheDocument();
      expect(within(broken).queryByRole('button', { name: /Карточка менеджера, Марина/ })).not.toBeInTheDocument();
    },
  );

  it('ТК-6 следующий успешный ответ заполняет строки всех сообщений человека', async () => {
    let fail = true;
    vi.mocked(getManagerCard).mockImplementation(async (id: string) => {
      if (id === MARINA && fail) throw new Error('503');
      if (id === MARINA) return { id, displayName: 'Марина Иванова', avatarUrl: PHOTO };
      return { id, displayName: 'Ольга Смирнова' };
    });
    renderChat('client', 'Илья Петров', [
      message({ id: 'bad', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Без имени' }),
      message({ id: 'ok', senderId: OTHER, senderName: 'olga@example.com', senderRole: 'manager', text: 'С именем' }),
    ]);
    expect(await screen.findByText('Ольга Смирнова')).toBeInTheDocument();
    await waitFor(() => expect(within(rowOf('Без имени')).queryByText('M')).not.toBeInTheDocument());

    fail = false;
    push(message({
      id: 'next',
      senderId: MARINA,
      senderName: MARINA_EMAIL,
      senderRole: 'manager',
      text: 'Уже с именем',
      createdAt: '2026-04-02T08:05:00Z',
    }));
    await waitFor(() => expect(screen.getAllByText('Марина Иванова')).toHaveLength(2));
    expect(screen.queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
    expect(screen.getByText('Ольга Смирнова')).toBeInTheDocument();
  });

  it('ТК-7 строка и кружок открывают ту же карточку и держат курсор ссылки', async () => {
    const user = userEvent.setup();
    const view = renderChat('client', 'Илья Петров', [
      message({ id: 'm1', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Здравствуйте' }),
    ]);
    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' })).toHaveLength(2);
    });
    const [circle, name] = screen.getAllByRole('button', { name: 'Карточка менеджера, Марина Иванова' });
    expect(circle).toHaveClass('cursor-pointer');
    expect(name).toHaveClass('cursor-pointer', 'text-left');
    expect(name).not.toHaveClass('text-right');
    expect(circle).toHaveAttribute('type', 'button');
    expect(name).toHaveAttribute('type', 'button');
    expect(name.textContent).toBe('Марина Иванова');
    expect(circle).not.toHaveTextContent(MARINA_EMAIL);

    name.focus();
    await user.keyboard('{Enter}');
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/dashboard/bookings/booking-1');
    expect(screen.getByText('Здравствуйте')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Закрыть' }));
    await user.click(circle);
    expect(await screen.findByRole('dialog', { name: 'Карточка менеджера' })).toBeInTheDocument();
    expect(screen.getByTestId('location').textContent).not.toContain(MARINA);
    view.unmount();

    renderChat('manager', 'Марина Иванова', [
      message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'Добрый день' }),
    ]);
    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: 'Карточка клиента, Анна Соколова' })).toHaveLength(2);
    });
    const [clientCircle, clientName] = screen.getAllByRole('button', { name: 'Карточка клиента, Анна Соколова' });
    expect(clientCircle).toHaveClass('cursor-pointer');
    expect(clientName).toHaveClass('cursor-pointer', 'text-left');
    clientName.focus();
    await user.keyboard(' ');
    expect(await screen.findByRole('dialog', { name: 'Карточка клиента' })).toBeInTheDocument();
    expect(screen.queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/booking-1');
    expect(screen.getByText('Добрый день')).toBeInTheDocument();
  });

  it('ТК-8 своё, служебное и чужой клиент у клиента без кнопки и без курсора', async () => {
    const user = userEvent.setup();
    const view = renderChat('client', 'Илья Петров', [
      message({ id: 'own', senderId: SELF, senderName: ILYA_EMAIL, senderRole: 'client', text: 'Своё' }),
      message({ id: 'system', senderId: 'system', senderName: 'Система', senderRole: 'system', text: 'Служебное' }),
      message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'Чужой клиент' }),
    ]);
    expect(await screen.findByText('Своё')).toBeInTheDocument();
    const own = screen.getByText('Илья Петров');
    expect(own.tagName).toBe('P');
    expect(own).not.toHaveClass('cursor-pointer');
    expect(own.closest('button')).toBeNull();
    const systemMark = screen.getByText('С');
    expect(systemMark.closest('button')).toBeNull();
    expect(systemMark).not.toHaveClass('cursor-pointer');
    expect(within(rowOf('Чужой клиент')).queryByRole('button')).not.toBeInTheDocument();
    expect(within(rowOf('Чужой клиент')).queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
    await user.click(own);
    await user.click(systemMark);
    await user.click(screen.getByText('Чужой клиент'));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(getClientCard).not.toHaveBeenCalled();
    expect(getManagerCard).not.toHaveBeenCalled();
    view.unmount();

    renderChat('manager', 'Марина Иванова', [
      message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'Чужой клиент' }),
    ]);
    const [name] = await screen.findAllByRole('button', { name: 'Карточка клиента, Анна Соколова' });
    await user.click(name);
    expect(await screen.findByRole('dialog', { name: 'Карточка клиента' })).toBeInTheDocument();
  });

  it('ТК-9 буквы кружка из строки имени, не из почты', async () => {
    vi.mocked(getManagerCard).mockResolvedValue({ id: MARINA, displayName: 'Марина Иванова' });
    vi.mocked(getClientCard).mockResolvedValue({
      id: ANNA,
      displayName: 'Анна',
      email: ANNA_EMAIL,
      bookings: [],
      notes: [],
    });
    const client = renderChat('client', 'Илья Петров', [
      message({ id: 'own', senderId: SELF, senderName: ILYA_EMAIL, senderRole: 'client', text: 'Своё' }),
      message({ id: 'marina', senderId: MARINA, senderName: MARINA_EMAIL, senderRole: 'manager', text: 'Чужое' }),
    ]);
    expect(await screen.findByText('ИП')).toBeInTheDocument();
    expect(screen.getByText('МИ')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(screen.queryByText(MARINA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByText(/нет фото/i)).not.toBeInTheDocument();
    client.unmount();

    renderChat('manager', 'Илья Петров', [
      message({ id: 'anna', senderId: ANNA, senderName: ANNA_EMAIL, senderRole: 'client', text: 'Анна пишет' }),
    ]);
    expect(await screen.findByText('А')).toBeInTheDocument();
    expect(screen.queryByText(ANNA_EMAIL)).not.toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('ТК-12 вложение остаётся без курсора ссылки и ничего не отправляет', async () => {
    vi.mocked(sendMessage).mockResolvedValue(message({
      id: 'saved',
      senderId: SELF,
      senderRole: 'client',
      text: 'текст',
    }));
    renderChat('client', 'Илья Петров', []);
    const user = userEvent.setup();
    const attachment = await screen.findByRole('button', { name: 'Появится позже' });
    expect(attachment).toBeDisabled();
    expect(attachment).toHaveClass('cursor-not-allowed');
    expect(attachment.className.split(' ')).not.toContain('cursor-pointer');
    await user.click(attachment);
    expect(sendMessage).not.toHaveBeenCalled();
  });
});
