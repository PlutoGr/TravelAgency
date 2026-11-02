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

  it.each(['client', 'manager', 'admin'] as const)('shows the narrow card for %s', (role) => {
    useAuthStore.setState({ user: person(role), isAuthenticated: true, isLoading: false });
    renderPage(<ProfilePage />, '/dashboard/profile');

    expect(screen.getByText('МИ')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'марина иванова' })).toBeInTheDocument();
    expect(screen.getByText('marina@example.com')).toBeInTheDocument();
    expect(screen.getByLabelText('Имя')).toHaveValue('марина');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('иванова');
    expect(screen.getByLabelText('Телефон')).toHaveValue('');
    const email = screen.getByLabelText('Email');
    expect(email).toBeDisabled();
    expect(email).toHaveValue('marina@example.com');
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
    expect(screen.queryByText('не указано')).not.toBeInTheDocument();
    expect(screen.queryByText('Появится позже')).not.toBeInTheDocument();
    expect(screen.queryByText(/Client|Manager|Admin|2026/)).not.toBeInTheDocument();
    for (const label of removed) {
      expect(screen.queryByLabelText(label)).not.toBeInTheDocument();
    }
    const preview = screen.queryByRole('button', { name: 'Как меня видит клиент' });
    if (role === 'client') expect(preview).not.toBeInTheDocument();
    else expect(preview).toBeEnabled();
  });

  it('keeps settings to the inactive password block', () => {
    for (const role of ['client', 'manager', 'admin'] as const) {
      useAuthStore.setState({ user: person(role), isAuthenticated: true, isLoading: false });
      const view = renderPage(<ProfilePage />, '/dashboard/profile?tab=settings');
      expect(screen.getByRole('region', { name: 'Пароль' })).toBeInTheDocument();
      expect(screen.getByText('Появится позже')).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Как меня видит клиент' })).not.toBeInTheDocument();
      expect(screen.queryByRole('region', { name: 'Видимость' })).not.toBeInTheDocument();
      expect(screen.queryByRole('region', { name: 'Уведомления' })).not.toBeInTheDocument();
      expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
      view.unmount();
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

    fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Черновик' } });
    fireEvent.click(screen.getByRole('button', { name: 'Как меня видит клиент' }));
    const dialog = await screen.findByRole('dialog', { name: 'Карточка менеджера' });
    expect(getManagerCard).toHaveBeenCalledWith('user-1');
    expect(within(dialog).getByTestId('manager-card-name')).toHaveTextContent('Марина Иванова');
    expect(within(dialog).queryByText('Черновик')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('+79991234567')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('marina@example.com')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }));
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
    fireEvent.click(screen.getByRole('button', { name: 'Как меня видит клиент' }));
    expect(await screen.findByTestId('manager-card-name')).toHaveTextContent('Анна Смирнова');
    expect(getManagerCard).toHaveBeenCalledTimes(2);
  });
});
