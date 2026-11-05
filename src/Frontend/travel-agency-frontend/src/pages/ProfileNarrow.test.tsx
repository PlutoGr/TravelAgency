import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import ProfilePage from './ProfilePage';
import * as authApi from '@/api/auth';
import { getManagerCard } from '@/api/managerCard';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import type { User } from '@/types';

vi.mock('@/api/managerCard', () => ({
  getManagerCard: vi.fn(),
}));

const removed = ['Город', 'О себе', 'В туризме с', 'Языки', 'Специализация', 'Паспорт'];

function person(role: User['role'], firstName = 'марина', lastName = 'иванова'): User {
  return {
    id: 'user-1',
    email: 'marina@example.com',
    firstName,
    lastName,
    phone: '',
    role,
    createdAt: '2026-01-01T00:00:00Z',
  };
}

describe('narrow profile', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });
  });

  it('shows a loading line until the account is present', () => {
    renderPage(<ProfilePage />, '/dashboard/profile');
    expect(screen.getByText('Загрузка профиля...')).toBeInTheDocument();
  });

  it.each(['client', 'manager', 'admin'] as const)('shows the same two blocks for %s', (role) => {
    useAuthStore.setState({ user: person(role), isAuthenticated: true, isLoading: false });
    renderPage(<ProfilePage />, '/dashboard/profile?tab=settings');

    const personal = screen.getByRole('region', { name: 'Личные данные' });
    const account = screen.getByRole('region', { name: 'Вход в аккаунт' });
    expect(personal.compareDocumentPosition(account) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(personal).getByText('МИ')).toBeInTheDocument();
    expect(within(personal).getByText('марина иванова')).toBeInTheDocument();
    expect(within(personal).getByText('Телефон')).toBeInTheDocument();
    expect(within(personal).queryByText('marina@example.com')).not.toBeInTheDocument();
    expect(within(account).getByText('marina@example.com')).toBeInTheDocument();
    expect(within(account).getByText('••••••••')).toBeInTheDocument();
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument();
    expect(screen.queryByText('Настройки')).not.toBeInTheDocument();
    expect(screen.queryByText('Смена адреса - на вкладке «Настройки».')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Сохранить' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Изменить' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Сменить email' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Сменить пароль' })).toBeInTheDocument();
    expect(screen.queryByText('не указано')).not.toBeInTheDocument();
    expect(screen.queryByText('Появится позже')).not.toBeInTheDocument();
    expect(screen.queryByText(/Client|Manager|Admin|2026/)).not.toBeInTheDocument();
    for (const label of removed) {
      expect(screen.queryByLabelText(label)).not.toBeInTheDocument();
    }
    if (role === 'client') expect(screen.queryByRole('tab')).not.toBeInTheDocument();
    else {
      expect(screen.getByRole('tab', { name: 'Аккаунт' })).toHaveAttribute('aria-selected', 'true');
      expect(screen.getByRole('tab', { name: 'Визитка для клиентов' })).toBeInTheDocument();
    }
  });

  it('opens the saved card, not the draft, and refreshes after save', async () => {
    useAuthStore.setState({
      user: person('manager', 'Марина', 'Иванова'),
      isAuthenticated: true,
      isLoading: false,
    });
    vi.mocked(getManagerCard).mockResolvedValue({
      id: 'user-1',
      displayName: 'Марина Иванова',
    });
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockResolvedValue({
      ...person('manager', 'Анна', 'Смирнова'),
      phone: '+79991234567',
    });
    renderPage(<ProfilePage />, '/dashboard/profile');

    fireEvent.click(screen.getByRole('tab', { name: 'Аккаунт' }));
    fireEvent.click(screen.getByRole('button', { name: 'Изменить' }));
    fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Черновик' } });
    expect(screen.getByText('Марина Иванова')).toBeInTheDocument();
    expect(screen.queryByText('Черновик')).not.toBeInTheDocument();
    const callsWhileDraft = vi.mocked(getManagerCard).mock.calls.length;
    expect(callsWhileDraft).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole('tab', { name: 'Визитка для клиентов' }));
    const card = await screen.findByTestId('manager-card-name');
    expect(getManagerCard).toHaveBeenCalledWith('user-1');
    expect(card).toHaveTextContent('Марина Иванова');
    expect(screen.queryByText('Черновик')).not.toBeInTheDocument();
    expect(screen.queryByText('+79991234567')).not.toBeInTheDocument();
    expect(screen.queryByText('marina@example.com')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Аккаунт' }));
    fireEvent.click(screen.getByRole('button', { name: 'Изменить' }));
    fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Анна' } });
    fireEvent.change(screen.getByLabelText('Фамилия'), { target: { value: 'Смирнова' } });
    fireEvent.change(screen.getByLabelText('Телефон'), { target: { value: '+79991234567' } });
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79991234567',
    });

    vi.mocked(getManagerCard).mockResolvedValue({
      id: 'user-1',
      displayName: 'Анна Смирнова',
    });
    const beforeRefresh = vi.mocked(getManagerCard).mock.calls.length;
    fireEvent.click(screen.getByRole('tab', { name: 'Визитка для клиентов' }));
    expect(await screen.findByTestId('manager-card-name')).toHaveTextContent('Анна Смирнова');
    expect(getManagerCard).toHaveBeenCalledTimes(beforeRefresh + 1);
  });
});
