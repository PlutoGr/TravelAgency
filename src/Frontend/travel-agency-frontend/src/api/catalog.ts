import type {
  Destination,
  PaginatedResponse,
  Tour,
  TourDay,
  TourFilters,
  TourImage,
  TourOffer,
} from '@/types';
import { mediaFileUrl, mediaIdFromUrl, isSafePhotoUrl } from '@/utils/media';
import { mealPlanLabel } from '@/utils/tourLabels';
import { apiClient } from './client';
import toast from 'react-hot-toast';

const PAGE_SIZE = 6;
const MANAGER_PAGE_SIZE = 100;

/** Maps frontend category label to backend TourType */
const CATEGORY_TO_TOUR_TYPE: Record<string, string> = {
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
  available?: boolean;
  departureCity?: string;
  mealPlan?: string | null;
  accommodation?: string;
  currency?: string | null;
  nearestDate?: string | null;
  priceFrom?: number | null;
  images?: TourImage[];
  days?: TourDay[];
  offers?: TourOffer[];
  included?: string[];
  notIncluded?: string[];
}

/** Builds a Tour from common DTO fields; varying parts passed as overrides. Exported for testing. */
export function buildTourFromDtoFields(params: BuildTourParams): Tour {
  const images = params.images ?? [];
  const photosFromMedia = images.map((image) => mediaFileUrl(image.mediaFileId, 'w800'));
  const fallbackPhoto = isSafePhotoUrl(params.imageUrl) ? [params.imageUrl] : [];

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
    photos: photosFromMedia.length > 0 ? photosFromMedia : fallbackPhoto,
    amenities: [],
    included: params.included ?? [],
    notIncluded: params.notIncluded ?? [],
    category: mapTourTypeToCategory(params.tourType),
    isHot: false,
    maxTravelers: params.maxTravelers ?? 0,
    available: params.available ?? true,
    departureCity: params.departureCity ?? '',
    mealPlan: params.mealPlan ?? null,
    accommodation: params.accommodation ?? '',
    currency: params.currency ?? null,
    nearestDate: params.nearestDate ?? null,
    priceFrom: params.priceFrom ?? null,
    images,
    days: params.days ?? [],
    offers: params.offers ?? [],
  };
}

interface PublicPreviewDto {
  mediaFileId?: string;
  url?: string | null;
  alt?: string | null;
  isCover?: boolean;
  sortOrder?: number;
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
  priceFrom?: number | null;
  nearestDate?: string | null;
  shortDescription?: string | null;
  departureCity?: string | null;
  previews?: PublicPreviewDto[] | null;
}

interface PublicTourCardDto {
  id: string;
  title: string;
  cover?: PublicPreviewDto | null;
  available: boolean;
  priceFrom?: number | null;
  currency?: string | null;
  nearestDate?: string | null;
  shortDescription?: string | null;
  country?: string | null;
  durationDays?: number | null;
}

interface TourPriceDto {
  id: string;
  validFrom: string;
  validTo: string;
  pricePerPerson: number;
  currency: string;
  availableSeats: number;
}

interface PublicDayDto {
  dayNumber: number;
  title: string;
  description: string;
}

interface PublicInclusionDto {
  text?: string | null;
  kind?: string | number;
  sortOrder?: number;
}

interface TourDto {
  id: string;
  title: string;
  description?: string | null;
  shortDescription?: string | null;
  departureCity?: string | null;
  country: string;
  tourType: string | number;
  durationDays: number;
  imageUrl: string | null;
  directionId: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  mealPlan?: string | number | null;
  accommodationText?: string | null;
  priceFrom?: number | null;
  currency?: string | null;
  nearestDate?: string | null;
  prices?: TourPriceDto[] | null;
  days?: PublicDayDto[] | null;
  inclusions?: PublicInclusionDto[] | null;
  images?: PublicPreviewDto[] | null;
}

function mapImages(previews?: PublicPreviewDto[] | null): TourImage[] {
  const images: TourImage[] = [];
  for (const preview of previews ?? []) {
    const mediaFileId = preview.mediaFileId || mediaIdFromUrl(preview.url);
    if (!mediaFileId) continue;
    images.push({
      mediaFileId,
      alt: preview.alt ?? null,
      isCover: Boolean(preview.isCover),
      sortOrder: preview.sortOrder ?? 0,
    });
  }
  return images;
}

function mapOffers(prices?: TourPriceDto[] | null): TourOffer[] {
  return (prices ?? []).map((price) => ({
    id: price.id,
    start: price.validFrom,
    end: price.validTo,
    price: price.pricePerPerson,
    currency: price.currency,
    seats: price.availableSeats,
  }));
}

function mapDays(days?: PublicDayDto[] | null): TourDay[] {
  return [...(days ?? [])].sort((a, b) => a.dayNumber - b.dayNumber);
}

function splitInclusions(items?: PublicInclusionDto[] | null): {
  included: string[];
  notIncluded: string[];
} {
  const included: string[] = [];
  const notIncluded: string[] = [];
  const ordered = [...(items ?? [])].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
  for (const item of ordered) {
    const text = item.text?.trim();
    if (!text) continue;
    const includedKind = item.kind === 0 || item.kind === '0' || item.kind === 'Included';
    if (includedKind) included.push(text);
    else notIncluded.push(text);
  }
  return { included, notIncluded };
}

function toShortDescription(description: string, shortDescription?: string | null): string {
  if (shortDescription) return shortDescription;
  if (description.length <= 150) return description;
  return `${description.slice(0, 150)}...`;
}

function mapTourSummaryToTour(dto: TourSummaryDto): Tour {
  const images = mapImages(dto.previews);
  const priceFrom = dto.priceFrom ?? dto.minPrice ?? null;
  return buildTourFromDtoFields({
    id: dto.id,
    title: dto.title,
    country: dto.country,
    tourType: dto.tourType,
    durationDays: dto.durationDays,
    imageUrl: images.length > 0 ? null : dto.imageUrl,
    shortDescription: dto.shortDescription ?? '',
    price: priceFrom ?? 0,
    priceFrom,
    currency: dto.currency,
    departureCity: dto.departureCity ?? '',
    nearestDate: dto.nearestDate ?? null,
    images,
    available: dto.isActive !== false,
  });
}

export function mapPublicCardToTour(dto: PublicTourCardDto): Tour {
  const images = dto.cover ? mapImages([dto.cover]) : [];
  return buildTourFromDtoFields({
    id: dto.id,
    title: dto.title,
    country: dto.country ?? '',
    tourType: '',
    durationDays: dto.durationDays ?? 0,
    imageUrl: null,
    shortDescription: dto.shortDescription ?? '',
    price: dto.priceFrom ?? 0,
    priceFrom: dto.priceFrom ?? null,
    currency: dto.currency ?? null,
    nearestDate: dto.nearestDate ?? null,
    images,
    available: dto.available,
  });
}

/** Карточка для id, которого нет в публичной выдаче (черновик или удалённый тур). */
export function unavailableTour(id: string): Tour {
  return buildTourFromDtoFields({
    id,
    title: 'Тур недоступен',
    country: '',
    tourType: '',
    durationDays: 0,
    imageUrl: null,
    available: false,
  });
}

function mapTourDtoToTour(dto: TourDto): Tour {
  const desc = dto.description ?? '';
  const images = mapImages(dto.images);
  const offers = mapOffers(dto.prices);
  const dates = offers.map((offer) => ({ start: offer.start, end: offer.end }));
  const { included, notIncluded } = splitInclusions(dto.inclusions);
  const priceFrom = dto.priceFrom ?? offers[0]?.price ?? null;

  return buildTourFromDtoFields({
    id: dto.id,
    title: dto.title,
    country: dto.country,
    tourType: dto.tourType,
    durationDays: dto.durationDays,
    imageUrl: images.length > 0 ? null : dto.imageUrl,
    description: desc,
    shortDescription: toShortDescription(desc, dto.shortDescription),
    price: priceFrom ?? 0,
    priceFrom,
    currency: dto.currency ?? offers[0]?.currency ?? null,
    dates,
    offers,
    maxTravelers: offers[0]?.seats ?? 0,
    departureCity: dto.departureCity ?? '',
    mealPlan: mealPlanLabel(dto.mealPlan),
    accommodation: dto.accommodationText ?? '',
    nearestDate: dto.nearestDate ?? null,
    images,
    days: mapDays(dto.days),
    included,
    notIncluded,
    available: dto.isActive !== false,
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
    params.TourType = CATEGORY_TO_TOUR_TYPE[filters.category] ?? filters.category;
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
      case 'title':
        params.SortBy = 'Title';
        params.SortDirection = 'Asc';
        break;
      case 'duration_asc':
        params.SortBy = 'DurationDays';
        params.SortDirection = 'Asc';
        break;
      case 'duration_desc':
        params.SortBy = 'DurationDays';
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

  if (filters.directionId) {
    params.DirectionId = filters.directionId;
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

const CARD_BATCH = 50;

/** Карточки избранного: опубликованный тур целиком, снятый — с available: false. */
export async function getTourCards(ids: string[]): Promise<Tour[]> {
  const unique = [...new Set(ids.filter(Boolean))];
  const tours: Tour[] = [];

  for (let offset = 0; offset < unique.length; offset += CARD_BATCH) {
    const chunk = unique.slice(offset, offset + CARD_BATCH);
    const query = chunk.map((id) => `ids=${encodeURIComponent(id)}`).join('&');
    const { data } = await apiClient.get<PublicTourCardDto[]>(`/catalog/tours/cards?${query}`);
    tours.push(...(data ?? []).map(mapPublicCardToTour));
  }

  return tours;
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
