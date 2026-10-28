import { describe, expect, it, vi } from 'vitest';
import { tourismTenurePhrase } from './tourismTenure';

const now = new Date('2026-01-01T00:30:00Z');

describe('tourismTenurePhrase', () => {
  it('склоняет стаж от календарного года UTC 2026', () => {
    expect(now.getUTCFullYear()).toBe(2026);
    expect(tourismTenurePhrase(2026, now)).toBe('В туризме меньше года');
    expect(tourismTenurePhrase(2025, now)).toBe('В туризме 1 год');
    expect(tourismTenurePhrase(2024, now)).toBe('В туризме 2 года');
    expect(tourismTenurePhrase(2023, now)).toBe('В туризме 3 года');
    expect(tourismTenurePhrase(2022, now)).toBe('В туризме 4 года');
    expect(tourismTenurePhrase(2004, now)).toBe('В туризме 22 года');
    expect(tourismTenurePhrase(2021, now)).toBe('В туризме 5 лет');
    expect(tourismTenurePhrase(2018, now)).toBe('В туризме 8 лет');
    expect(tourismTenurePhrase(2015, now)).toBe('В туризме 11 лет');
    expect(tourismTenurePhrase(2014, now)).toBe('В туризме 12 лет');
    expect(tourismTenurePhrase(2013, now)).toBe('В туризме 13 лет');
    expect(tourismTenurePhrase(2012, now)).toBe('В туризме 14 лет');
    expect(tourismTenurePhrase(2005, now)).toBe('В туризме 21 год');
    expect(tourismTenurePhrase(2027, now)).toBeNull();
    expect(tourismTenurePhrase('abc', now)).toBeNull();
    expect(tourismTenurePhrase(undefined, now)).toBeNull();
    expect(tourismTenurePhrase(null, now)).toBeNull();
  });

  it('берёт год UTC, даже если локальный год другой', () => {
    const spy = vi.spyOn(Date.prototype, 'getFullYear').mockReturnValue(1999);
    try {
      expect(tourismTenurePhrase(2018, now)).toBe('В туризме 8 лет');
    } finally {
      spy.mockRestore();
    }
  });
});
