import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ChatMessage } from '@/types';
import { getMessages, sendMessage } from '@/api/chat';
import ChatWindow from './ChatWindow';

vi.mock('@/api/chat', () => ({
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
  subscribeToMessages: vi.fn(() => () => {}),
  disconnectChat: vi.fn(),
}));

const BOOKING_ID = 'booking-1';

function longHistory(count = 40): ChatMessage[] {
  return Array.from({ length: count }, (_, index) => {
    const fromManager = index % 2 === 0;
    return {
      id: `msg-${index + 1}`,
      bookingId: BOOKING_ID,
      senderId: fromManager ? 'manager-1' : 'client-1',
      senderName: fromManager ? 'Марина' : 'Илья',
      senderRole: fromManager ? 'manager' : 'client',
      text: `Сообщение ${index + 1}`,
      createdAt: new Date(Date.UTC(2026, 3, 2, 8, 0, index)).toISOString(),
    };
  });
}

function renderChat(role: 'client' | 'manager') {
  const isManager = role === 'manager';
  return render(
    <ChatWindow
      bookingId={BOOKING_ID}
      currentUserId={isManager ? 'manager-1' : 'client-1'}
      currentUserName={isManager ? 'Марина' : 'Илья'}
      currentUserRole={role}
    />,
  );
}

describe('ChatWindow long history', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue(longHistory());
  });

  it.each(['client', 'manager'] as const)(
    'keeps the input under the scrolling list for the %s view',
    async (role) => {
      renderChat(role);

      const input = await screen.findByPlaceholderText('Введите сообщение...');
      const list = screen.getByTestId('chat-message-list');

      expect(list).toHaveClass('min-h-0', 'flex-1', 'overflow-y-auto');
      expect(list).toContainElement(screen.getByText('Сообщение 1'));
      expect(list).toContainElement(screen.getByText('Сообщение 40'));
      expect(list).not.toContainElement(input);

      const column = list.parentElement;
      expect(column).toHaveClass('flex', 'h-full', 'min-h-0', 'flex-1', 'flex-col');
      expect(column).toContainElement(input);

      const inputBar = input.parentElement?.parentElement ?? null;
      expect(inputBar).toHaveClass('shrink-0');
      expect(column).toContainElement(inputBar);
      expect(list.compareDocumentPosition(input) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    },
  );
});

describe('ChatWindow attachment stub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMessages).mockResolvedValue([]);
    vi.mocked(sendMessage).mockImplementation(async (bookingId, text, senderId) => ({
      id: 'saved-1',
      bookingId,
      senderId,
      senderName: senderId === 'manager-1' ? 'Марина' : 'Илья',
      senderRole: senderId === 'manager-1' ? 'manager' : 'client',
      text,
      createdAt: new Date().toISOString(),
    }));
  });

  it.each(['client', 'manager'] as const)(
    'keeps the attachment inactive and still sends text for the %s',
    async (role) => {
      renderChat(role);
      const user = userEvent.setup();
      const userId = role === 'manager' ? 'manager-1' : 'client-1';

      const attachment = await screen.findByRole('button', { name: 'Появится позже' });
      expect(attachment).toBeDisabled();
      expect(screen.getByText('Появится позже')).toBeVisible();
      expect(attachment).not.toHaveAttribute('title');
      expect(screen.queryByTitle('Скоро')).not.toBeInTheDocument();
      expect(document.querySelector('input[type="file"]')).toBeNull();

      const input = screen.getByPlaceholderText('Введите сообщение...');
      const send = screen.getAllByRole('button').find((button) => button !== attachment);
      expect(send).toBeDefined();
      expect(send).toBeDisabled();

      await user.click(input);
      await user.keyboard('{Enter}');
      expect(sendMessage).not.toHaveBeenCalled();

      await user.type(input, '   ');
      expect(send).toBeDisabled();
      expect(sendMessage).not.toHaveBeenCalled();

      await user.clear(input);
      await user.type(input, 'Нужна виза?');
      expect(send).toBeEnabled();
      await user.click(send!);

      expect(sendMessage).toHaveBeenCalledWith(BOOKING_ID, 'Нужна виза?', userId);
      expect(await screen.findByText('Нужна виза?')).toBeInTheDocument();
    },
  );
});
