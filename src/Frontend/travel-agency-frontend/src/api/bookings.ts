import type { Booking, BookingStatus, Tour } from '@/types';
import { apiClient } from './client';

/** Backend BookingStatus enum: New=0, InProgress=1, ProposalSent=2, Confirmed=3, Closed=4, Cancelled=5 */
type BackendBookingStatus = 0 | 1 | 2 | 3 | 4 | 5;

/**
 * Tour snapshot DTO from backend (TourSnapshot in Booking domain).
 * Note: imageUrl is optional and not populated by backend; reserved for future use.
 */
interface TourSnapshotDto {
  tourId: string;
  title: string;
  description: string;
  price: number;
  currency: string;
  durationDays: number;
  snapshotTakenAt: string;
  /** Optional; not populated by backend, reserved for future use */
  imageUrl?: string | null;
}

interface ProposalDto {
  id: string;
  bookingId: string;
  managerId: string;
  tourSnapshot: TourSnapshotDto;
  notes?: string;
  isConfirmed: boolean;
  createdAt: string;
}

interface BookingDto {
  id: string;
  clientId: string;
  tourId: string;
  comment?: string;
  status: BackendBookingStatus;
  createdAt: string;
  updatedAt: string;
  proposals?: ProposalDto[];
  clientName?: string | null;
  clientEmail?: string | null;
  clientPhone?: string | null;
}

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

function mapStatus(backend: BackendBookingStatus): BookingStatus {
  return BACKEND_TO_FRONTEND_STATUS[backend] ?? 'new';
}

function mapTourSnapshotToTour(snapshot: TourSnapshotDto, tourId: string): Tour {
  const desc = snapshot.description ?? '';
  return {
    id: tourId,
    title: snapshot.title,
    description: desc,
    shortDescription: desc.slice(0, 150) + (desc.length > 150 ? '...' : ''),
    country: '',
    city: '',
    hotel: '',
    price: snapshot.price,
    rating: 0,
    reviewCount: 0,
    dates: [],
    duration: snapshot.durationDays,
    photos: snapshot.imageUrl ? [snapshot.imageUrl] : [],
    amenities: [],
    included: [],
    notIncluded: [],
    category: '',
    isHot: false,
    maxTravelers: 0,
  };
}

function mapDtoToBooking(dto: BookingDto): Booking {
  const proposal = dto.proposals?.[0];
  const snapshot = proposal?.tourSnapshot;
  const destination = snapshot?.title ?? '';
  const country = '';
  const budget = snapshot?.price ?? 0;
  const tourId = dto.tourId ?? snapshot?.tourId ?? '';
  const tour: Tour | undefined =
    snapshot && tourId
      ? mapTourSnapshotToTour(snapshot, tourId)
      : undefined;

  return {
    id: dto.id,
    clientId: dto.clientId,
    clientName: dto.clientName ?? '',
    clientEmail: dto.clientEmail ?? null,
    clientPhone: dto.clientPhone ?? null,
    destination,
    country,
    dateFrom: '',
    dateTo: '',
    travelers: 1,
    budget,
    status: mapStatus(dto.status),
    managerId: proposal?.managerId,
    managerName: '',
    tourId: tourId || undefined,
    tour,
    proposalId: proposal?.id,
    notes: dto.comment ?? '',
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  };
}

export interface CreateBookingParams {
  tourId: string;
  comment?: string;
}

export async function createBooking(params: CreateBookingParams): Promise<Booking> {
  const { data } = await apiClient.post<BookingDto>('/bookings', {
    tourId: params.tourId,
    comment: params.comment,
  });
  return mapDtoToBooking(data);
}

export async function getMyBookings(status?: BookingStatus): Promise<Booking[]> {
  const { data } = await apiClient.get<BookingDto[]>('/bookings/my');
  const bookings = data.map(mapDtoToBooking);
  if (status) {
    return bookings.filter((b) => b.status === status);
  }
  return bookings;
}

export async function getBookingById(id: string): Promise<Booking> {
  const { data } = await apiClient.get<BookingDto>(`/bookings/${id}`);
  return mapDtoToBooking(data);
}

export async function updateBookingStatus(
  id: string,
  status: BookingStatus,
): Promise<Booking> {
  const backendStatus = FRONTEND_TO_BACKEND_STATUS[status] ?? 0;
  const { data } = await apiClient.patch<BookingDto>(
    `/bookings/${id}/status`,
    { newStatus: backendStatus },
  );
  return mapDtoToBooking(data);
}

export async function confirmProposal(
  bookingId: string,
  proposalId: string,
): Promise<Booking> {
  const { data } = await apiClient.post<BookingDto>(
    `/bookings/${bookingId}/confirm`,
    { proposalId },
  );
  return mapDtoToBooking(data);
}

export interface CreateProposalParams {
  notes?: string;
}

export interface ProposalResponse {
  id: string;
  bookingId: string;
  managerId: string;
  tourSnapshot: TourSnapshotDto;
  notes?: string;
  isConfirmed: boolean;
  createdAt: string;
}

export async function createProposal(
  bookingId: string,
  params: CreateProposalParams = {},
): Promise<ProposalResponse> {
  const { data } = await apiClient.post<ProposalResponse>(
    `/bookings/${bookingId}/proposal`,
    { notes: params.notes },
  );
  return data;
}

/** Manager-only: returns all bookings. Requires manager role. */
export async function getAllBookings(
  filters?: {
    status?: BookingStatus;
    search?: string;
  },
): Promise<Booking[]> {
  const { data } = await apiClient.get<BookingDto[]>('/bookings');
  const items = Array.isArray(data) ? data : [];
  let bookings = items.map(mapDtoToBooking);
  if (filters?.status) {
    bookings = bookings.filter((b) => b.status === filters.status);
  }
  return bookings;
}
