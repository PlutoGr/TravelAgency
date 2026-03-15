import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { BookingStatus } from '@/types';
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

describe('bookings API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('createBooking', () => {
    it('posts to /bookings with tourId and comment, returns mapped Booking', async () => {
      const dto = {
        ...baseBookingDto,
        proposals: [
          {
            id: 'p1',
            bookingId: 'b1',
            managerId: 'm1',
            tourSnapshot: tourSnapshotDto,
            notes: 'Proposal notes',
            isConfirmed: false,
            createdAt: '2025-01-01T12:00:00Z',
          },
        ],
      };
      mockApiClient.post.mockResolvedValue({ data: dto });

      const result = await bookings.createBooking({
        tourId: 't1',
        comment: 'Test comment',
      });

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings', {
        tourId: 't1',
        comment: 'Test comment',
      });
      expect(result).toMatchObject({
        id: 'b1',
        clientId: 'c1',
        tourId: 't1',
        destination: 'Greek Islands Cruise',
        budget: 2500,
        status: 'new',
        managerId: 'm1',
        notes: 'Test comment',
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: '2025-01-02T00:00:00Z',
      });
    });

    it('posts without comment when not provided', async () => {
      mockApiClient.post.mockResolvedValue({ data: baseBookingDto });

      await bookings.createBooking({ tourId: 't2' });

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings', {
        tourId: 't2',
        comment: undefined,
      });
    });

    it('maps DTO without proposals to Booking with empty destination and budget 0', async () => {
      mockApiClient.post.mockResolvedValue({
        data: { ...baseBookingDto, comment: 'No proposal' },
      });

      const result = await bookings.createBooking({
        tourId: 't1',
        comment: 'No proposal',
      });

      expect(result.destination).toBe('');
      expect(result.budget).toBe(0);
      expect(result.managerId).toBeUndefined();
      expect(result.notes).toBe('No proposal');
    });

    it('throws when API fails', async () => {
      mockApiClient.post.mockRejectedValue(new Error('Network error'));

      await expect(
        bookings.createBooking({ tourId: 't1' }),
      ).rejects.toThrow('Network error');
    });
  });

  describe('getMyBookings', () => {
    it('fetches /bookings/my and maps DTOs to Bookings', async () => {
      const dtos = [
        { ...baseBookingDto, id: 'b1', status: 0 },
        {
          ...baseBookingDto,
          id: 'b2',
          status: 2,
          proposals: [
            {
              id: 'p2',
              bookingId: 'b2',
              managerId: 'm2',
              tourSnapshot: tourSnapshotDto,
              isConfirmed: false,
              createdAt: '2025-01-02T00:00:00Z',
            },
          ],
        },
      ];
      mockApiClient.get.mockResolvedValue({ data: dtos });

      const result = await bookings.getMyBookings();

      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings/my');
      expect(result).toHaveLength(2);
      expect(result[0].id).toBe('b1');
      expect(result[0].status).toBe('new');
      expect(result[1].id).toBe('b2');
      expect(result[1].status).toBe('proposal_sent');
      expect(result[1].destination).toBe('Greek Islands Cruise');
    });

    it('filters by status when provided', async () => {
      const dtos = [
        { ...baseBookingDto, id: 'b1', status: 0 },
        { ...baseBookingDto, id: 'b2', status: 2 },
        { ...baseBookingDto, id: 'b3', status: 0 },
      ];
      mockApiClient.get.mockResolvedValue({ data: dtos });

      const result = await bookings.getMyBookings('new');

      expect(result).toHaveLength(2);
      expect(result.every((b) => b.status === 'new')).toBe(true);
    });

    it('returns empty array when API returns empty', async () => {
      mockApiClient.get.mockResolvedValue({ data: [] });

      const result = await bookings.getMyBookings();

      expect(result).toEqual([]);
    });
  });

  describe('getBookingById', () => {
    it('fetches /bookings/{id} and maps DTO to Booking', async () => {
      const dto = {
        ...baseBookingDto,
        id: 'b99',
        proposals: [
          {
            id: 'p99',
            bookingId: 'b99',
            managerId: 'm99',
            tourSnapshot: tourSnapshotDto,
            isConfirmed: true,
            createdAt: '2025-01-01T00:00:00Z',
          },
        ],
      };
      mockApiClient.get.mockResolvedValue({ data: dto });

      const result = await bookings.getBookingById('b99');

      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings/b99');
      expect(result.id).toBe('b99');
      expect(result.destination).toBe('Greek Islands Cruise');
      expect(result.status).toBe('new');
    });

    it('throws when API fails', async () => {
      mockApiClient.get.mockRejectedValue(new Error('Not found'));

      await expect(bookings.getBookingById('nonexistent')).rejects.toThrow(
        'Not found',
      );
    });
  });

  describe('updateBookingStatus', () => {
    it('patches /bookings/{id}/status with backend status and returns mapped Booking', async () => {
      const dto = {
        ...baseBookingDto,
        id: 'b1',
        status: 3,
      };
      mockApiClient.patch.mockResolvedValue({ data: dto });

      const result = await bookings.updateBookingStatus('b1', 'confirmed');

      expect(mockApiClient.patch).toHaveBeenCalledWith(
        '/bookings/b1/status',
        { newStatus: 3 },
      );
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

        expect(mockApiClient.patch).toHaveBeenCalledWith(
          '/bookings/b1/status',
          { newStatus: backend },
        );
      }
    });

    it('uses 0 when unknown status (fallback)', async () => {
      mockApiClient.patch.mockResolvedValue({
        data: baseBookingDto,
      });

      await bookings.updateBookingStatus('b1', 'unknown' as BookingStatus);

      expect(mockApiClient.patch).toHaveBeenCalledWith(
        '/bookings/b1/status',
        { newStatus: 0 },
      );
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

      await expect(
        bookings.confirmProposal('b1', 'invalid'),
      ).rejects.toThrow('Proposal not found');
    });
  });

  describe('createProposal', () => {
    it('posts to /bookings/{bookingId}/proposal with notes, returns ProposalResponse', async () => {
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
      const proposalResponse = {
        id: 'p2',
        bookingId: 'b1',
        managerId: 'm1',
        tourSnapshot: tourSnapshotDto,
        isConfirmed: false,
        createdAt: '2025-01-03T00:00:00Z',
      };
      mockApiClient.post.mockResolvedValue({ data: proposalResponse });

      await bookings.createProposal('b1');

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings/b1/proposal', {
        notes: undefined,
      });
    });

    it('posts with empty object when called with default params', async () => {
      mockApiClient.post.mockResolvedValue({
        data: {
          id: 'p3',
          bookingId: 'b1',
          managerId: 'm1',
          tourSnapshot: tourSnapshotDto,
          isConfirmed: false,
          createdAt: '2025-01-03T00:00:00Z',
        },
      });

      await bookings.createProposal('b1', {});

      expect(mockApiClient.post).toHaveBeenCalledWith('/bookings/b1/proposal', {
        notes: undefined,
      });
    });
  });

  describe('status mapping (mapDtoToBooking)', () => {
    it('maps backend status 0 to new', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 0 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('new');
    });

    it('maps backend status 1 to in_progress', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 1 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('in_progress');
    });

    it('maps backend status 2 to proposal_sent', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 2 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('proposal_sent');
    });

    it('maps backend status 3 to confirmed', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 3 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('confirmed');
    });

    it('maps backend status 4 to closed', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 4 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('closed');
    });

    it('maps backend status 5 (Cancelled) to cancelled', async () => {
      mockApiClient.get.mockResolvedValue({
        data: { ...baseBookingDto, status: 5 },
      });

      const result = await bookings.getBookingById('b1');
      expect(result.status).toBe('cancelled');
    });
  });

  describe('mapDtoToBooking', () => {
    it('maps DTO with proposal to Booking with destination, budget, managerId from first proposal', async () => {
      const dto = {
        ...baseBookingDto,
        comment: 'My comment',
        proposals: [
          {
            id: 'p1',
            bookingId: 'b1',
            managerId: 'manager-123',
            tourSnapshot: {
              ...tourSnapshotDto,
              title: 'Alpine Trek',
              price: 1500,
            },
            isConfirmed: false,
            createdAt: '2025-01-01T00:00:00Z',
          },
        ],
      };
      mockApiClient.get.mockResolvedValue({ data: dto });

      const result = await bookings.getBookingById('b1');

      expect(result.destination).toBe('Alpine Trek');
      expect(result.budget).toBe(1500);
      expect(result.managerId).toBe('manager-123');
      expect(result.notes).toBe('My comment');
      expect(result.clientName).toBe('');
      expect(result.country).toBe('');
      expect(result.dateFrom).toBe('');
      expect(result.dateTo).toBe('');
      expect(result.travelers).toBe(1);
    });

    it('maps null/undefined comment to empty string notes', async () => {
      const dto = { ...baseBookingDto, comment: undefined };
      mockApiClient.get.mockResolvedValue({ data: dto });

      const result = await bookings.getBookingById('b1');
      expect(result.notes).toBe('');
    });
  });

  describe('getAllBookings', () => {
    it('fetches /bookings and returns mapped bookings', async () => {
      mockApiClient.get.mockResolvedValue({ data: [] });

      const result = await bookings.getAllBookings();
      expect(result).toEqual([]);
      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings');
    });

    it('returns filtered bookings when filters provided', async () => {
      mockApiClient.get.mockResolvedValue({ data: [] });

      const result = await bookings.getAllBookings({
        status: 'confirmed',
        search: 'test',
      });
      expect(result).toEqual([]);
      expect(mockApiClient.get).toHaveBeenCalledWith('/bookings');
    });
  });
});
