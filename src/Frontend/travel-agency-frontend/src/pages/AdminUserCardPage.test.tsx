import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import toast from 'react-hot-toast';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import AdminUserCardPage from './AdminUserCardPage';
import { roleChangeConfirm } from '@/domain/adminUsers';
import { useAuthStore } from '@/store/authStore';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

const getAdminUser = vi.fn();
const changeAdminUserRole = vi.fn();
const blockAdminUser = vi.fn();
const unblockAdminUser = vi.fn();

vi.mock('@/api/adminUsers', () => ({
  getAdminUser: (...args: unknown[]) => getAdminUser(...args),
  changeAdminUserRole: (...args: unknown[]) => changeAdminUserRole(...args),
  blockAdminUser: (...args: unknown[]) => blockAdminUser(...args),
  unblockAdminUser: (...args: unknown[]) => unblockAdminUser(...args),
  adminUserErrorMessage: (error: unknown, fallback: string) => {
    const detail = (error as { response?: { data?: { detail?: string } } }).response?.data?.detail;
    return detail || fallback;
  },
}));

const userId = '22222222-2222-2222-2222-222222222222';

function person(overrides: Record<string, unknown> = {}) {
  return {
    id: userId,
    firstName: 'Иван',
    lastName: 'Петров',
    email: 'ivan@example.com',
    phone: null,
    role: 'Client',
    status: 'Active',
    createdAt: '2026-10-01T10:00:00.000Z',
    lastLoginAt: '2026-10-20T10:00:00.000Z',
    blockedAt: null,
    blockReason: null,
    ...overrides,
  };
}

function details(user = person(), audit: unknown[] = []) {
  return { user, audit };
}

function renderCard() {
  return render(
    <MemoryRouter initialEntries={[`/admin/users/${userId}`]}>
      <Routes>
        <Route path="/admin/users/:userId" element={<AdminUserCardPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('AdminUserCardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        email: 'admin@example.com',
        firstName: 'Админ',
        lastName: 'Главный',
        phone: '',
        city: '',
        role: 'admin',
        createdAt: '2026-01-01T00:00:00.000Z',
      },
      isAuthenticated: true,
      isLoading: false,
    });
    getAdminUser.mockResolvedValue(details());
  });

  it('confirms a role change and updates the card from the response', async () => {
    const actor = userEvent.setup();
    changeAdminUserRole.mockResolvedValue({
      user: person({ role: 'Manager' }),
      message: 'Роль изменена',
    });
    getAdminUser
      .mockResolvedValueOnce(details())
      .mockResolvedValueOnce(
        details(person({ role: 'Manager' }), [
          {
            id: 'audit-1',
            action: 'RoleChanged',
            oldRole: 'Client',
            newRole: 'Manager',
            reason: null,
            actorUserId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            actorName: 'Админ Главный',
            createdAt: '2026-10-26T12:00:00.000Z',
          },
        ]),
      );

    renderCard();
    expect(await screen.findByRole('heading', { name: 'Иван Петров' })).toBeInTheDocument();

    await actor.selectOptions(screen.getByLabelText('Роль'), 'Manager');
    expect(screen.getByText(roleChangeConfirm('Иван Петров', 'Manager'))).toBeInTheDocument();
    await actor.click(screen.getByRole('button', { name: 'Подтвердить' }));

    await waitFor(() => {
      expect(changeAdminUserRole).toHaveBeenCalledWith(userId, 'Manager');
    });
    expect(toast.success).toHaveBeenCalledWith('Роль изменена');
    expect(await screen.findByText('Роль: Клиент → Менеджер')).toBeInTheDocument();
    expect(screen.getByText(/Админ Главный/)).toBeInTheDocument();
  });

  it('requires a block reason and then shows the blocked status', async () => {
    const actor = userEvent.setup();
    blockAdminUser.mockResolvedValue({
      user: person({
        status: 'Blocked',
        blockedAt: '2026-10-26T12:00:00.000Z',
        blockReason: 'спам',
      }),
      message: 'Пользователь заблокирован',
    });
    getAdminUser
      .mockResolvedValueOnce(details())
      .mockResolvedValueOnce(
        details(
          person({ status: 'Blocked', blockedAt: '2026-10-26T12:00:00.000Z', blockReason: 'спам' }),
          [
            {
              id: 'audit-2',
              action: 'Blocked',
              oldRole: null,
              newRole: null,
              reason: 'спам',
              actorUserId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
              actorName: 'Админ Главный',
              createdAt: '2026-10-26T12:00:00.000Z',
            },
          ],
        ),
      );

    renderCard();
    await actor.click(await screen.findByRole('button', { name: 'Заблокировать' }));
    await actor.click(screen.getByRole('button', { name: 'Подтвердить' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Укажите причину блокировки');
    expect(blockAdminUser).not.toHaveBeenCalled();

    await actor.type(screen.getByLabelText('Причина'), 'спам');
    await actor.click(screen.getByRole('button', { name: 'Подтвердить' }));

    await waitFor(() => {
      expect(blockAdminUser).toHaveBeenCalledWith(userId, 'спам');
    });
    expect(await screen.findByText('спам')).toBeInTheDocument();
    expect(screen.getByText('Блокировка. Причина: спам')).toBeInTheDocument();
  });

  it('does not let an admin block their own account', async () => {
    useAuthStore.setState({
      user: {
        id: userId,
        email: 'ivan@example.com',
        firstName: 'Иван',
        lastName: 'Петров',
        phone: '',
        city: '',
        role: 'admin',
        createdAt: '2026-01-01T00:00:00.000Z',
      },
      isAuthenticated: true,
      isLoading: false,
    });
    renderCard();

    expect(await screen.findByRole('button', { name: 'Заблокировать' })).toBeDisabled();
  });
});
