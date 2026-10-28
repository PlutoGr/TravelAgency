import { describe, expect, it } from 'vitest';
import { mapDtoToMessage } from './chat';

describe('mapDtoToMessage', () => {
  it('сохраняет admin и system отдельно от client', () => {
    const base = {
      id: 'm1',
      bookingId: 'b1',
      senderId: 'u1',
      senderName: 'Имя',
      text: 'текст',
      createdAt: '2026-01-01T00:00:00Z',
    };

    expect(mapDtoToMessage({ ...base, senderRole: 'Manager' }).senderRole).toBe('manager');
    expect(mapDtoToMessage({ ...base, senderRole: 'Admin' }).senderRole).toBe('admin');
    expect(mapDtoToMessage({ ...base, senderRole: 'admin' }).senderRole).toBe('admin');
    expect(mapDtoToMessage({ ...base, senderRole: 'SYSTEM' }).senderRole).toBe('system');
    expect(mapDtoToMessage({ ...base, senderRole: 'Client' }).senderRole).toBe('client');
  });
});
