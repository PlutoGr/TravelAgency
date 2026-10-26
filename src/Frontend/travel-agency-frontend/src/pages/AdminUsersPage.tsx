import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import { PageTransition } from '@/components/common';
import { Button, Input } from '@/components/ui';
import { getAdminUsers } from '@/api/adminUsers';
import {
  EMPTY_USERS_TEXT,
  ROLE_OPTIONS,
  STATUS_OPTIONS,
  roleLabel,
  statusLabel,
  userDisplayName,
  type AdminUser,
} from '@/domain/adminUsers';

function formatStamp(value: string | null): string {
  if (!value) return 'нет данных';
  const date = parseISO(value);
  if (Number.isNaN(date.getTime())) return 'нет данных';
  return format(date, 'd MMM yyyy, HH:mm', { locale: ru });
}

export default function AdminUsersPage() {
  const [q, setQ] = useState('');
  const [role, setRole] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [applied, setApplied] = useState({ q: '', role: '', status: '', page: 1 });
  const [items, setItems] = useState<AdminUser[]>([]);
  const [total, setTotal] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    getAdminUsers({
      q: applied.q || undefined,
      role: applied.role || undefined,
      status: applied.status || undefined,
      page: applied.page,
    })
      .then((result) => {
        if (cancelled) return;
        setItems(result.items);
        setTotal(result.total);
        setPageSize(result.pageSize);
      })
      .catch(() => {
        if (!cancelled) setLoadError('Не удалось загрузить пользователей');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [applied]);

  const pages = Math.max(1, Math.ceil(total / pageSize));

  function search(event?: FormEvent) {
    event?.preventDefault();
    setPage(1);
    setApplied({ q: q.trim(), role, status, page: 1 });
  }

  function resetFilters() {
    setQ('');
    setRole('');
    setStatus('');
    setPage(1);
    setApplied({ q: '', role: '', status: '', page: 1 });
  }

  function goTo(nextPage: number) {
    setPage(nextPage);
    setApplied((current) => ({ ...current, page: nextPage }));
  }

  return (
    <PageTransition>
      <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">Пользователи</h1>
      <p className="mt-3 max-w-2xl text-sm text-warm-gray">
        Поиск, роль и блокировка. Создать или удалить аккаунт здесь нельзя.
      </p>

      <form onSubmit={search} className="mt-6 grid gap-3 rounded-2xl border border-sand bg-white p-4 sm:grid-cols-4">
        <Input label="Поиск" value={q} onChange={(event) => setQ(event.target.value)} placeholder="Имя, email или телефон" />
        <label className="block text-sm font-medium text-dark">
          Роль
          <select
            aria-label="Фильтр по роли"
            className="mt-1 w-full rounded-xl border border-sand bg-white px-3 py-2 text-sm"
            value={role}
            onChange={(event) => setRole(event.target.value)}
          >
            <option value="">Все роли</option>
            {ROLE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-medium text-dark">
          Статус
          <select
            aria-label="Фильтр по статусу"
            className="mt-1 w-full rounded-xl border border-sand bg-white px-3 py-2 text-sm"
            value={status}
            onChange={(event) => setStatus(event.target.value)}
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value || 'all'} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <div className="flex items-end gap-2">
          <Button type="submit" size="sm">
            Найти
          </Button>
          <Button type="button" size="sm" variant="ghost" onClick={resetFilters}>
            Сбросить
          </Button>
        </div>
      </form>

      {loadError && (
        <p role="alert" className="mt-4 text-sm text-red-500">
          {loadError}
        </p>
      )}

      {!loading && !loadError && items.length === 0 && (
        <p className="mt-6 text-sm text-warm-gray">{EMPTY_USERS_TEXT}</p>
      )}

      {items.length > 0 && (
        <div className="mt-6 overflow-x-auto rounded-2xl border border-sand bg-white">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-sand text-warm-gray">
              <tr>
                <th className="px-4 py-3 font-medium">Имя</th>
                <th className="px-4 py-3 font-medium">Email</th>
                <th className="px-4 py-3 font-medium">Роль</th>
                <th className="px-4 py-3 font-medium">Статус</th>
                <th className="px-4 py-3 font-medium">Регистрация</th>
                <th className="px-4 py-3 font-medium">Последний вход</th>
                <th className="px-4 py-3 font-medium" />
              </tr>
            </thead>
            <tbody>
              {items.map((user) => (
                <tr key={user.id} className="border-b border-sand last:border-0">
                  <td className="px-4 py-3">{userDisplayName(user)}</td>
                  <td className="px-4 py-3">{user.email}</td>
                  <td className="px-4 py-3">{roleLabel(user.role)}</td>
                  <td className="px-4 py-3">{statusLabel(user.status)}</td>
                  <td className="px-4 py-3">{formatStamp(user.createdAt)}</td>
                  <td className="px-4 py-3">{formatStamp(user.lastLoginAt)}</td>
                  <td className="px-4 py-3">
                    <Link className="font-medium text-primary hover:underline" to={`/admin/users/${user.id}`}>
                      Карточка
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {total > pageSize && (
        <div className="mt-4 flex items-center gap-3 text-sm">
          <Button type="button" size="sm" variant="secondary" disabled={page <= 1} onClick={() => goTo(page - 1)}>
            Назад
          </Button>
          <span>
            Страница {page} из {pages}
          </span>
          <Button type="button" size="sm" variant="secondary" disabled={page >= pages} onClick={() => goTo(page + 1)}>
            Далее
          </Button>
        </div>
      )}
    </PageTransition>
  );
}
