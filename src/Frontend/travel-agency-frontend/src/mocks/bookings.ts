import type { Booking, BookingStatus } from '@/types';
import { mockTours } from './tours';

function mockBooking(
  id: string,
  tourIndex: number,
  status: BookingStatus,
  comment: string,
  createdAt: string,
  client: { id: string; name: string },
): Booking {
  const tour = mockTours[tourIndex];
  return {
    id,
    clientId: client.id,
    tourId: tour?.id ?? `tour-${tourIndex + 1}`,
    comment,
    status,
    createdAt,
    updatedAt: createdAt,
    proposals: [],
    clientName: client.name,
    clientEmail: null,
    clientPhone: null,
    tour,
  };
}

const ivan = { id: 'user-1', name: 'Иван Иванов' };

export const mockBookings: Booking[] = [
  mockBooking(
    'booking-1',
    0,
    'confirmed',
    'Предпочитаем номер с видом на море. Годовщина свадьбы — возможен ли декор?',
    '2026-02-10T14:22:00Z',
    ivan,
  ),
  mockBooking(
    'booking-2',
    11,
    'proposal_sent',
    'Едем с друзьями, интересует винный тур и национальная кухня.',
    '2026-02-28T09:45:00Z',
    ivan,
  ),
  mockBooking(
    'booking-3',
    0,
    'new',
    'Хотим люкс-отель с видом на Бурдж-Халифа. Интересует сафари в пустыне.',
    '2026-03-10T18:00:00Z',
    ivan,
  ),
  mockBooking(
    'booking-4',
    5,
    'in_progress',
    'Медовый месяц. Хотим люкс с джакузи и видом на кальдеру.',
    '2026-02-20T12:10:00Z',
    { id: 'user-2', name: 'Мария Лебедева' },
  ),
  mockBooking(
    'booking-5',
    2,
    'confirmed',
    'Соло-путешествие. Интересуют дайвинг и экскурсии на острова.',
    '2026-01-15T10:30:00Z',
    { id: 'user-3', name: 'Алексей Новиков' },
  ),
  mockBooking(
    'booking-6',
    3,
    'closed',
    'Водная вилла обязательно. Годовщина 10 лет.',
    '2025-12-01T08:15:00Z',
    { id: 'user-4', name: 'Елена Смирнова' },
  ),
  mockBooking(
    'booking-7',
    6,
    'proposal_sent',
    'С ребёнком 8 лет. Нужен семейный номер и детское меню.',
    '2026-02-25T15:40:00Z',
    { id: 'user-5', name: 'Дмитрий Козлов' },
  ),
  mockBooking(
    'booking-8',
    1,
    'in_progress',
    'Хотим отель с хорошим домашним рифом для снорклинга.',
    '2026-03-02T13:00:00Z',
    { id: 'user-6', name: 'Наталья Соколова' },
  ),
];
