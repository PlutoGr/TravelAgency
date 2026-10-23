import { beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { screen, waitFor } from '@testing-library/react';
import toast from 'react-hot-toast';
import ProfilePage from './ProfilePage';
import * as authApi from '@/api/auth';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import type { User } from '@/types';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

const user: User = {
  id: 'user-1',
  email: 'anna@example.com',
  firstName: 'Анна',
  lastName: 'Смирнова',
  phone: '+79990001122',
  role: 'client',
  createdAt: '2026-01-01T00:00:00Z',
};

describe('ProfilePage save errors', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
  });

  it('shows the shared fallback and hides the English detail', async () => {
    vi.spyOn(authApi, 'updateProfile').mockRejectedValue({
      response: {
        status: 400,
        data: {
          code: 'NotAKnownCode',
          detail: 'Phone number format is invalid.',
        },
      },
    });

    renderPage(<ProfilePage />);
    await userEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Не удалось выполнить запрос. Попробуйте ещё раз.',
    );
    expect(screen.queryByText('Phone number format is invalid.')).not.toBeInTheDocument();
    expect(toast.error).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
    expect(screen.getByLabelText('Email')).toHaveValue('anna@example.com');
  });
});

describe('ProfilePage unfinished photo and passport', () => {
  const avatarUrl = 'https://example.com/avatar.png';
  const profileUser: User = {
    ...user,
    avatar: avatarUrl,
    passport: '4510 123456',
  };

  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    useAuthStore.setState({ user: profileUser, isAuthenticated: true, isLoading: false });
  });

  it('keeps the camera and passport inactive and still saves name, last name and phone', async () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockResolvedValue({
      ...profileUser,
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
    });
    const fileClick = vi.spyOn(HTMLInputElement.prototype, 'click');
    const userEvents = userEvent.setup();

    renderPage(<ProfilePage />);

    const camera = screen.getByRole('button', { name: 'Появится позже' });
    expect(camera).toBeDisabled();
    expect(document.querySelector('input[type="file"]')).toBeNull();

    await userEvents.click(camera);

    expect(document.querySelector('input[type="file"]')).toBeNull();
    expect(fileClick).not.toHaveBeenCalled();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', avatarUrl);

    const passport = screen.getByLabelText('Паспорт');
    expect(passport).toBeDisabled();
    expect(passport).toHaveValue('4510 123456');
    const caption = document.getElementById('profile-passport-later');
    expect(caption).not.toBeNull();
    expect(caption).toHaveTextContent('Появится позже');
    expect(caption?.textContent?.trim()).toBe('Появится позже');
    expect(caption?.closest('button')).toBeNull();
    await waitFor(() => {
      expect(caption).toBeVisible();
    });
    expect(passport).toHaveAccessibleDescription('Появится позже');
    expect(passport).not.toHaveAttribute('placeholder', 'Пока не поддерживается');
    expect(passport).not.toHaveAttribute('title');
    expect(screen.queryByText('Пока не поддерживается')).not.toBeInTheDocument();

    const email = screen.getByLabelText('Email');
    expect(email).toBeDisabled();
    expect(email).toHaveValue('anna@example.com');
    expect(email).not.toHaveAccessibleDescription('Появится позже');
    expect(email.parentElement?.parentElement).not.toHaveTextContent('Появится позже');

    const name = screen.getByLabelText('Имя');
    const lastName = screen.getByLabelText('Фамилия');
    const phone = screen.getByLabelText('Телефон');
    await userEvents.clear(name);
    await userEvents.type(name, 'Мария');
    await userEvents.clear(lastName);
    await userEvents.type(lastName, 'Ковалева');
    await userEvents.clear(phone);
    await userEvents.type(phone, '+79991112233');
    await userEvents.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
    });
    expect(updateProfile.mock.calls[0][0]).not.toHaveProperty('passport');
  });
});
