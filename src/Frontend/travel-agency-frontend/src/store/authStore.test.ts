import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useAuthStore } from './authStore';
import type { User } from '@/types';

const { mockLogin, mockRegister, mockLogout, mockGetMe } = vi.hoisted(() => ({
  mockLogin: vi.fn(),
  mockRegister: vi.fn(),
  mockLogout: vi.fn(),
  mockGetMe: vi.fn(),
}));

vi.mock('@/api/auth', () => ({
  login: (...args: unknown[]) => mockLogin(...args),
  register: (...args: unknown[]) => mockRegister(...args),
  logout: (...args: unknown[]) => mockLogout(...args),
  getMe: (...args: unknown[]) => mockGetMe(...args),
  updateProfile: vi.fn(),
}));

const sampleUser: User = {
  id: 'u1',
  email: 'a@b.com',
  firstName: 'John',
  lastName: 'Doe',
  phone: '',
  role: 'client',
  createdAt: '2025-01-01T00:00:00Z',
};

function resetStore() {
  useAuthStore.setState({
    user: null,
    isAuthenticated: false,
    isLoading: false,
  });
}

describe('authStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    resetStore();
  });

  afterEach(() => {
    resetStore();
  });

  describe('login', () => {
    it('calls authApi.login, sets user and isAuthenticated on success', async () => {
      mockLogin.mockResolvedValue({ user: sampleUser });

      await useAuthStore.getState().login('a@b.com', 'secret');

      expect(mockLogin).toHaveBeenCalledWith({ email: 'a@b.com', password: 'secret' });
      expect(useAuthStore.getState().user).toEqual(sampleUser);
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });

    it('sets isLoading during request, clears on completion', async () => {
      let resolveLogin: (value: unknown) => void;
      mockLogin.mockImplementation(
        () =>
          new Promise((resolve) => {
            resolveLogin = resolve;
          }),
      );

      const loginPromise = useAuthStore.getState().login('a@b.com', 'pass');
      expect(useAuthStore.getState().isLoading).toBe(true);

      resolveLogin!({ user: sampleUser });
      await loginPromise;

      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    it('clears isLoading when login fails', async () => {
      mockLogin.mockRejectedValue(new Error('Invalid'));

      await expect(
        useAuthStore.getState().login('bad@b.com', 'wrong'),
      ).rejects.toThrow('Invalid');
      expect(useAuthStore.getState().isLoading).toBe(false);
    });
  });

  describe('register', () => {
    it('calls authApi.register, sets user and isAuthenticated on success', async () => {
      mockRegister.mockResolvedValue({ user: sampleUser });

      await useAuthStore.getState().register({
        email: 'new@b.com',
        password: 'pass',
        firstName: 'Jane',
        lastName: 'Smith',
      });

      expect(mockRegister).toHaveBeenCalledWith({
        email: 'new@b.com',
        password: 'pass',
        firstName: 'Jane',
        lastName: 'Smith',
      });
      expect(useAuthStore.getState().user).toEqual(sampleUser);
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });
  });

  describe('logout', () => {
    it('calls authApi.logout and clears user and isAuthenticated', async () => {
      useAuthStore.setState({
        user: sampleUser,
        isAuthenticated: true,
      });
      mockLogout.mockResolvedValue(undefined);

      await useAuthStore.getState().logout();

      expect(mockLogout).toHaveBeenCalled();
      expect(useAuthStore.getState().user).toBeNull();
      expect(useAuthStore.getState().isAuthenticated).toBe(false);
    });

    it('clears state even when authApi.logout throws', async () => {
      useAuthStore.setState({ user: sampleUser, isAuthenticated: true });
      mockLogout.mockRejectedValue(new Error('Network error'));

      await expect(useAuthStore.getState().logout()).rejects.toThrow('Network error');

      expect(useAuthStore.getState().user).toBeNull();
      expect(useAuthStore.getState().isAuthenticated).toBe(false);
    });
  });

  describe('setUser', () => {
    it('updates user in state', () => {
      useAuthStore.getState().setUser(sampleUser);
      expect(useAuthStore.getState().user).toEqual(sampleUser);
    });
  });

  describe('checkAuth', () => {
    it('always calls getMe to determine auth state', async () => {
      mockGetMe.mockResolvedValue(sampleUser);

      useAuthStore.getState().checkAuth();

      expect(useAuthStore.getState().isLoading).toBe(true);
      expect(mockGetMe).toHaveBeenCalled();

      await vi.waitFor(() => !useAuthStore.getState().isLoading, { timeout: 500 });

      expect(useAuthStore.getState().user).toEqual(sampleUser);
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    it('clears auth state on 401 (auth failure)', async () => {
      const err401 = { response: { status: 401 } };
      mockGetMe.mockImplementation(() => Promise.reject(err401));

      useAuthStore.getState().checkAuth();

      await vi.waitFor(() => mockGetMe.mock.calls.length > 0, { timeout: 500 });
      await new Promise((r) => setTimeout(r, 0));

      expect(useAuthStore.getState().user).toBeNull();
      expect(useAuthStore.getState().isAuthenticated).toBe(false);
    });

    it('does not logout on network/server errors, only clears isLoading', async () => {
      const err500 = { response: { status: 500 } };
      mockGetMe.mockImplementation(() => Promise.reject(err500));

      useAuthStore.getState().checkAuth();

      await vi.waitFor(() => mockGetMe.mock.calls.length > 0, { timeout: 500 });
      await new Promise((r) => setTimeout(r, 0));

      expect(mockLogout).not.toHaveBeenCalled();
      expect(useAuthStore.getState().isAuthenticated).toBe(false);
      expect(useAuthStore.getState().isLoading).toBe(false);
    });
  });
});
