import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Button, Modal } from '@/components/ui';
import {
  adminUserErrorMessage,
  blockAdminUser,
  changeAdminUserRole,
  getAdminUser,
  unblockAdminUser,
} from '@/api/adminUsers';
import {
  ROLE_OPTIONS,
  roleChangeConfirm,
  roleLabel,
  statusLabel,
  userDisplayName,
  type AdminUser,
  type UserAdminAuditItem,
} from '@/domain/adminUsers';
import { useAuthStore } from '@/store/authStore';

function formatStamp(value: string | null): string {
  if (!value) return 'нет данных';
  const date = parseISO(value);
  if (Number.isNaN(date.getTime())) return 'нет данных';
  return format(date, 'd MMM yyyy, HH:mm', { locale: ru });
}

function auditText(item: UserAdminAuditItem): string {
  if (item.action === 'RoleChanged') {
    return `Роль: ${roleLabel(item.oldRole ?? '')} → ${roleLabel(item.newRole ?? '')}`;
  }
  if (item.action === 'Blocked') {
    return `Блокировка. Причина: ${item.reason ?? ''}`;
  }
  if (item.action === 'Unblocked') return 'Разблокировка';
  return item.action;
}

export default function AdminUserCardPage() {
  const { userId = '' } = useParams();
  const currentUserId = useAuthStore((state) => state.user?.id ?? '');
  const [user, setUser] = useState<AdminUser | null>(null);
  const [audit, setAudit] = useState<UserAdminAuditItem[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [pendingRole, setPendingRole] = useState<string | null>(null);
  const [blockOpen, setBlockOpen] = useState(false);
  const [unblockOpen, setUnblockOpen] = useState(false);
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setLoadError(null);
    getAdminUser(userId)
      .then((details) => {
        if (cancelled) return;
        setUser(details.user);
        setAudit(details.audit);
      })
      .catch(() => {
        if (!cancelled) setLoadError('Не удалось загрузить карточку');
      });
    return () => {
      cancelled = true;
    };
  }, [userId]);

  const isSelf = user !== null && currentUserId.toLowerCase() === user.id.toLowerCase();
  const name = user ? userDisplayName(user) : '';

  async function confirmRole() {
    if (!user || !pendingRole) return;
    setSaving(true);
    try {
      const saved = await changeAdminUserRole(user.id, pendingRole);
      setUser(saved.user);
      setPendingRole(null);
      toast.success(saved.message);
      const details = await getAdminUser(user.id);
      setAudit(details.audit);
    } catch (error) {
      toast.error(adminUserErrorMessage(error, 'Не удалось изменить роль'));
    } finally {
      setSaving(false);
    }
  }

  async function confirmBlock() {
    if (!user) return;
    const trimmed = reason.trim();
    if (!trimmed) {
      setReasonError('Укажите причину блокировки');
      return;
    }
    if (trimmed.length > 500) {
      setReasonError('Причина не длиннее 500 символов');
      return;
    }
    setSaving(true);
    try {
      const saved = await blockAdminUser(user.id, trimmed);
      setUser(saved.user);
      setBlockOpen(false);
      setReason('');
      setReasonError(null);
      toast.success(saved.message);
      const details = await getAdminUser(user.id);
      setAudit(details.audit);
    } catch (error) {
      toast.error(adminUserErrorMessage(error, 'Не удалось заблокировать пользователя'));
    } finally {
      setSaving(false);
    }
  }

  async function confirmUnblock() {
    if (!user) return;
    setSaving(true);
    try {
      const saved = await unblockAdminUser(user.id);
      setUser(saved.user);
      setUnblockOpen(false);
      toast.success(saved.message);
      const details = await getAdminUser(user.id);
      setAudit(details.audit);
    } catch (error) {
      toast.error(adminUserErrorMessage(error, 'Не удалось разблокировать пользователя'));
    } finally {
      setSaving(false);
    }
  }

  return (
    <PageTransition>
      <Link to="/admin/users" className="text-sm font-medium text-primary hover:underline">
        К списку пользователей
      </Link>
      <h1 className="mt-3 font-heading text-2xl font-bold text-dark sm:text-3xl">
        {user ? name : 'Карточка пользователя'}
      </h1>
      {loadError && (
        <p role="alert" className="mt-4 text-sm text-red-500">
          {loadError}
        </p>
      )}

      {user && (
        <div className="mt-6 max-w-3xl space-y-6">
          <section className="rounded-2xl border border-sand bg-white p-5 sm:p-6">
            <dl className="grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <dt className="text-warm-gray">Email</dt>
                <dd>{user.email}</dd>
              </div>
              <div>
                <dt className="text-warm-gray">Телефон</dt>
                <dd>{user.phone || 'нет данных'}</dd>
              </div>
              <div>
                <dt className="text-warm-gray">Статус</dt>
                <dd>{statusLabel(user.status)}</dd>
              </div>
              <div>
                <dt className="text-warm-gray">Последний вход</dt>
                <dd>{formatStamp(user.lastLoginAt)}</dd>
              </div>
              <div>
                <dt className="text-warm-gray">Регистрация</dt>
                <dd>{formatStamp(user.createdAt)}</dd>
              </div>
              {user.status === 'Blocked' && (
                <>
                  <div>
                    <dt className="text-warm-gray">Дата блокировки</dt>
                    <dd>{formatStamp(user.blockedAt)}</dd>
                  </div>
                  <div className="sm:col-span-2">
                    <dt className="text-warm-gray">Причина</dt>
                    <dd>{user.blockReason}</dd>
                  </div>
                </>
              )}
            </dl>

            <label className="mt-6 block text-sm font-medium text-dark" htmlFor="user-role">
              Роль
            </label>
            <select
              id="user-role"
              className="mt-1 w-full max-w-xs rounded-xl border border-sand bg-white px-3 py-2 text-sm"
              value={pendingRole ?? user.role}
              onChange={(event) => {
                const next = event.target.value;
                setPendingRole(next === user.role ? null : next);
              }}
            >
              {ROLE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            <div className="mt-4 flex flex-wrap gap-2">
              {user.status === 'Blocked' ? (
                <Button type="button" variant="secondary" onClick={() => setUnblockOpen(true)}>
                  Разблокировать
                </Button>
              ) : (
                <Button type="button" variant="danger" disabled={isSelf} onClick={() => setBlockOpen(true)}>
                  Заблокировать
                </Button>
              )}
            </div>
          </section>

          <section className="rounded-2xl border border-sand bg-white p-5 sm:p-6">
            <h2 className="font-heading text-lg font-semibold text-dark">Журнал</h2>
            {audit.length === 0 ? (
              <p className="mt-3 text-sm text-warm-gray">Записей пока нет</p>
            ) : (
              <ul className="mt-3 space-y-3 text-sm">
                {audit.map((item) => (
                  <li key={item.id} className="border-b border-sand pb-3 last:border-0">
                    <p>{auditText(item)}</p>
                    <p className="text-warm-gray">
                      {item.actorName}, {formatStamp(item.createdAt)}
                    </p>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      )}

      <Modal
        isOpen={pendingRole !== null}
        onClose={() => setPendingRole(null)}
        title="Смена роли"
        size="sm"
      >
        <p>{user && pendingRole ? roleChangeConfirm(name, pendingRole) : ''}</p>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={() => setPendingRole(null)}>
            Отмена
          </Button>
          <Button type="button" isLoading={saving} onClick={() => void confirmRole()}>
            Подтвердить
          </Button>
        </div>
      </Modal>

      <Modal isOpen={blockOpen} onClose={() => setBlockOpen(false)} title="Блокировка" size="sm">
        <p>Заблокировать {name}?</p>
        <label className="mt-4 block text-sm font-medium text-dark" htmlFor="block-reason">
          Причина
        </label>
        <textarea
          id="block-reason"
          className="mt-1 w-full rounded-xl border border-sand px-3 py-2 text-sm"
          maxLength={500}
          rows={4}
          value={reason}
          onChange={(event) => {
            setReason(event.target.value);
            setReasonError(null);
          }}
        />
        {reasonError && (
          <p role="alert" className="mt-2 text-sm text-red-500">
            {reasonError}
          </p>
        )}
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={() => setBlockOpen(false)}>
            Отмена
          </Button>
          <Button type="button" variant="danger" isLoading={saving} onClick={() => void confirmBlock()}>
            Подтвердить
          </Button>
        </div>
      </Modal>

      <Modal isOpen={unblockOpen} onClose={() => setUnblockOpen(false)} title="Разблокировка" size="sm">
        <p>Разблокировать {name}? Старые сессии не восстановятся.</p>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={() => setUnblockOpen(false)}>
            Отмена
          </Button>
          <Button type="button" isLoading={saving} onClick={() => void confirmUnblock()}>
            Подтвердить
          </Button>
        </div>
      </Modal>
    </PageTransition>
  );
}
