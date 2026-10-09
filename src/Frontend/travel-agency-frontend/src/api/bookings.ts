import type {
  Booking,
  BookingProposal,
  BookingStatus,
  BookingTourSnapshot,
  Tour,
} from '@/types';
import { apiClient } from './client';
import { getTourCards, unavailableTour } from './catalog';

/** Backend BookingStatus enum: New=0, InProgress=1, ProposalSent=2, Confirmed=3, Closed=4, Cancelled=5 */
type BackendBookingStatus = 0 | 1 | 2 | 3 | 4 | 5;

const BACKEND_TO_FRONTEND_STATUS: Record<BackendBookingStatus, BookingStatus> = {
  0: 'new',
  1: 'in_progress',
  2: 'proposal_sent',
  3: 'confirmed',
  4: 'closed',
  5: 'cancelled',
};

const FRONTEND_TO_BACKEND_STATUS: Record<BookingStatus, BackendBookingStatus> = {
  new: 0,
  in_progress: 1,
  proposal_sent: 2,
  confirmed: 3,
  closed: 4,
  cancelled: 5,
};

function asRecord(value: unknown): Record<string, unknown> | null {
  if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  return null;
}

function asString(value: unknown): string {
  return typeof value === 'string' ? value : '';
}

function asNullableString(value: unknown): string | null {
  if (value == null) return null;
  return typeof value === 'string' ? value : null;
}

function asNumber(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) return parsed;
  }
  return 0;
}

function mapStatus(value: unknown): BookingStatus {
  if (typeof value !== 'number' || !Number.isInteger(value)) return 'new';
  return BACKEND_TO_FRONTEND_STATUS[value as BackendBookingStatus] ?? 'new';
}

function mapSnapshot(raw: unknown): BookingTourSnapshot {
  const dto = asRecord(raw) ?? {};
  return {
    tourId: asString(dto.tourId),
    title: asString(dto.title),
    description: asString(dto.description),
    price: asNumber(dto.price),
    currency: asString(dto.currency),
    durationDays: asNumber(dto.durationDays),
    snapshotTakenAt: asString(dto.snapshotTakenAt),
  };
}

function mapProposal(raw: unknown): BookingProposal {
  const dto = asRecord(raw) ?? {};
  return {
    id: asString(dto.id),
    bookingId: asString(dto.bookingId),
    managerId: asString(dto.managerId),
    tourSnapshot: mapSnapshot(dto.tourSnapshot),
    notes: asNullableString(dto.notes),
    isConfirmed: dto.isConfirmed === true,
    createdAt: asString(dto.createdAt),
  };
}

/**
 * Единственная точка, где сырой JSON брони становится моделью экрана.
 * Ожидается camelCase-тело BookingDto (System.Text.Json в ASP.NET Core).
 * Полей дат поездки, направления, числа туристов и бюджета в контракте нет — они не читаются.
 */
export function mapBookingDto(raw: unknown): Booking {
  const dto = asRecord(raw);
  if (!dto) {
    throw new Error('BookingDto: ожидался объект');
  }

  const proposals = Array.isArray(dto.proposals) ? dto.proposals.map(mapProposal) : [];

  return {
    id: asString(dto.id),
    clientId: asString(dto.clientId),
    tourId: asString(dto.tourId),
    comment: asNullableString(dto.comment),
    status: mapStatus(dto.status),
    createdAt: asString(dto.createdAt),
    updatedAt: asNullableString(dto.updatedAt),
    proposals,
    clientName: asNullableString(dto.clientName),
    clientEmail: asNullableString(dto.clientEmail),
    clientPhone: asNullableString(dto.clientPhone),
  };
}

/** Название тура для экрана: из каталога, как у избранного. */
export function bookingTourTitle(booking: Pick<Booking, 'tour'>): string {
  const title = booking.tour?.title?.trim();
  return title || 'Тур недоступен';
}

async function withCatalogTour(booking: Booking): Promise<Booking> {
  const [withTour] = await withCatalogTours([booking]);
  return withTour ?? { ...booking, tour: unavailableTour(booking.tourId || 'unknown') };
}

function withUnavailableTours(bookings: Booking[]): Booking[] {
  return bookings.map((booking) => ({
    ...booking,
    tour: unavailableTour(booking.tourId || 'unknown'),
  }));
}

async function withCatalogTours(bookings: Booking[]): Promise<Booking[]> {
  const ids = bookings.map((booking) => booking.tourId).filter(Boolean);
  if (ids.length === 0) return withUnavailableTours(bookings);

  // Каталог вторичен: его отказ не должен прятать уже загруженные брони.
  let cards: Tour[] = [];
  try {
    cards = await getTourCards(ids);
  } catch {
    return withUnavailableTours(bookings);
  }

  const byId = new Map(cards.map((tour) => [tour.id, tour]));
  return bookings.map((booking) => ({
    ...booking,
    tour: byId.get(booking.tourId) ?? unavailableTour(booking.tourId || 'unknown'),
  }));
}

function mapList(data: unknown): Booking[] {
  const items = Array.isArray(data) ? data : [];
  return items.map(mapBookingDto);
}

export interface CreateBookingParams {
  tourId: string;
  comment?: string;
}

export async function createBooking(params: CreateBookingParams): Promise<Booking> {
  const { data } = await apiClient.post<unknown>('/bookings', {
    tourId: params.tourId,
    comment: params.comment,
  });
  return withCatalogTour(mapBookingDto(data));
}

export async function getMyBookings(status?: BookingStatus): Promise<Booking[]> {
  const { data } = await apiClient.get<unknown>('/bookings/my');
  let bookings = await withCatalogTours(mapList(data));
  if (status) {
    bookings = bookings.filter((booking) => booking.status === status);
  }
  return bookings;
}

export async function getBookingById(id: string): Promise<Booking> {
  const { data } = await apiClient.get<unknown>(`/bookings/${id}`);
  return withCatalogTour(mapBookingDto(data));
}

export async function updateBookingStatus(
  id: string,
  status: BookingStatus,
): Promise<Booking> {
  const backendStatus = FRONTEND_TO_BACKEND_STATUS[status] ?? 0;
  const { data } = await apiClient.patch<unknown>(`/bookings/${id}/status`, {
    newStatus: backendStatus,
  });
  return withCatalogTour(mapBookingDto(data));
}

export async function confirmProposal(
  bookingId: string,
  proposalId: string,
): Promise<Booking> {
  const { data } = await apiClient.post<unknown>(`/bookings/${bookingId}/confirm`, {
    proposalId,
  });
  return withCatalogTour(mapBookingDto(data));
}

export interface CreateProposalParams {
  notes?: string;
}

export async function createProposal(
  bookingId: string,
  params: CreateProposalParams = {},
): Promise<BookingProposal> {
  const { data } = await apiClient.post<unknown>(`/bookings/${bookingId}/proposal`, {
    notes: params.notes,
  });
  return mapProposal(data);
}

/** Предложение, которое клиент ещё может подтвердить. */
export function pendingProposal(booking: Booking): BookingProposal | undefined {
  return booking.proposals.find((proposal) => !proposal.isConfirmed);
}

/** Manager-only: returns all bookings. Requires manager role. */
export async function getAllBookings(filters?: {
  status?: BookingStatus;
  search?: string;
}): Promise<Booking[]> {
  const { data } = await apiClient.get<unknown>('/bookings');
  let bookings = await withCatalogTours(mapList(data));
  if (filters?.status) {
    bookings = bookings.filter((booking) => booking.status === filters.status);
  }
  return bookings;
}
