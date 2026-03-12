import type { ChatMessage } from '@/types';
import { mockMessages } from '@/mocks/messages';

const delay = () => new Promise((r) => setTimeout(r, 300 + Math.random() * 200));

let messagesState = [...mockMessages];

export async function getMessages(bookingId: string): Promise<ChatMessage[]> {
  await delay();
  return messagesState
    .filter((m) => m.bookingId === bookingId)
    .sort(
      (a, b) =>
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    );
}

export async function sendMessage(
  bookingId: string,
  text: string,
): Promise<ChatMessage> {
  await delay();

  const newMessage: ChatMessage = {
    id: 'msg-' + (messagesState.length + 1),
    bookingId,
    senderId: 'user-1',
    senderName: 'Иван Иванов',
    senderRole: 'client',
    text,
    createdAt: new Date().toISOString(),
  };

  messagesState = [...messagesState, newMessage];
  return newMessage;
}
