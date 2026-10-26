import type { AppRole } from '@/domain/roles';

export type StaffRouteDef = {
  path: string;
  url: string;
};

/** Панель менеджера. Админ входит по иерархии, даже если в списке только manager. */
export const MANAGER_AREA_ROLES = ['manager', 'admin'] as const satisfies readonly AppRole[];

export const ADMIN_AREA_ROLES = ['admin'] as const satisfies readonly AppRole[];

export const MANAGER_ROUTE_DEFS: readonly StaffRouteDef[] = [
  { path: 'manager', url: '/manager' },
  { path: 'manager/bookings', url: '/manager/bookings' },
  { path: 'manager/bookings/:id', url: '/manager/bookings/1' },
  { path: 'manager/tours/new', url: '/manager/tours/new' },
  { path: 'manager/tours/:tourId', url: '/manager/tours/tour-1' },
  { path: 'manager/tours', url: '/manager/tours' },
  { path: 'manager/clients', url: '/manager/clients' },
];

export const ADMIN_ROUTE_DEFS: readonly StaffRouteDef[] = [
  { path: 'admin', url: '/admin' },
  { path: 'admin/users', url: '/admin/users' },
  { path: 'admin/users/:userId', url: '/admin/users/11111111-1111-1111-1111-111111111111' },
  { path: 'admin/settings', url: '/admin/settings' },
];
