export const APP_ROLES = ['client', 'manager', 'admin'] as const;

export type AppRole = (typeof APP_ROLES)[number];

const RANK: Record<AppRole, number> = {
  client: 1,
  manager: 2,
  admin: 3,
};

export function isAppRole(role: string): role is AppRole {
  return (APP_ROLES as readonly string[]).includes(role);
}

/** Роль не ниже заданной: admin проходит проверку manager, manager не проходит проверку admin. */
export function hasAtLeastRole(role: string | null | undefined, minimum: AppRole): boolean {
  if (!role || !isAppRole(role)) return false;
  return RANK[role] >= RANK[minimum];
}

/**
 * Доступ по нижней роли из списка.
 * Список `['manager']` пускает и admin: новая страница менеджера не требует отдельной правки для админа.
 */
export function allowsRole(
  role: string | null | undefined,
  allowedRoles: readonly AppRole[],
): boolean {
  if (allowedRoles.length === 0) return false;
  const minimum = allowedRoles.reduce((lowest, candidate) =>
    RANK[candidate] < RANK[lowest] ? candidate : lowest,
  );
  return hasAtLeastRole(role, minimum);
}
