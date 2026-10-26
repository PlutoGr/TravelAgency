import { describe, expect, it } from 'vitest';
import { allowsRole, hasAtLeastRole } from './roles';

describe('role hierarchy', () => {
  it('treats admin as at least manager and manager as below admin', () => {
    expect(hasAtLeastRole('admin', 'manager')).toBe(true);
    expect(hasAtLeastRole('manager', 'manager')).toBe(true);
    expect(hasAtLeastRole('client', 'manager')).toBe(false);
    expect(hasAtLeastRole('manager', 'admin')).toBe(false);
    expect(hasAtLeastRole('admin', 'admin')).toBe(true);
    expect(hasAtLeastRole('client', 'client')).toBe(true);
    expect(hasAtLeastRole('admin', 'client')).toBe(true);
    expect(hasAtLeastRole(undefined, 'manager')).toBe(false);
    expect(hasAtLeastRole('owner', 'client')).toBe(false);
  });

  it('lets a higher role through a list that names only the lower role', () => {
    expect(allowsRole('admin', ['manager'])).toBe(true);
    expect(allowsRole('manager', ['manager'])).toBe(true);
    expect(allowsRole('client', ['manager'])).toBe(false);
    expect(allowsRole('manager', ['admin'])).toBe(false);
    expect(allowsRole('client', ['admin'])).toBe(false);
    expect(allowsRole('admin', ['client', 'manager', 'admin'])).toBe(true);
    expect(allowsRole('manager', [])).toBe(false);
  });
});
