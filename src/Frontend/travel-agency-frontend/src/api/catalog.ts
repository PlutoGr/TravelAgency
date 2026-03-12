import type { Destination, PaginatedResponse, Tour, TourFilters } from '@/types';
import { mockTours } from '@/mocks/tours';
import { mockDestinations } from '@/mocks/destinations';

const delay = () => new Promise((r) => setTimeout(r, 300 + Math.random() * 200));
const PAGE_SIZE = 6;

function applyFilters(tours: Tour[], filters?: TourFilters): Tour[] {
  if (!filters) return tours;

  let result = [...tours];

  if (filters.search) {
    const q = filters.search.toLowerCase();
    result = result.filter(
      (t) =>
        t.title.toLowerCase().includes(q) ||
        t.country.toLowerCase().includes(q) ||
        t.city.toLowerCase().includes(q) ||
        t.description.toLowerCase().includes(q),
    );
  }

  if (filters.country?.length) {
    result = result.filter((t) => filters.country!.includes(t.country));
  }

  if (filters.priceMin != null) {
    result = result.filter((t) => t.price >= filters.priceMin!);
  }

  if (filters.priceMax != null) {
    result = result.filter((t) => t.price <= filters.priceMax!);
  }

  if (filters.rating != null) {
    result = result.filter((t) => t.rating >= filters.rating!);
  }

  if (filters.category) {
    result = result.filter((t) => t.category === filters.category);
  }

  if (filters.amenities?.length) {
    result = result.filter((t) =>
      filters.amenities!.every((a) => t.amenities.includes(a)),
    );
  }

  switch (filters.sortBy) {
    case 'price_asc':
      result.sort((a, b) => a.price - b.price);
      break;
    case 'price_desc':
      result.sort((a, b) => b.price - a.price);
      break;
    case 'rating':
      result.sort((a, b) => b.rating - a.rating);
      break;
    case 'date':
      result.sort(
        (a, b) =>
          new Date(a.dates[0]?.start ?? '').getTime() -
          new Date(b.dates[0]?.start ?? '').getTime(),
      );
      break;
    case 'popularity':
    default:
      result.sort((a, b) => b.reviewCount - a.reviewCount);
      break;
  }

  return result;
}

export async function getTours(
  filters?: TourFilters,
  page = 1,
): Promise<PaginatedResponse<Tour>> {
  await delay();

  const filtered = applyFilters(mockTours, filters);
  const total = filtered.length;
  const totalPages = Math.ceil(total / PAGE_SIZE);
  const start = (page - 1) * PAGE_SIZE;
  const items = filtered.slice(start, start + PAGE_SIZE);

  return { items, total, page, pageSize: PAGE_SIZE, totalPages };
}

export async function getTourById(id: string): Promise<Tour> {
  await delay();

  const tour = mockTours.find((t) => t.id === id);
  if (!tour) throw new Error(`Тур с ID "${id}" не найден`);
  return tour;
}

export async function getDestinations(): Promise<Destination[]> {
  await delay();
  return mockDestinations;
}

export async function searchTours(query: string): Promise<Tour[]> {
  await delay();

  const q = query.toLowerCase();
  return mockTours.filter(
    (t) =>
      t.title.toLowerCase().includes(q) ||
      t.country.toLowerCase().includes(q) ||
      t.city.toLowerCase().includes(q),
  );
}
