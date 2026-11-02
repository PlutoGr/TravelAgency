import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mapBookingDto } from './bookings';
import { mapDtoToMessage } from './chat';
import { mapUserProfileDtoToUser } from './auth';
import {
  clientBookingTitles,
  clientNoteFieldError,
  getClientCard,
  getClientList,
  mapClientCard,
  mapClientListItem,
} from './clientCards';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
}));

const anna = {
  id: '6b1c2a0e-7b4d-4e1a-9c2f-6a8b0d1e2f30',
  displayName: 'Анна Соколова',
  email: 'anna@example.com',
  avatarUrl: '/api/v1/media/files/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/a256',
  phone: '+79991234567',
  bookings: [
    { id: 'b-new', tourId: 'tour-known', status: 0, createdAt: '2026-11-02T10:00:00Z' },
    { id: 'b-old', tourId: 'tour-missing', status: 5, createdAt: '2026-11-01T10:00:00Z' },
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

describe('client card API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('omits phone and avatar when the keys are absent and keeps status numbers', () => {
    const { phone: _phone, avatarUrl: _avatar, ...without } = anna;
    const card = mapClientCard(without);
    expect(card).not.toHaveProperty('phone');
    expect(card).not.toHaveProperty('avatarUrl');
    expect(card.bookings.map((item) => item.status)).toEqual(['new', 'cancelled']);
    expect(card.notes).toHaveLength(1);

    const row = mapClientListItem({
      id: anna.id,
      displayName: anna.displayName,
      email: anna.email,
      bookingCount: 2,
    });
    expect(row).not.toHaveProperty('phone');
    expect(row).not.toHaveProperty('avatarUrl');
    expect(row.bookingCount).toBe(2);
    expect(row).not.toHaveProperty('notes');
    expect(row).not.toHaveProperty('bookings');
  });

  it('loads the card and the list from the clients endpoints', async () => {
    mockApiClient.get.mockImplementation((url: string) => {
      if (url === '/bookings/clients') return Promise.resolve({ data: [anna] });
      return Promise.resolve({ data: anna });
    });

    const card = await getClientCard(anna.id);
    const list = await getClientList();
    expect(mockApiClient.get).toHaveBeenCalledWith(`/bookings/clients/${anna.id}`);
    expect(mockApiClient.get).toHaveBeenCalledWith('/bookings/clients');
    expect(card.phone).toBe('+79991234567');
    expect(list[0]?.phone).toBe('+79991234567');
  });

  it('takes tour titles from the same catalog enrichment', async () => {
    mockApiClient.get.mockImplementation((url: string) => {
      if (String(url).includes('tour-known')) {
        return Promise.resolve({
          data: [{ id: 'tour-known', title: 'Солнечная Греция', country: '', tourType: '', durationDays: 7, coverMediaFileId: null, available: true }],
        });
      }
      return Promise.resolve({ data: [] });
    });

    const titles = await clientBookingTitles(mapClientCard(anna).bookings);
    expect(titles.get('b-new')).toBe('Солнечная Греция');
    expect(titles.get('b-old')).toBe('Тур недоступен');
  });

  it('reads the 400 phrase from errors.text', () => {
    expect(
      clientNoteFieldError({
        response: { status: 400, data: { title: 'Validation Error', errors: { text: ['Напишите текст заметки.'] } } },
      }),
    ).toBe('Напишите текст заметки.');
    expect(clientNoteFieldError({ response: { status: 503, data: { detail: 'Upstream service unavailable' } } })).toBeNull();
  });

  it('does not keep a client note on booking, profile or chat payloads', () => {
    const booking = mapBookingDto({
      id: 'b1',
      clientId: 'c1',
      tourId: 't1',
      comment: null,
      status: 0,
      createdAt: '2026-11-02T09:00:00Z',
      updatedAt: null,
      proposals: [],
      clientPhone: null,
      notes: [{ text: 'просит вылет после 15 мая' }],
    });
    expect(booking).not.toHaveProperty('notes');
    expect(JSON.stringify(booking)).not.toContain('просит вылет после 15 мая');

    const user = mapUserProfileDtoToUser({
      id: 'c1',
      email: 'anna@example.com',
      firstName: 'Анна',
      lastName: 'Соколова',
      phone: '+79991234567',
      role: 'client',
      createdAt: '2026-11-02T09:00:00Z',
      notes: [{ text: 'просит вылет после 15 мая' }],
    } as never);
    expect(user).not.toHaveProperty('notes');

    const message = mapDtoToMessage({
      id: 'm1',
      bookingId: 'b1',
      senderId: 'c1',
      senderName: 'Анна',
      senderRole: 'client',
      text: 'Здравствуйте',
      createdAt: '2026-11-02T09:00:00Z',
      notes: ['просит вылет после 15 мая'],
    } as never);
    expect(message).not.toHaveProperty('notes');
    expect(message).not.toHaveProperty('phone');
  });
});
