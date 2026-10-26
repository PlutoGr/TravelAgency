export const EMPTY_USERS_TEXT =
  'Пользователи не найдены. Измените запрос или сбросьте фильтр';

export const ROLE_OPTIONS = [
  { value: 'Client', label: 'Клиент' },
  { value: 'Manager', label: 'Менеджер' },
  { value: 'Admin', label: 'Админ' },
] as const;

export type AdminRole = (typeof ROLE_OPTIONS)[number]['value'];

export const STATUS_OPTIONS = [
  { value: '', label: 'Все статусы' },
  { value: 'Active', label: 'Активен' },
  { value: 'Blocked', label: 'Заблокирован' },
] as const;

export function roleLabel(role: string): string {
  return ROLE_OPTIONS.find((option) => option.value === role)?.label ?? role;
}

export function statusLabel(status: string): string {
  if (status === 'Blocked') return 'Заблокирован';
  if (status === 'Active') return 'Активен';
  return status;
}

export function userDisplayName(user: { firstName: string; lastName: string }): string {
  return `${user.firstName} ${user.lastName}`.trim();
}

export function roleChangeConfirm(name: string, role: string): string {
  return `Изменить роль пользователя ${name} на «${roleLabel(role)}»?`;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  role: AdminRole | string;
  status: 'Active' | 'Blocked' | string;
  createdAt: string;
  lastLoginAt: string | null;
  blockedAt: string | null;
  blockReason: string | null;
}

export interface AdminUserList {
  items: AdminUser[];
  total: number;
  page: number;
  pageSize: number;
}

export interface UserAdminAuditItem {
  id: string;
  action: 'RoleChanged' | 'Blocked' | 'Unblocked' | string;
  oldRole: string | null;
  newRole: string | null;
  reason: string | null;
  actorUserId: string;
  actorName: string;
  createdAt: string;
}

export interface AdminUserDetails {
  user: AdminUser;
  audit: UserAdminAuditItem[];
}

export interface AdminUserMutation {
  user: AdminUser;
  message: string;
}

export interface AdminUserQuery {
  q?: string;
  role?: string;
  status?: string;
  page?: number;
}

export const BLOCKED_LOGIN_PREFIX = 'Доступ к аккаунту ограничен. Свяжитесь с поддержкой';
