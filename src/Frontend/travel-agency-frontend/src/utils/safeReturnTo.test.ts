import { describe, expect, it } from 'vitest';
import { resolveReturnTo } from './safeReturnTo';

describe('resolveReturnTo', () => {
  it('accepts an internal path', () => {
    expect(resolveReturnTo('/dashboard/bookings')).toBe('/dashboard/bookings');
    expect(resolveReturnTo('/dashboard/favorites')).toBe('/dashboard/favorites');
    expect(resolveReturnTo('/')).toBe('/');
  });

  it('rejects protocol-relative URLs, including a backslash that browsers treat as a slash', () => {
    expect(resolveReturnTo('//evil.com')).toBe('/');
    expect(resolveReturnTo('/\\evil.com')).toBe('/');
    expect(resolveReturnTo('/\\\\evil.com')).toBe('/');
  });

  it('rejects absolute URLs and javascript: schemes', () => {
    expect(resolveReturnTo('https://evil.com')).toBe('/');
    expect(resolveReturnTo('http://evil.com')).toBe('/');
    expect(resolveReturnTo('javascript:alert(1)')).toBe('/');
    expect(resolveReturnTo('javascript:alert(document.cookie)')).toBe('/');
  });

  it('falls back when the value is missing', () => {
    expect(resolveReturnTo(null)).toBe('/');
    expect(resolveReturnTo(undefined)).toBe('/');
    expect(resolveReturnTo('')).toBe('/');
  });
});
