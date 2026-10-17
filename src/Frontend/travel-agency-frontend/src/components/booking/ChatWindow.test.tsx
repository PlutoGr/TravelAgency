import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { ChatMessage } from '@/types';
import { getMessages } from '@/api/chat';
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
