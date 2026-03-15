import * as signalR from '@microsoft/signalr';
import type { ChatMessage } from '@/types';
import { apiClient } from '@/api/client';

/** DTO shape from backend ChatMessageDto */
interface ChatMessageDto {
  id: string;
  bookingId: string;
  senderId: string;
  senderName: string;
  senderRole: string;
  text: string;
  attachments?: string[] | null;
  createdAt: string;
}

function mapDtoToMessage(dto: ChatMessageDto): ChatMessage {
  const senderRole =
    dto.senderRole?.toLowerCase() === 'manager' ? 'manager' : 'client';
  return {
    id: String(dto.id),
    bookingId: String(dto.bookingId),
    senderId: String(dto.senderId),
    senderName: dto.senderName ?? '',
    senderRole,
    text: dto.text,
    attachments: dto.attachments ?? undefined,
    createdAt: dto.createdAt,
  };
}

function getHubBaseUrl(): string {
  const override = import.meta.env.VITE_CHAT_HUB_URL;
  if (override && typeof override === 'string' && override.trim() !== '') {
    return override.trim();
  }
  const base = typeof window !== 'undefined' ? window.location.origin : '';
  return `${base}/api/v1/chat/hubs/chat`;
}

const connectionCache = new Map<string, signalR.HubConnection>();

function getOrCreateConnection(bookingId: string): signalR.HubConnection {
  let conn = connectionCache.get(bookingId);
  if (conn) return conn;

  const hubUrl = getHubBaseUrl();
  conn = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, { withCredentials: true })
    .withAutomaticReconnect()
    .build();

  connectionCache.set(bookingId, conn);
  return conn;
}

async function ensureConnected(conn: signalR.HubConnection): Promise<void> {
  if (conn.state === signalR.HubConnectionState.Connected) return;
  if (conn.state === signalR.HubConnectionState.Connecting) {
    await new Promise<void>((resolve, reject) => {
      const timeout = setTimeout(() => reject(new Error('Connection timeout')), 10000);
      const check = () => {
        if (conn.state === signalR.HubConnectionState.Connected) {
          clearTimeout(timeout);
          resolve();
        } else if (conn.state === signalR.HubConnectionState.Disconnected) {
          clearTimeout(timeout);
          reject(new Error('Connection failed'));
        } else {
          setTimeout(check, 50);
        }
      };
      setTimeout(check, 50);
    });
    return;
  }
  await conn.start();
}

/**
 * Fetches chat messages for a booking via REST API.
 */
export async function getMessages(bookingId: string): Promise<ChatMessage[]> {
  const { data } = await apiClient.get<ChatMessageDto[]>(
    `chat/booking/${bookingId}/messages`,
  );
  const list = Array.isArray(data) ? data : [];
  return list.map(mapDtoToMessage).sort(
    (a, b) =>
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
  );
}

/**
 * Sends a chat message via SignalR hub. Resolves when the server broadcasts
 * MessageReceived (with our message). Requires currentUserId to match the response.
 */
export async function sendMessage(
  bookingId: string,
  text: string,
  currentUserId: string,
  attachments?: string[],
): Promise<ChatMessage> {
  const conn = getOrCreateConnection(bookingId);

  const resultPromise = new Promise<ChatMessage>((resolve, reject) => {
    const handler = (dto: ChatMessageDto) => {
      const msg = mapDtoToMessage(dto);
      if (msg.senderId === currentUserId && msg.text === text) {
        conn.off('MessageReceived', handler);
        clearTimeout(timeout);
        resolve(msg);
      }
    };
    conn.on('MessageReceived', handler);

    const timeout = setTimeout(() => {
      conn.off('MessageReceived', handler);
      reject(new Error('Send message timeout'));
    }, 15000);
  });

  await ensureConnected(conn);
  await conn.invoke('JoinBookingGroup', bookingId);
  await conn.invoke('SendMessage', bookingId, text, attachments ?? null);

  return resultPromise;
}

/**
 * Subscribes to real-time messages for a booking. Call on ChatWindow mount.
 * The callback receives new messages (from any sender). Call disconnectChat on unmount.
 */
export function subscribeToMessages(
  bookingId: string,
  onMessage: (msg: ChatMessage) => void,
): () => void {
  const conn = getOrCreateConnection(bookingId);

  const handler = (dto: ChatMessageDto) => {
    onMessage(mapDtoToMessage(dto));
  };
  conn.on('MessageReceived', handler);

  void ensureConnected(conn).then(() => conn.invoke('JoinBookingGroup', bookingId));

  return () => {
    conn.off('MessageReceived', handler);
  };
}

/**
 * Disconnects and removes the cached connection for a booking.
 * Call when leaving the chat (e.g. ChatWindow unmount).
 */
export function disconnectChat(bookingId: string): void {
  const conn = connectionCache.get(bookingId);
  if (conn) {
    connectionCache.delete(bookingId);
    void conn.stop();
  }
}
