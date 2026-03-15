import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AUTH_TOKEN_KEY,
  REFRESH_TOKEN_KEY,
  apiClient,
  applyTokens,
} from './client';

describe('client', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    delete apiClient.defaults.headers.common.Authorization;
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('constants', () => {
    it('AUTH_TOKEN_KEY is auth_token', () => {
      expect(AUTH_TOKEN_KEY).toBe('auth_token');
    });

    it('REFRESH_TOKEN_KEY is auth_refresh_token', () => {
      expect(REFRESH_TOKEN_KEY).toBe('auth_refresh_token');
    });
  });

  describe('applyTokens', () => {
    it('sets Authorization header with Bearer access token', () => {
      applyTokens({
        accessToken: 'access-xyz',
        refreshToken: 'refresh-abc',
      });

      expect(apiClient.defaults.headers.common.Authorization).toBe(
        'Bearer access-xyz',
      );
    });

    it('stores access token in localStorage', () => {
      applyTokens({
        accessToken: 'stored-access',
        refreshToken: 'stored-refresh',
      });

      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('stored-access');
    });

    it('stores refresh token in localStorage', () => {
      applyTokens({
        accessToken: 'at',
        refreshToken: 'rt-123',
      });

      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('rt-123');
    });

    it('overwrites existing tokens when called again', () => {
      applyTokens({
        accessToken: 'first-access',
        refreshToken: 'first-refresh',
      });
      applyTokens({
        accessToken: 'second-access',
        refreshToken: 'second-refresh',
      });

      expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('second-access');
      expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('second-refresh');
      expect(apiClient.defaults.headers.common.Authorization).toBe(
        'Bearer second-access',
      );
    });
  });
});
