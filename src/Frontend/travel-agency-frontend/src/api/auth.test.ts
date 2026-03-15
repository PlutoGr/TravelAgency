import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as auth from './auth';
import { AUTH_TOKEN_KEY, REFRESH_TOKEN_KEY } from './client';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    post: vi.fn(),
    get: vi.fn(),
    patch: vi.fn(),
    defaults: {
      headers: {
        common: {} as Record<string, string>,
      },
    },
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
  AUTH_TOKEN_KEY: 'auth_token',
  REFRESH_TOKEN_KEY: 'auth_refresh_token',
  applyTokens: (tokens: { accessToken: string; refreshToken: string }) => {
    mockApiClient.defaults.headers.common.Authorization = `Bearer ${tokens.accessToken}`;
    localStorage.setItem('auth_token', tokens.accessToken);
    localStorage.setItem('auth_refresh_token', tokens.refreshToken);
  },
}));

describe('auth API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    mockApiClient.defaults.headers.common = {};
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('login', () => {
    it('posts to /auth/login, stores tokens, sets Authorization, fetches user, returns user and tokens', async () => {
      const tokensDto = {
        accessToken: 'access-123',
        refreshToken: 'refresh-456',
        expiresAt: '2025-12-31T00:00:00Z',
      };
      const userDto = {
        id: 'u1',
        email: 'a@b.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: null,
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
      };

      mockApiClient.post.mockResolvedValue({ data: tokensDto });
      mockApiClient.get.mockResolvedValue({ data: userDto });

      const result = await auth.login({ email: 'a@b.com', password: 'secret' });

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/login', {
        email: 'a@b.com',
        password: 'secret',
      });
      expect(mockApiClient.get).toHaveBeenCalledWith('/auth/me');
      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('access-123');
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-456');
      expect(mockApiClient.defaults.headers.common.Authorization).toBe(
        'Bearer access-123',
      );
      expect(result.user).toEqual({
        id: 'u1',
        email: 'a@b.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: '',
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
      });
      expect(result.tokens).toEqual({
        accessToken: 'access-123',
        refreshToken: 'refresh-456',
      });
    });

    it('throws when login API fails', async () => {
      mockApiClient.post.mockRejectedValue(new Error('Invalid credentials'));

      await expect(
        auth.login({ email: 'bad@b.com', password: 'wrong' }),
      ).rejects.toThrow('Invalid credentials');
    });
  });

  describe('register', () => {
    it('posts to /auth/register, stores tokens, fetches user, returns user and tokens', async () => {
      const tokensDto = {
        accessToken: 'access-new',
        refreshToken: 'refresh-new',
        expiresAt: '2025-12-31T00:00:00Z',
      };
      const userDto = {
        id: 'u2',
        email: 'new@b.com',
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+123',
        role: 'client',
        createdAt: '2025-01-02T00:00:00Z',
      };

      mockApiClient.post.mockResolvedValueOnce({ data: tokensDto });
      mockApiClient.get.mockResolvedValue({ data: userDto });

      const result = await auth.register({
        email: 'new@b.com',
        password: 'pass',
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+123',
      });

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/register', {
        email: 'new@b.com',
        password: 'pass',
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+123',
      });
      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('access-new');
      expect(result.user.phone).toBe('+123');
      expect(result.tokens.accessToken).toBe('access-new');
    });
  });

  describe('logout', () => {
    it('posts to /auth/logout when refresh token exists, clears storage and Authorization', async () => {
      localStorage.setItem(AUTH_TOKEN_KEY, 'access');
      localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh');
      mockApiClient.defaults.headers.common.Authorization = 'Bearer access';
      mockApiClient.post.mockResolvedValue({});

      await auth.logout();

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/logout', {
        refreshToken: 'refresh',
      });
      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
      expect(mockApiClient.defaults.headers.common.Authorization).toBeUndefined();
    });

    it('clears storage and Authorization even when no refresh token', async () => {
      localStorage.setItem(AUTH_TOKEN_KEY, 'access');
      mockApiClient.defaults.headers.common.Authorization = 'Bearer access';

      await auth.logout();

      expect(mockApiClient.post).not.toHaveBeenCalled();
      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
      expect(mockApiClient.defaults.headers.common.Authorization).toBeUndefined();
    });

    it('clears storage even when logout API fails', async () => {
      localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh');
      mockApiClient.post.mockRejectedValue(new Error('Network error'));

      await expect(auth.logout()).rejects.toThrow('Network error');

      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
    });
  });

  describe('getMe', () => {
    it('fetches /auth/me and maps DTO to User', async () => {
      const userDto = {
        id: 'u3',
        email: 'me@b.com',
        firstName: 'Bob',
        lastName: 'Lee',
        phone: null,
        role: 'admin',
        createdAt: '2025-01-03T00:00:00Z',
      };
      mockApiClient.get.mockResolvedValue({ data: userDto });

      const result = await auth.getMe();

      expect(mockApiClient.get).toHaveBeenCalledWith('/auth/me');
      expect(result).toEqual({
        id: 'u3',
        email: 'me@b.com',
        firstName: 'Bob',
        lastName: 'Lee',
        phone: '',
        role: 'admin',
        createdAt: '2025-01-03T00:00:00Z',
      });
    });

    it('maps null phone to empty string', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          id: 'u4',
          email: 'x@b.com',
          firstName: 'X',
          lastName: 'Y',
          phone: null,
          role: 'client',
          createdAt: '2025-01-01T00:00:00Z',
        },
      });

      const result = await auth.getMe();
      expect(result.phone).toBe('');
    });
  });

  describe('updateProfile', () => {
    it('patches /auth/me with provided fields and returns mapped User', async () => {
      const userDto = {
        id: 'u5',
        email: 'upd@b.com',
        firstName: 'Updated',
        lastName: 'Name',
        phone: '+999',
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
      };
      mockApiClient.patch.mockResolvedValue({ data: userDto });

      const result = await auth.updateProfile({
        firstName: 'Updated',
        lastName: 'Name',
        phone: '+999',
      });

      expect(mockApiClient.patch).toHaveBeenCalledWith('/auth/me', {
        firstName: 'Updated',
        lastName: 'Name',
        phone: '+999',
      });
      expect(result.firstName).toBe('Updated');
      expect(result.lastName).toBe('Name');
      expect(result.phone).toBe('+999');
    });

    it('sends only defined fields in payload', async () => {
      mockApiClient.patch.mockResolvedValue({
        data: {
          id: 'u6',
          email: 'e@b.com',
          firstName: 'OnlyFirst',
          lastName: 'Doe',
          phone: null,
          role: 'client',
          createdAt: '2025-01-01T00:00:00Z',
        },
      });

      await auth.updateProfile({ firstName: 'OnlyFirst' });

      expect(mockApiClient.patch).toHaveBeenCalledWith('/auth/me', {
        firstName: 'OnlyFirst',
      });
    });
  });

  describe('refresh', () => {
    it('posts to /auth/refresh, stores new tokens, sets Authorization', async () => {
      localStorage.setItem(REFRESH_TOKEN_KEY, 'old-refresh');
      const tokensDto = {
        accessToken: 'new-access',
        refreshToken: 'new-refresh',
        expiresAt: '2025-12-31T00:00:00Z',
      };
      mockApiClient.post.mockResolvedValue({ data: tokensDto });

      const result = await auth.refresh();

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/refresh', {
        refreshToken: 'old-refresh',
      });
      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('new-access');
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('new-refresh');
      expect(mockApiClient.defaults.headers.common.Authorization).toBe(
        'Bearer new-access',
      );
      expect(result).toEqual({
        accessToken: 'new-access',
        refreshToken: 'new-refresh',
      });
    });

    it('throws when no refresh token in storage', async () => {
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();

      await expect(auth.refresh()).rejects.toThrow('No refresh token available');
      expect(mockApiClient.post).not.toHaveBeenCalled();
    });

    it('maps AuthTokensDto to AuthTokens (drops expiresAt)', async () => {
      localStorage.setItem(REFRESH_TOKEN_KEY, 'old-refresh');
      const tokensDto = {
        accessToken: 'new-access',
        refreshToken: 'new-refresh',
        expiresAt: '2025-12-31T00:00:00Z',
      };
      mockApiClient.post.mockResolvedValue({ data: tokensDto });

      const result = await auth.refresh();

      expect(result).toEqual({
        accessToken: 'new-access',
        refreshToken: 'new-refresh',
      });
      expect(result).not.toHaveProperty('expiresAt');
    });
  });
});
