import type { Destination, PaginatedResponse, Tour, TourFilters } from '@/types';
import { apiClient } from './client';
import toast from 'react-hot-toast';

const PAGE_SIZE = 6;
const MANAGER_PAGE_SIZE = 100;

/** Backend TourType enum values */
const TOUR_TYPE_VALUES = [
  'Beach',
  'Mountain',
  'City',
  'Cultural',
  'Adventure',
  'Cruise',
  'Safari',
] as const;

/** Maps frontend category label to backend TourType */
const CATEGORY_TO_TOUR_TYPE: Record<string, (typeof TOUR_TYPE_VALUES)[number]> = {
  'Пляжный отдых': 'Beach',
  Beach: 'Beach',
  Экзотика: 'Safari',
  Safari: 'Safari',
  Люкс: 'Cultural',
  Экскурсионный: 'Cultural',
  Романтический: 'Cultural',
  Гастрономический: 'Cultural',
  Cultural: 'Cultural',
  Mountain: 'Mountain',
  City: 'City',
  Adventure: 'Adventure',
  Cruise: 'Cruise',
};

function categoryToTourType(category: string): (typeof TOUR_TYPE_VALUES)[number] {
  return CATEGORY_TO_TOUR_TYPE[category] ?? 'Cultural';
}

/** Backend API response types (camelCase from ASP.NET Core JSON) */
interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/** Maps numeric TourType (0-6) from backend to string label */
const TOUR_TYPE_LABELS: Record<number, string> = {
  0: 'Beach',
  1: 'Mountain',
  2: 'City',
  3: 'Cultural',
  4: 'Adventure',
  5: 'Cruise',
  6: 'Safari',
};

function mapTourTypeToCategory(tourType: string | number): string {
  if (typeof tourType === 'number' && tourType in TOUR_TYPE_LABELS) {
    return TOUR_TYPE_LABELS[tourType];
  }
  return String(tourType ?? '');
}

/** Shared params for building a Tour from DTO fields (used by both mappers) */
interface BuildTourParams {
  id: string;
  title: string;
  country: string;
  tourType: string | number;
  durationDays: number;
  imageUrl: string | null;
  description?: string;
  shortDescription?: string;
  price?: number;
  dates?: { start: string; end: string }[];
  maxTravelers?: number;
}

/** Builds a Tour from common DTO fields; varying parts passed as overrides. Exported for testing. */
export function buildTourFromDtoFields(params: BuildTourParams): Tour {
  return {
    id: params.id,
    title: params.title,
    description: params.description ?? '',
    shortDescription: params.shortDescription ?? '',
    country: params.country,
    city: '',
    hotel: '',
    price: params.price ?? 0,
    rating: 0,
    reviewCount: 0,
    dates: params.dates ?? [],
    duration: params.durationDays,
    photos: params.imageUrl ? [params.imageUrl] : [],
    amenities: [],
    included: [],
    notIncluded: [],
    category: mapTourTypeToCategory(params.tourType),
    isHot: false,
    maxTravelers: params.maxTravelers ?? 0,
  };
}

interface TourSummaryDto {
  id: string;
  title: string;
  country: string;
  tourType: string | number;
  durationDays: number;
  imageUrl: string | null;
  minPrice: number | null;
  currency: string | null;
  isActive: boolean;
}

interface TourPriceDto {
  id: string;
  validFrom: string;
  validTo: string;
  pricePerPerson: number;
  currency: string;
  availableSeats: number;
}

interface TourDto {
  id: string;
  title: string;
  description?: string | null;
  country: string;
  tourType: string | number;
  durationDays: number;
  imageUrl: string | null;
  directionId: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  prices: TourPriceDto[];
}

function mapTourSummaryToTour(dto: TourSummaryDto): Tour {
  return buildTourFromDtoFields({
    id: dto.id,
    title: dto.title,
    country: dto.country,
    tourType: dto.tourType,
    durationDays: dto.durationDays,
    imageUrl: dto.imageUrl,
    price: dto.minPrice ?? 0,
  });
}

function mapTourDtoToTour(dto: TourDto): Tour {
  const desc = dto.description ?? '';
  const firstPrice = dto.prices?.[0];
  const dates =
    dto.prices?.map((p) => ({
      start: p.validFrom,
      end: p.validTo,
    })) ?? [];

  return buildTourFromDtoFields({
    id: dto.id,
    title: dto.title,
    country: dto.country,
    tourType: dto.tourType,
    durationDays: dto.durationDays,
    imageUrl: dto.imageUrl,
    description: desc,
    shortDescription: desc.slice(0, 150) + (desc.length > 150 ? '...' : ''),
    price: firstPrice?.pricePerPerson ?? 0,
    dates,
    maxTravelers: firstPrice?.availableSeats ?? 0,
  });
}

function buildToursQueryParams(
  filters?: TourFilters,
  page = 1,
  pageSize?: number,
): Record<string, string | number | boolean | undefined> {
  const params: Record<string, string | number | boolean | undefined> = {
    Page: page,
    PageSize: pageSize ?? PAGE_SIZE,
  };

  if (!filters) return params;

  if (filters.country?.length) {
    params.Country = filters.country[0];
  }
  if (filters.priceMin != null) {
    params.MinPrice = filters.priceMin;
  }
  if (filters.priceMax != null) {
    params.MaxPrice = filters.priceMax;
  }
  if (filters.dateFrom) {
    params.DateFrom = filters.dateFrom;
  }
  if (filters.dateTo) {
    params.DateTo = filters.dateTo;
  }
  if (filters.category) {
    params.TourType = filters.category;
  }
  if (filters.sortBy) {
    switch (filters.sortBy) {
      case 'price_asc':
        params.SortBy = 'Price';
        params.SortDirection = 'Asc';
        break;
      case 'price_desc':
        params.SortBy = 'Price';
        params.SortDirection = 'Desc';
        break;
      case 'date':
        params.SortBy = 'CreatedAt';
        params.SortDirection = 'Desc';
        break;
      case 'rating':
      case 'popularity':
      default:
        params.SortBy = 'CreatedAt';
        params.SortDirection = 'Desc';
        break;
    }
  }

  return params;
}

export async function getTours(
  filters?: TourFilters,
  page = 1,
  pageSize?: number,
): Promise<PaginatedResponse<Tour>> {
  const { data } = await apiClient.get<PagedResultDto<TourSummaryDto>>('/catalog/tours', {
    params: buildToursQueryParams(filters, page, pageSize),
  });

  const items = (data.items ?? []).map(mapTourSummaryToTour);
  const totalPages = data.pageSize > 0 ? Math.ceil(data.totalCount / data.pageSize) : 0;

  return {
    items,
    total: data.totalCount,
    page: data.page,
    pageSize: data.pageSize,
    totalPages,
  };
}

/** Manager: fetch all tours for listing (uses larger page size) */
export async function getToursForManager(
  filters?: TourFilters,
  page = 1,
): Promise<PaginatedResponse<Tour>> {
  return getTours(filters, page, MANAGER_PAGE_SIZE);
}

/** Create tour request payload (backend CreateTourRequest) */
export interface CreateTourRequest {
  title: string;
  description: string;
  tourType: string;
  country: string;
  durationDays: number;
  imageUrl: string | null;
  directionId: string | null;
}

/** Update tour request payload (backend UpdateTourRequest) */
export interface UpdateTourRequest {
  title: string;
  description: string;
  tourType: string;
  country: string;
  durationDays: number;
  imageUrl: string | null;
  directionId: string | null;
}

/** Tour price request (backend TourPriceRequest) */
export interface TourPriceRequest {
  validFrom: string;
  validTo: string;
  pricePerPerson: number;
  currency: string;
  availableSeats: number;
}

export async function createTour(
  request: CreateTourRequest,
): Promise<Tour> {
  const { data } = await apiClient.post<TourDto>('/catalog/tours', {
    title: request.title,
    description: request.description,
    tourType: categoryToTourType(request.tourType),
    country: request.country,
    durationDays: request.durationDays,
    imageUrl: request.imageUrl,
    directionId: request.directionId,
  });
  return mapTourDtoToTour(data);
}

export async function updateTour(
  id: string,
  request: UpdateTourRequest,
): Promise<Tour> {
  const { data } = await apiClient.put<TourDto>(`/catalog/tours/${id}`, {
    title: request.title,
    description: request.description,
    tourType: categoryToTourType(request.tourType),
    country: request.country,
    durationDays: request.durationDays,
    imageUrl: request.imageUrl,
    directionId: request.directionId,
  });
  return mapTourDtoToTour(data);
}

export async function updateTourPrices(
  id: string,
  prices: TourPriceRequest[],
): Promise<Tour> {
  const { data } = await apiClient.patch<TourDto>(`/catalog/tours/${id}/prices`, {
    prices: prices.map((p) => ({
      validFrom: p.validFrom,
      validTo: p.validTo,
      pricePerPerson: p.pricePerPerson,
      currency: p.currency,
      availableSeats: p.availableSeats,
    })),
  });
  return mapTourDtoToTour(data);
}

export async function deleteTour(id: string): Promise<void> {
  await apiClient.delete(`/catalog/tours/${id}`);
}

export async function getTourById(id: string): Promise<Tour> {
  try {
    const { data } = await apiClient.get<TourDto>(`/catalog/tours/${id}`);
    return mapTourDtoToTour(data);
  } catch (err: unknown) {
    if (typeof err === 'object' && err !== null && 'response' in err) {
      const axiosErr = err as { response?: { status?: number } };
      if (axiosErr.response?.status === 404) {
        throw new Error(`Тур с ID "${id}" не найден`);
      }
    }
    throw err;
  }
}

/** Backend DirectionDto (GET /catalog/directions) */
interface DirectionDto {
  id: string;
  name: string;
  country: string;
  description?: string | null;
}

function mapDirectionDtoToDestination(dto: DirectionDto): Destination {
  return {
    id: dto.id,
    name: dto.name,
    country: dto.country,
    photo: '',
    tourCount: 0,
    description: dto.description ?? '',
  };
}

/**
 * Fetches destinations from GET /catalog/directions.
 * Maps DirectionDto (id, name, country, description) to Destination.
 * photo and tourCount use defaults (empty string, 0) as backend does not provide them.
 */
export async function getDestinations(): Promise<Destination[]> {
  try {
    const { data } = await apiClient.get<DirectionDto[] | null | undefined>(
      '/catalog/directions',
    );
    const items = Array.isArray(data) ? data : [];
    return items.map(mapDirectionDtoToDestination);
  } catch (err: unknown) {
    if (typeof err === 'object' && err !== null && 'response' in err) {
      const axiosErr = err as { response?: { status?: number }; message?: string };
      if (axiosErr.response?.status === 404 || axiosErr.response?.status === 500) {
        console.error('[getDestinations]', axiosErr.response?.status, axiosErr);
        toast.error('Не удалось загрузить направления');
        return [];
      }
    }
    throw err;
  }
}

/**
 * Backend has no equivalent endpoint. Returns empty array until search API is available.
 * For now, use getTours with filters (e.g. country) for catalog browsing.
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars -- stub; query reserved for future search API
export async function searchTours(_query: string): Promise<Tour[]> {
  return [];
}
