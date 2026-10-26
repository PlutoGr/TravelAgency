import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import AdminUsersPage from './AdminUsersPage';
import { EMPTY_USERS_TEXT } from '@/domain/adminUsers';

const getAdminUsers = vi.fn();

vi.mock('@/api/adminUsers', () => ({
  getAdminUsers: (...args: unknown[]) => getAdminUsers(...args),
}));

const user = {
  id: '11111111-1111-1111-1111-111111111111',
  firstName: 'Иван',
  lastName: 'Петров',
  email: 'ivan@example.com',
  phone: '+79990001122',
  role: 'Client',
  status: 'Active',
  createdAt: '2026-10-01T10:00:00.000Z',
  lastLoginAt: null,
  blockedAt: null,
  blockReason: null,
};

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminUsersPage />
    </MemoryRouter>,
  );
}

describe('AdminUsersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the empty result text', async () => {
    getAdminUsers.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 20 });
    renderPage();

    expect(await screen.findByText(EMPTY_USERS_TEXT)).toBeInTheDocument();
  });

  it('renders a user row and sends the search query', async () => {
    getAdminUsers.mockResolvedValue({ items: [user], total: 1, page: 1, pageSize: 20 });
    const actor = userEvent.setup();
    renderPage();

    expect(await screen.findByText('Иван Петров')).toBeInTheDocument();
    expect(screen.getByText('ivan@example.com')).toBeInTheDocument();
    const table = screen.getByRole('table');
    expect(within(table).getByText('Клиент')).toBeInTheDocument();
    expect(within(table).getByText('Активен')).toBeInTheDocument();
    expect(screen.getByText('нет данных')).toBeInTheDocument();

    await actor.type(screen.getByLabelText('Поиск'), 'ivan');
    await actor.click(screen.getByRole('button', { name: 'Найти' }));

    await waitFor(() => {
      expect(getAdminUsers).toHaveBeenCalledWith(expect.objectContaining({ q: 'ivan', page: 1 }));
    });
  });
});
