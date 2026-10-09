import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { BookingStatus } from '@/types';
import {
  BOOKING_MINIMAL_JSON,
  BOOKING_WITH_PROPOSAL_JSON,
  parseBookingJson,
} from '@/test/bookingDtoJson';
import * as bookings from './bookings';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    post: vi.fn(),
    get: vi.fn(),
    patch: vi.fn(),
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
}));

const baseBookingDto = {
  id: 'b1',
  clientId: 'c1',
  tourId: 't1',
  comment: 'Test comment',
  status: 0 as const,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-02T00:00:00Z',
  proposals: [] as unknown[],
  clientName: null,
  clientEmail: null,
  clientPhone: null,
};

const tourSnapshotDto = {
  tourId: 't1',
  title: 'Greek Islands Cruise',
  description: 'Sail the Aegean',
  price: 2500,
  currency: 'EUR',
  durationDays: 10,
  snapshotTakenAt: '2025-01-01T00:00:00Z',
};

const catalogCard = {
  id: 't1',
  title: 'Солнечная Греция — Санторини',
  available: true,
  priceFrom: 148000,
  currency: 'RUB',
  country: 'Греция',
  durationDays: 7,
};

function installGet(data: unknown, cards: unknown[] = []) {
  mockApiClient.get.mockImplementation((url: string) => {
    if (String(url).startsWith('/catalog/tours/cards')) {
      return Promise.resolve({ data: cards });
    }
    return Promise.resolve({ data });
  });
}

describe('bookings API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    installGet(baseBookingDto);
  });

  describe('mapBookingDto', () => {
    it('maps JSON in the exact BookingDto shape', () => {
      const result = bookings.mapBookingDto(parseBookingJson(BOOKING_WITH_PROPOSAL_JSON));

      expect(result).toEqual({
        id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
        clientId: '11111111-1111-1111-1111-111111111111',
        tourId: '22222222-2222-2222-2222-222222222222',
        comment: 'Хочу поехать в мае, двое взрослых',
        status: 'proposal_sent',
        createdAt: '2026-04-15T10:30:00Z',
        updatedAt: '2026-04-16T08:00:00Z',
        proposals: [
          {
            id: '33333333-3333-3333-3333-333333333333',
            bookingId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
            managerId: '44444444-4444-4444-4444-444444444444',
            tourSnapshot: {
              tourId: '22222222-2222-2222-2222-222222222222',
              title: 'Санторини',
              description: 'Неделя на кальдере',
              price: 148000.5,
              currency: 'RUB',
              durationDays: 7,
              snapshotTakenAt: '2026-04-16T08:00:00Z',
            },
            notes: 'Отель с видом на кальдеру',
            isConfirmed: false,
            createdAt: '2026-04-16T08:00:00Z',
          },
        ],
        clientName: 'Анна Смирнова',
        clientEmail: 'anna@example.com',
        clientPhone: '+79990001122',
      });
      expect(result).not.toHaveProperty('dateFrom');
      expect(result).not.toHaveProperty('dateTo');
      expect(result).not.toHaveProperty('destination');
      expect(result).not.toHaveProperty('travelers');
      expect(result).not.toHaveProperty('budget');
    });

    it('maps a booking with null comment, dates and client fields', () => {
      const result = bookings.mapBookingDto(parseBookingJson(BOOKING_MINIMAL_JSON));

      expect(result.comment).toBeNull();
      expect(result.updatedAt).toBeNull();
      expect(result.clientName).toBeNull();
      expect(result.proposals).toEqual([]);
      expect(result.status).toBe('new');
      expect(result.tourId).toBe('99999999-9999-9999-9999-999999999999');
    });

    it('throws when the payload is not an object', () => {
      expect(() => bookings.mapBookingDto(null)).toThrow(/BookingDto/);
    });
  });

  describe('createBooking', () => {
    it('posts to /bookings with tourId and comment only, returns mapped Booking', async () => {
      const dto = parseBookingJson(BOOKING_WITH_PROPOSAL_JSON);
      mockApiClient.post.mockResolvedValue({ data: dto });
      installGet(dto, [
        { ...catalogCard, id: '22222222-2222-2222-2222-222222222222' },
      ]);

      const result = await bookings.createBooking({
        tourId: '22222222-2222-2222-2222-222222222222',
        comment: 'Хочу поехать в мае, двое взрослых',
      });

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings', {
        tourId: '22222222-2222-2222-2222-222222222222',
        comment: 'Хочу поехать в мае, двое взрослых',
      });
      const body = mockApiClient.post.mock.calls[0][1] as Record<string, unknown>;
      expect(Object.keys(body).sort()).toEqual(['comment', 'tourId']);
      expect(result.status).toBe('proposal_sent');
      expect(result.comment).toBe('Хочу поехать в мае, двое взрослых');
      expect(result.tour?.title).toBe('Солнечная Греция — Санторини');
    });

    it('posts without comment when not provided', async () => {
      mockApiClient.post.mockResolvedValue({ data: baseBookingDto });

      await bookings.createBooking({ tourId: 't2' });

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings', {
        tourId: 't2',
        comment: undefined,
      });
    });

    it('maps a DTO without proposals and marks a missing catalog card unavailable', async () => {
      mockApiClient.post.mockResolvedValue({
        data: { ...baseBookingDto, comment: 'No proposal', proposals: [] },
      });

      const result = await bookings.createBooking({
        tourId: 't1',
        comment: 'No proposal',
      });

      expect(result.comment).toBe('No proposal');
      expect(result.proposals).toEqual([]);
      expect(result.tour?.title).toBe('Тур недоступен');
      expect(result.tour?.available).toBe(false);
    });

    it('throws when API fails', async () => {
      mockApiClient.post.mockRejectedValue(new Error('Network error'));

      await expect(bookings.createBooking({ tourId: 't1' })).rejects.toThrow('Network error');
    });
  });

  describe('getMyBookings', () => {
    it('fetches /bookings/my, maps DTOs and loads catalog cards by tourId', async () => {
      const dtos = [
        { ...baseBookingDto, id: 'b1', status: 0, tourId: 't1' },
        {
          ...baseBookingDto,
          id: 'b2',
          status: 2,
          tourId: 't1',
          proposals: [
            {
              id: 'p2',
              bookingId: 'b2',
              managerId: 'm2',
              tourSnapshot: tourSnapshotDto,
              notes: null,
              isConfirmed: false,
              createdAt: '2025-01-02T00:00:00Z',
            },
          ],
        },
      ];
      installGet(dtos, [catalogCard]);

      const result = await bookings.getMyBookings();

      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings/my');
      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours/cards?ids=t1');
      expect(result).toHaveLength(2);
      expect(result[0].status).toBe('new');
      expect(result[1].status).toBe('proposal_sent');
      expect(result[1].tour?.title).toBe('Солнечная Греция — Санторини');
      expect(result[1].proposals[0]?.tourSnapshot.title).toBe('Greek Islands Cruise');
    });

    it('filters by status when provided', async () => {
      installGet([
        { ...baseBookingDto, id: 'b1', status: 0 },
        { ...baseBookingDto, id: 'b2', status: 2 },
        { ...baseBookingDto, id: 'b3', status: 0 },
      ]);

      const result = await bookings.getMyBookings('new');

      expect(result).toHaveLength(2);
      expect(result.every((b) => b.status === 'new')).toBe(true);
    });

    it('returns empty array when API returns empty', async () => {
      installGet([]);

      const result = await bookings.getMyBookings();

      expect(result).toEqual([]);
    });
  });

  describe('getBookingById', () => {
    it('fetches /bookings/{id} and maps DTO to Booking', async () => {
      installGet(parseBookingJson(BOOKING_WITH_PROPOSAL_JSON), [
        { ...catalogCard, id: '22222222-2222-2222-2222-222222222222' },
      ]);

      const result = await bookings.getBookingById('3fa85f64-5717-4562-b3fc-2c963f66afa6');

      expect(mockApiClient.get).toHaveBeenCalledWith(
        '/bookings/3fa85f64-5717-4562-b3fc-2c963f66afa6',
      );
      expect(result.tour?.title).toBe('Солнечная Греция — Санторини');
      expect(result.status).toBe('proposal_sent');
    });

    it('throws when API fails', async () => {
      mockApiClient.get.mockRejectedValue(new Error('Not found'));

      await expect(bookings.getBookingById('nonexistent')).rejects.toThrow('Not found');
    });
  });

  describe('updateBookingStatus', () => {
    it('patches /bookings/{id}/status with backend status and returns mapped Booking', async () => {
      installGet({ ...baseBookingDto, id: 'b1', status: 3 });
      mockApiClient.patch.mockResolvedValue({
        data: { ...baseBookingDto, id: 'b1', status: 3 },
      });

      const result = await bookings.updateBookingStatus('b1', 'confirmed');

      expect(mockApiClient.patch).toHaveBeenCalledWith('/bookings/b1/status', { newStatus: 3 });
      expect(result.status).toBe('confirmed');
    });

    it('maps frontend status to backend: new->0, in_progress->1, proposal_sent->2, confirmed->3, closed->4, cancelled->5', async () => {
      const statusMap: Array<[BookingStatus, number]> = [
        ['new', 0],
        ['in_progress', 1],
        ['proposal_sent', 2],
        ['confirmed', 3],
        ['closed', 4],
        ['cancelled', 5],
      ];

      for (const [frontend, backend] of statusMap) {
        mockApiClient.patch.mockResolvedValue({
          data: { ...baseBookingDto, status: backend },
        });

        await bookings.updateBookingStatus('b1', frontend);

        expect(mockApiClient.patch).toHaveBeenCalledWith('/bookings/b1/status', {
          newStatus: backend,
        });
      }
    });

    it('uses 0 when unknown status (fallback)', async () => {
      mockApiClient.patch.mockResolvedValue({ data: baseBookingDto });

      await bookings.updateBookingStatus('b1', 'unknown' as BookingStatus);

      expect(mockApiClient.patch).toHaveBeenCalledWith('/bookings/b1/status', { newStatus: 0 });
    });
  });

  describe('confirmProposal', () => {
    it('posts to /bookings/{bookingId}/confirm with proposalId, returns mapped Booking', async () => {
      const dto = {
        ...baseBookingDto,
        id: 'b1',
        status: 3,
        proposals: [
          {
            id: 'p1',
            bookingId: 'b1',
            managerId: 'm1',
            tourSnapshot: tourSnapshotDto,
            notes: null,
            isConfirmed: true,
            createdAt: '2025-01-01T00:00:00Z',
          },
        ],
      };
      mockApiClient.post.mockResolvedValue({ data: dto });

      const result = await bookings.confirmProposal('b1', 'p1');

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings/b1/confirm', {
        proposalId: 'p1',
      });
      expect(result.id).toBe('b1');
      expect(result.status).toBe('confirmed');
    });

    it('throws when API fails', async () => {
      mockApiClient.post.mockRejectedValue(new Error('Proposal not found'));

      await expect(bookings.confirmProposal('b1', 'invalid')).rejects.toThrow('Proposal not found');
    });
  });

  describe('createProposal', () => {
    it('posts to /bookings/{bookingId}/proposal with notes and maps ProposalDto', async () => {
      const proposalResponse = {
        id: 'p-new',
        bookingId: 'b1',
        managerId: 'm1',
        tourSnapshot: tourSnapshotDto,
        notes: 'Custom notes',
        isConfirmed: false,
        createdAt: '2025-01-03T00:00:00Z',
      };
      mockApiClient.post.mockResolvedValue({ data: proposalResponse });

      const result = await bookings.createProposal('b1', { notes: 'Custom notes' });

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings/b1/proposal', {
        notes: 'Custom notes',
      });
      expect(result).toEqual(proposalResponse);
    });

    it('posts with empty notes when params not provided', async () => {
      mockApiClient.post.mockResolvedValue({
        data: {
          id: 'p2',
          bookingId: 'b1',
          managerId: 'm1',
          tourSnapshot: tourSnapshotDto,
          isConfirmed: false,
          createdAt: '2025-01-03T00:00:00Z',
        },
      });

      const result = await bookings.createProposal('b1');

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings/b1/proposal', {
        notes: undefined,
      });
      expect(result.notes).toBeNull();
    });
  });

  describe('status mapping', () => {
    it.each([
      [0, 'new'],
      [1, 'in_progress'],
      [2, 'proposal_sent'],
      [3, 'confirmed'],
      [4, 'closed'],
      [5, 'cancelled'],
    ] as const)('maps backend status %s to %s', async (backend, frontend) => {
      installGet({ ...baseBookingDto, status: backend });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe(frontend);
    });
  });

  describe('getAllBookings', () => {
    it('fetches /bookings and returns mapped bookings', async () => {
      installGet([]);

      const result = await bookings.getAllBookings();
      expect(result).toEqual([]);
      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings');
    });

    it('returns filtered bookings when filters provided', async () => {
      installGet([
        { ...baseBookingDto, id: 'b1', status: 3 },
        { ...baseBookingDto, id: 'b2', status: 0 },
      ]);

      const result = await bookings.getAllBookings({
        status: 'confirmed',
        search: 'test',
      });
      expect(result.map((booking) => booking.id)).toEqual(['b1']);
      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings');
    });
  });
});
