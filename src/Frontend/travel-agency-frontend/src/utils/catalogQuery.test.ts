import { describe, expect, it } from 'vitest';
import type { Destination } from '@/types';
import {
  applyCatalogFilters,
  catalogPath,
  filtersFromSearchParams,
  filtersToSearchParams,
  formatCatalogDateChip,
  DEPARTURE_YEAR_HINT,
  departureDateIssue,
  isDepartureDateOutOfRange,
  isDepartureRangeInverted,
  resolveHomeSearch,
} from './catalogQuery';

const destinations: Destination[] = [
  {
    id: 'dir-bali',
    name: 'Бали',
    country: 'Индонезия',
    photo: '',
    tourCount: 3,
    description: '',
  },
  {
    id: 'dir-rome',
    name: 'Рим',
    country: 'Италия',
    photo: '',
    tourCount: 1,
    description: '',
  },
];

describe('catalogQuery', () => {
  it('roundtrips catalog filters and omits the default sort', () => {
    const params = filtersToSearchParams({
      country: ['Италия'],
      directionId: 'dir-rome',
      dateFrom: '2026-11-01',
      dateTo: '2026-11-20',
      priceMin: 10000,
      priceMax: 80000,
      category: 'Beach',
      sortBy: 'date',
    });

    expect(params.toString()).not.toContain('sort=');
    expect(filtersFromSearchParams(params)).toEqual({
      country: ['Италия'],
      directionId: 'dir-rome',
      dateFrom: '2026-11-01',
      dateTo: '2026-11-20',
      priceMin: 10000,
      priceMax: 80000,
      category: 'Beach',
    });
  });

  it('keeps an explicit non-default sort and drops unknown values', () => {
    const params = new URLSearchParams('sort=price_asc&dateFrom=завтра&priceMin=-5&category=City');
    expect(filtersFromSearchParams(params)).toEqual({
      sortBy: 'price_asc',
      category: 'City',
    });

    const written = filtersToSearchParams({ sortBy: 'price_desc' });
    expect(written.get('sort')).toBe('price_desc');
  });

  it('replaces catalog params and leaves unrelated ones', () => {
    const current = new URLSearchParams('auth=login&country=Италия&sort=title');
    const next = applyCatalogFilters(current, { directionId: 'dir-bali', dateFrom: '2026-07-01' });

    expect(next.get('auth')).toBe('login');
    expect(next.get('country')).toBeNull();
    expect(next.get('sort')).toBeNull();
    expect(next.get('direction')).toBe('dir-bali');
    expect(next.get('dateFrom')).toBe('2026-07-01');
  });

  it('formats departure chips as dd.mm.yyyy and compares calendar days', () => {
    expect(formatCatalogDateChip('с', '2026-11-01')).toBe('с 01.11.2026');
    expect(formatCatalogDateChip('по', '2026-11-15')).toBe('по 15.11.2026');
    expect(isDepartureRangeInverted('2026-12-01', '2026-11-01')).toBe(true);
    expect(isDepartureRangeInverted('2026-11-01', '2026-11-15')).toBe(false);
    expect(isDepartureRangeInverted('2026-11-15', '2026-11-15')).toBe(false);
    expect(isDepartureRangeInverted('2026-11-01', undefined)).toBe(false);
  });

  it('treats a departure date outside 2000-2100 as its own hint', () => {
    expect(DEPARTURE_YEAR_HINT).toBe('Выберите дату в пределах 2000\u20132100 года');
    expect(DEPARTURE_YEAR_HINT.includes('\u2014')).toBe(false);
    expect(isDepartureDateOutOfRange('1999-12-31')).toBe(true);
    expect(isDepartureDateOutOfRange('2101-01-01')).toBe(true);
    expect(isDepartureDateOutOfRange('9999-12-31')).toBe(true);
    expect(isDepartureDateOutOfRange('2000-01-01')).toBe(false);
    expect(isDepartureDateOutOfRange('2100-12-31')).toBe(false);
    expect(isDepartureDateOutOfRange(undefined)).toBe(false);
    expect(departureDateIssue('1999-12-31', '2026-11-01')).toBe('year');
    expect(departureDateIssue('2026-11-01', '9999-12-31')).toBe('year');
    expect(departureDateIssue('2026-12-01', '2026-11-01')).toBe('order');
    expect(departureDateIssue('2000-01-01', '2100-12-31')).toBeUndefined();
  });

  it('builds a catalog path from filters', () => {
    expect(catalogPath({})).toBe('/tours');
    expect(catalogPath({ directionId: 'dir-bali', dateFrom: '2026-11-01' })).toBe(
      '/tours?direction=dir-bali&dateFrom=2026-11-01',
    );
    expect(catalogPath({ dateFrom: '2026-11-02', dateTo: '2026-11-02' })).toBe(
      '/tours?dateFrom=2026-11-02&dateTo=2026-11-02',
    );
  });

  it('maps a home search to a direction, a country, or the typed text', () => {
    expect(resolveHomeSearch({ destination: '  бали ', date: '2026-11-01' }, destinations)).toEqual({
      directionId: 'dir-bali',
      dateFrom: '2026-11-01',
    });

    expect(resolveHomeSearch({ destination: 'Индонезия', date: '' }, destinations)).toEqual({
      country: ['Индонезия'],
    });

    expect(resolveHomeSearch({ destination: 'Атлантида', date: 'не дата' }, destinations)).toEqual({
      country: ['Атлантида'],
    });

    expect(resolveHomeSearch({ destination: '', date: '' }, destinations)).toEqual({});
  });
});
