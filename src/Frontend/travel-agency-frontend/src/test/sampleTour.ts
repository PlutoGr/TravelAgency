import type { Tour } from '@/types';

export function sampleTour(overrides: Partial<Tour> = {}): Tour {
  return {
    id: 'tour-1',
    title: 'Мальдивы — рай на земле',
    description: 'Водные виллы над океаном.',
    shortDescription: 'Водные виллы и белый песок.',
    country: 'Мальдивы',
    city: '',
    hotel: '',
    price: 289000,
    priceFrom: 289000,
    currency: 'RUB',
    rating: 0,
    reviewCount: 0,
    dates: [{ start: '2026-11-01', end: '2026-11-08' }],
    duration: 7,
    photos: ['/api/v1/media/files/11111111-1111-1111-1111-111111111111/w800'],
    amenities: [],
    included: ['Проживание и питание по программе'],
    notIncluded: ['Личные расходы'],
    category: 'Beach',
    isHot: false,
    maxTravelers: 8,
    available: true,
    departureCity: 'Москва',
    mealPlan: 'Всё включено',
    accommodation: 'Водная вилла, всё включено',
    nearestDate: '2026-11-01T00:00:00Z',
    images: [
      {
        mediaFileId: '11111111-1111-1111-1111-111111111111',
        alt: 'Вилла',
        isCover: true,
        sortOrder: 0,
      },
    ],
    days: [{ dayNumber: 1, title: 'Прилёт', description: 'Встреча и трансфер на атолл.' }],
    offers: [
      {
        id: 'offer-1',
        start: '2026-11-01T00:00:00Z',
        end: '2026-11-08T00:00:00Z',
        price: 289000,
        currency: 'RUB',
        seats: 8,
      },
    ],
    ...overrides,
  };
}
