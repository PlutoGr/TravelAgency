import type { BookingStatus } from '@/types';
import { apiClient } from './client';
import { bookingTourTitle, catalogToursById } from './bookings';
import { unavailableTour } from './catalog';

type BackendBookingStatus = 0 | 1 | 2 | 3 | 4 | 5;

const BACKEND_TO_FRONTEND_STATUS: Record<BackendBookingStatus, BookingStatus> = {
  0: 'new',
  1: 'in_progress',
  2: 'proposal_sent',
  3: 'confirmed',
  4: 'closed',
  5: 'cancelled',
};

export type ClientCardBooking = {
  id: string;
  tourId: string;
  status: BookingStatus;
  createdAt: string;
};

export type ClientNote = {
  id: string;
  text: string;
  authorName: string;
  createdAt: string;
};

export type ClientCard = {
  id: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  phone?: string;
  bookings: ClientCardBooking[];
  notes: ClientNote[];
};

export type ClientListItem = {
  id: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  phone?: string;
  bookingCount: number;
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

function optionalText(value: unknown): string | undefined {
  return typeof value === 'string' && value.length > 0 ? value : undefined;
}

function mapStatus(value: unknown): BookingStatus {
  if (typeof value !== 'number' || !Number.isInteger(value)) return 'new';
  return BACKEND_TO_FRONTEND_STATUS[value as BackendBookingStatus] ?? 'new';
}

export function mapClientNote(raw: unknown): ClientNote {
  const dto = asRecord(raw) ?? {};
  return {
    id: asString(dto.id),
    text: asString(dto.text),
    authorName: asString(dto.authorName),
    createdAt: asString(dto.createdAt),
  };
}

export function mapClientCard(raw: unknown): ClientCard {
  const dto = asRecord(raw) ?? {};
  const bookings = Array.isArray(dto.bookings) ? dto.bookings.map(mapBooking) : [];
  const notes = Array.isArray(dto.notes) ? dto.notes.map(mapClientNote) : [];
  const avatarUrl = optionalText(dto.avatarUrl);
  const phone = optionalText(dto.phone);
  return {
    id: asString(dto.id),
    displayName: asString(dto.displayName),
    email: asString(dto.email),
    ...(avatarUrl ? { avatarUrl } : {}),
    ...(phone ? { phone } : {}),
    bookings,
    notes,
  };
}

function mapBooking(raw: unknown): ClientCardBooking {
  const dto = asRecord(raw) ?? {};
  return {
    id: asString(dto.id),
    tourId: asString(dto.tourId),
    status: mapStatus(dto.status),
    createdAt: asString(dto.createdAt),
  };
}

export function mapClientListItem(raw: unknown): ClientListItem {
  const dto = asRecord(raw) ?? {};
  const avatarUrl = optionalText(dto.avatarUrl);
  const phone = optionalText(dto.phone);
  const count = dto.bookingCount;
  return {
    id: asString(dto.id),
    displayName: asString(dto.displayName),
    email: asString(dto.email),
    ...(avatarUrl ? { avatarUrl } : {}),
    ...(phone ? { phone } : {}),
    bookingCount: typeof count === 'number' && Number.isFinite(count) ? count : 0,
  };
}

export async function getClientCard(clientId: string): Promise<ClientCard> {
  const { data } = await apiClient.get<unknown>(`/bookings/clients/${clientId}`);
  return mapClientCard(data);
}

export async function getClientList(): Promise<ClientListItem[]> {
  const { data } = await apiClient.get<unknown>('/bookings/clients');
  return Array.isArray(data) ? data.map(mapClientListItem) : [];
}

export async function addClientNote(clientId: string, text: string): Promise<ClientNote> {
  const { data } = await apiClient.post<unknown>(`/bookings/clients/${clientId}/notes`, { text });
  return mapClientNote(data);
}

export function clientNoteFieldError(error: unknown): string | null {
  if (!error || typeof error !== 'object') return null;
  const response = (error as { response?: { status?: number; data?: unknown } }).response;
  if (response?.status !== 400) return null;
  const data = asRecord(response.data);
  const errors = asRecord(data?.errors);
  const text = errors?.text;
  if (Array.isArray(text) && typeof text[0] === 'string' && text[0].length > 0) return text[0];
  return null;
}

/** Названия туров тем же обогащением каталогом, что список заявок менеджера. */
export async function clientBookingTitles(
  bookings: ClientCardBooking[],
): Promise<Map<string, string>> {
  const byId = await catalogToursById(bookings.map((booking) => booking.tourId));
  return new Map(
    bookings.map((booking) => {
      const tour = byId.get(booking.tourId) ?? unavailableTour(booking.tourId || 'unknown');
      return [booking.id, bookingTourTitle({ tour })];
    }),
  );
}
