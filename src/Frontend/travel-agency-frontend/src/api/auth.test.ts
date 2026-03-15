import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as auth from './auth';

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
}));

describe('auth API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiClient.defaults.headers.common = {};
  });

  describe('login', () => {
    it('posts to /auth/login, fetches user via getMe, returns user', async () => {
      const userDto = {
        id: 'u1',
        email: 'a@b.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: null,
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
      };

      mockApiClient.post.mockResolvedValue({ data: { success: true } });
      mockApiClient.get.mockResolvedValue({ data: userDto });

      const result = await auth.login({ email: 'a@b.com', password: 'secret' });

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/login', {
        email: 'a@b.com',
        password: 'secret',
      });
      expect(mockApiClient.get).toHaveBeenCalledWith('/auth/me');
      expect(result.user).toEqual({
        id: 'u1',
        email: 'a@b.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: '',
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
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
    it('posts to /auth/register, fetches user via getMe, returns user', async () => {
      const userDto = {
        id: 'u2',
        email: 'new@b.com',
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+123',
        role: 'client',
        createdAt: '2025-01-02T00:00:00Z',
      };

      mockApiClient.post.mockResolvedValueOnce({ data: { success: true } });
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
      expect(result.user.phone).toBe('+123');
    });
  });

  describe('logout', () => {
    it('posts to /auth/logout with empty body (credentials sent via cookies)', async () => {
      mockApiClient.post.mockResolvedValue({});

      await auth.logout();

      expect(mockApiClient.post).toHaveBeenCalledWith('/auth/logout', {});
    });

    it('clears state even when logout API fails', async () => {
      mockApiClient.post.mockRejectedValue(new Error('Network error'));

      await expect(auth.logout()).rejects.toThrow('Network error');
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
});
