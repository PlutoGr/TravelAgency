import type { Booking, BookingStatus } from '@/types';
import { mockBookings } from '@/mocks/bookings';

const delay = () => new Promise((r) => setTimeout(r, 300 + Math.random() * 200));

let bookingsState = [...mockBookings];

export async function createBooking(data: Partial<Booking>): Promise<Booking> {
  await delay();

  const newBooking: Booking = {
    id: 'booking-' + (bookingsState.length + 1),
    clientId: data.clientId ?? 'user-1',
    clientName: data.clientName ?? 'Иван Иванов',
    destination: data.destination ?? '',
    country: data.country ?? '',
    dateFrom: data.dateFrom ?? '',
    dateTo: data.dateTo ?? '',
    travelers: data.travelers ?? 1,
    budget: data.budget ?? 0,
    status: 'new',
    notes: data.notes ?? '',
    tourId: data.tourId,
    tour: data.tour,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  bookingsState = [newBooking, ...bookingsState];
  return newBooking;
}

export async function getMyBookings(status?: BookingStatus): Promise<Booking[]> {
  await delay();

  const userBookings = bookingsState.filter((b) => b.clientId === 'user-1');
  if (!status) return userBookings;
  return userBookings.filter((b) => b.status === status);
}

export async function getBookingById(id: string): Promise<Booking> {
  await delay();

  const booking = bookingsState.find((b) => b.id === id);
  if (!booking) throw new Error(`Заявка с ID "${id}" не найдена`);
  return booking;
}

export async function getAllBookings(filters?: {
  status?: BookingStatus;
  search?: string;
}): Promise<Booking[]> {
  await delay();

  let result = [...bookingsState];

  if (filters?.status) {
    result = result.filter((b) => b.status === filters.status);
  }

  if (filters?.search) {
    const q = filters.search.toLowerCase();
    result = result.filter(
      (b) =>
        b.clientName.toLowerCase().includes(q) ||
        b.destination.toLowerCase().includes(q) ||
        b.country.toLowerCase().includes(q),
    );
  }

  return result;
}

export async function updateBookingStatus(
  id: string,
  status: BookingStatus,
): Promise<Booking> {
  await delay();

  const index = bookingsState.findIndex((b) => b.id === id);
  if (index === -1) throw new Error(`Заявка с ID "${id}" не найдена`);

  bookingsState[index] = {
    ...bookingsState[index],
    status,
    updatedAt: new Date().toISOString(),
  };

  return bookingsState[index];
}
