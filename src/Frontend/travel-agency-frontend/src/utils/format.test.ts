import { describe, expect, it } from 'vitest';
import { formatDate } from './format';

describe('formatDate', () => {
  it('returns an em dash for undefined', () => {
    expect(formatDate(undefined)).toBe('—');
  });

  it('returns an em dash for null and empty string', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate('')).toBe('—');
  });

  it('returns an em dash for garbage', () => {
    expect(formatDate('garbage')).toBe('—');
    expect(formatDate('not-a-date')).toBe('—');
    expect(formatDate('32.13.2026')).toBe('—');
  });

  it('formats a real ISO date', () => {
    expect(formatDate('2026-04-15T10:30:00Z')).toMatch(/15/);
    expect(formatDate('2026-04-15T10:30:00Z')).not.toBe('—');
  });
});
