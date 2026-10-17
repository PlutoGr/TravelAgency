import type { Destination, TourFilters } from '@/types';

const SORTS = new Set<NonNullable<TourFilters['sortBy']>>([
  'popularity',
  'price_asc',
  'price_desc',
  'date',
  'rating',
  'title',
  'duration_asc',
  'duration_desc',
]);

export const CATALOG_QUERY_KEYS = [
  'country',
  'direction',
  'dateFrom',
  'dateTo',
  'priceMin',
  'priceMax',
  'category',
  'sort',
] as const;

function normalizePlace(value: string): string {
  return value.trim().toLocaleLowerCase('ru').replace(/ё/g, 'е').replace(/\s+/g, ' ');
}

function readDate(value: string | null | undefined): string | undefined {
  if (!value) return undefined;
  const trimmed = value.trim();
  return /^\d{4}-\d{2}-\d{2}$/.test(trimmed) ? trimmed : undefined;
}

export const DEPARTURE_RANGE_HINT = 'Дата «по» должна быть не раньше даты «с»';

export function formatCatalogDateChip(prefix: 'с' | 'по', isoDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
  if (!match) return `${prefix} ${isoDate}`;
  return `${prefix} ${match[3]}.${match[2]}.${match[1]}`;
}

export function isDepartureRangeInverted(dateFrom?: string, dateTo?: string): boolean {
  if (!dateFrom || !dateTo) return false;
  return dateFrom > dateTo;
}

function readPrice(value: string | null): number | undefined {
  if (value == null || value.trim() === '') return undefined;
  const price = Number(value);
  if (!Number.isFinite(price) || price < 0) return undefined;
  return Math.round(price);
}

function isSort(value: string): value is NonNullable<TourFilters['sortBy']> {
  return SORTS.has(value as NonNullable<TourFilters['sortBy']>);
}

export function filtersFromSearchParams(params: URLSearchParams): TourFilters {
  const filters: TourFilters = {};

  const country = params.get('country')?.trim();
  if (country) filters.country = [country];

  const directionId = params.get('direction')?.trim();
  if (directionId) filters.directionId = directionId;

  const dateFrom = readDate(params.get('dateFrom'));
  if (dateFrom) filters.dateFrom = dateFrom;

  const dateTo = readDate(params.get('dateTo'));
  if (dateTo) filters.dateTo = dateTo;

  const priceMin = readPrice(params.get('priceMin'));
  if (priceMin != null) filters.priceMin = priceMin;

  const priceMax = readPrice(params.get('priceMax'));
  if (priceMax != null) filters.priceMax = priceMax;

  const category = params.get('category')?.trim();
  if (category) filters.category = category;

  const sort = params.get('sort')?.trim() ?? '';
  if (isSort(sort)) filters.sortBy = sort;

  return filters;
}

export function filtersToSearchParams(filters: TourFilters): URLSearchParams {
  const params = new URLSearchParams();
  const country = filters.country?.map((item) => item.trim()).find(Boolean);
  if (country) params.set('country', country);
  if (filters.directionId?.trim()) params.set('direction', filters.directionId.trim());

  const dateFrom = readDate(filters.dateFrom);
  if (dateFrom) params.set('dateFrom', dateFrom);
  const dateTo = readDate(filters.dateTo);
  if (dateTo) params.set('dateTo', dateTo);

  if (filters.priceMin != null && Number.isFinite(filters.priceMin) && filters.priceMin >= 0) {
    params.set('priceMin', String(Math.round(filters.priceMin)));
  }
  if (filters.priceMax != null && Number.isFinite(filters.priceMax) && filters.priceMax >= 0) {
    params.set('priceMax', String(Math.round(filters.priceMax)));
  }
  if (filters.category?.trim()) params.set('category', filters.category.trim());
  if (filters.sortBy && filters.sortBy !== 'date') params.set('sort', filters.sortBy);

  return params;
}

/** Копирует адрес и подменяет только параметры каталога, остальные (например auth) оставляет. */
export function applyCatalogFilters(current: URLSearchParams, filters: TourFilters): URLSearchParams {
  const next = new URLSearchParams(current);
  for (const key of CATALOG_QUERY_KEYS) next.delete(key);
  filtersToSearchParams(filters).forEach((value, key) => {
    next.set(key, value);
  });
  return next;
}

export function catalogPath(filters: TourFilters): string {
  const search = filtersToSearchParams(filters).toString();
  return search ? `/tours?${search}` : '/tours';
}

export function hasCatalogQuery(filters: TourFilters): boolean {
  return Boolean(
    filters.country?.some((item) => item.trim()) ||
      filters.directionId?.trim() ||
      readDate(filters.dateFrom) ||
      readDate(filters.dateTo) ||
      (filters.priceMin != null && filters.priceMin > 0) ||
      (filters.priceMax != null && filters.priceMax >= 0) ||
      filters.category?.trim(),
  );
}

export function resolveHomeSearch(
  input: { destination: string; date: string },
  destinations: Destination[],
): TourFilters {
  const filters: TourFilters = {};
  const query = input.destination.trim();
  if (query) {
    const place = matchPlace(query, destinations);
    if (place?.directionId) filters.directionId = place.directionId;
    else if (place?.country) filters.country = [place.country];
    else filters.country = [query];
  }
  const dateFrom = readDate(input.date);
  if (dateFrom) filters.dateFrom = dateFrom;
  return filters;
}

function uniqueCountries(destinations: Destination[]): string[] {
  const seen = new Set<string>();
  const countries: string[] = [];
  for (const destination of destinations) {
    const country = destination.country?.trim();
    if (!country) continue;
    const key = normalizePlace(country);
    if (seen.has(key)) continue;
    seen.add(key);
    countries.push(country);
  }
  return countries;
}

function uniqueBy<T>(items: T[], predicate: (item: T) => boolean): T | undefined {
  let found: T | undefined;
  for (const item of items) {
    if (!predicate(item)) continue;
    if (found) return undefined;
    found = item;
  }
  return found;
}

function matchPlace(
  query: string,
  destinations: Destination[],
): { directionId?: string; country?: string } | undefined {
  const normalized = normalizePlace(query);
  if (!normalized) return undefined;

  const exactDirection = destinations.find(
    (item) => item.id && normalizePlace(item.name) === normalized,
  );
  if (exactDirection) return { directionId: exactDirection.id };

  const countries = uniqueCountries(destinations);
  const exactCountry = countries.find((country) => normalizePlace(country) === normalized);
  if (exactCountry) return { country: exactCountry };

  if (normalized.length < 2) return undefined;

  const directionPrefix = uniqueBy(
    destinations,
    (item) => Boolean(item.id) && normalizePlace(item.name).startsWith(normalized),
  );
  if (directionPrefix) return { directionId: directionPrefix.id };

  const countryPrefix = uniqueBy(countries, (country) => normalizePlace(country).startsWith(normalized));
  if (countryPrefix) return { country: countryPrefix };

  if (normalized.length < 3) return undefined;

  const directionContains = uniqueBy(destinations, (item) => {
    const name = normalizePlace(item.name);
    return Boolean(item.id) && (name.includes(normalized) || (name.length >= 3 && normalized.includes(name)));
  });
  if (directionContains) return { directionId: directionContains.id };

  const countryContains = uniqueBy(countries, (country) => {
    const name = normalizePlace(country);
    return name.includes(normalized) || (name.length >= 3 && normalized.includes(name));
  });
  if (countryContains) return { country: countryContains };

  return undefined;
}
