import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { User } from '@/types';
import { login, register } from '@/api/auth';
import AuthModal from './AuthModal';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  getMe: vi.fn(),
  updateProfile: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

const clientUser: User = {
  id: 'client-1',
  email: 'client@test.com',
  firstName: 'Анна',
  lastName: 'Клиент',
  phone: '+79990001122',
  city: '',
  role: 'client',
  createdAt: '2025-01-01T00:00:00Z',
};

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

function renderModal() {
  return render(
    <MemoryRouter initialEntries={['/tours/tour-1']}>
      <LocationProbe />
      <Routes>
        <Route path="/tours/:id" element={<h1>Тур</h1>} />
        <Route path="/" element={<h1>Главная</h1>} />
      </Routes>
      <AuthModal />
    </MemoryRouter>,
  );
}

describe('AuthModal intent', () => {
  beforeEach(() => {
    vi.mocked(login).mockReset();
    vi.mocked(login).mockResolvedValue({ user: clientUser });
    vi.mocked(register).mockReset();
    vi.mocked(register).mockResolvedValue({ user: clientUser });
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: null,
      isMobileMenuOpen: false,
    });
  });

  it('keeps a request intent after login and stays on the tour', async () => {
    const user = userEvent.setup();
    useUIStore.getState().openAuthModal('login', {
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-2',
    });
    renderModal();

    await user.type(screen.getByLabelText('Email'), 'client@test.com');
    await user.type(screen.getByLabelText('Пароль'), 'secret');
    await user.click(screen.getByRole('button', { name: 'Войти' }));

    await waitFor(() => {
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });
    expect(screen.getByTestId('location')).toHaveTextContent('/tours/tour-1');
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(useUIStore.getState().authIntent).toEqual({
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-2',
    });
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });

  it('keeps a favorite intent after registration', async () => {
    const user = userEvent.setup();
    useUIStore.getState().openAuthModal('login', { type: 'favorite', tourId: 'tour-1' });
    renderModal();

    await user.click(screen.getByRole('button', { name: 'Регистрация' }));
    expect(useUIStore.getState().authIntent).toEqual({ type: 'favorite', tourId: 'tour-1' });

    await user.type(await screen.findByLabelText('Имя'), 'Анна');
    await user.type(screen.getByLabelText('Фамилия'), 'Клиент');
    await user.type(screen.getByLabelText('Email'), 'client@test.com');
    await user.type(screen.getByLabelText('Телефон'), '+79990001122');
    await user.type(screen.getByLabelText('Пароль'), 'Password1');
    await user.click(screen.getByRole('button', { name: 'Зарегистрироваться' }));

    await waitFor(() => {
      expect(register).toHaveBeenCalledOnce();
    });
    expect(screen.getByTestId('location')).toHaveTextContent('/tours/tour-1');
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(useUIStore.getState().authIntent).toEqual({ type: 'favorite', tourId: 'tour-1' });
  });

  it('clears the intent when the guest closes the window', async () => {
    const user = userEvent.setup();
    useUIStore.getState().openAuthModal('login', {
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-1',
    });
    renderModal();

    await user.click(screen.getByRole('button', { name: 'Закрыть' }));

    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(useUIStore.getState().authIntent).toBeNull();
  });
});
