import axios from 'axios';
import type { InternalAxiosRequestConfig } from 'axios';
import type { AuthTokens } from '@/types';
import { mapAuthTokensDtoToAuthTokens, type AuthTokensDto } from './dto';

/**
 * localStorage key for access token (used in Authorization header).
 * SECURITY: Tokens in localStorage are vulnerable to XSS. Migration to httpOnly cookies is planned.
 */
export const AUTH_TOKEN_KEY = 'auth_token';

/**
 * localStorage key for refresh token (used for token refresh in FE-008).
 * SECURITY: Tokens in localStorage are vulnerable to XSS. Migration to httpOnly cookies is planned.
 */
export const REFRESH_TOKEN_KEY = 'auth_refresh_token';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Stores tokens in memory and localStorage. Exported for auth.ts and refresh interceptor.
 * SECURITY: localStorage is XSS-vulnerable. Migration to httpOnly cookies is planned (requires backend).
 */
export function applyTokens(tokens: AuthTokens): void {
  apiClient.defaults.headers.common.Authorization = `Bearer ${tokens.accessToken}`;
  localStorage.setItem(AUTH_TOKEN_KEY, tokens.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
}

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

function clearTokensAndRedirect(): void {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  delete apiClient.defaults.headers.common.Authorization;
  window.location.href = '/?auth=login';
}

let refreshPromise: Promise<void> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retried?: boolean;
    };

    if (error.response?.status !== 401) {
      return Promise.reject(error);
    }

    if (originalRequest?.url?.includes('/auth/refresh')) {
      clearTokensAndRedirect();
      return Promise.reject(error);
    }

    if (originalRequest?._retried) {
      clearTokensAndRedirect();
      return Promise.reject(error);
    }

    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) {
      clearTokensAndRedirect();
      return Promise.reject(error);
    }

    if (!refreshPromise) {
      refreshPromise = (async () => {
        try {
          const { data } = await apiClient.post<AuthTokensDto>('/auth/refresh', {
            refreshToken,
          });
          applyTokens(mapAuthTokensDtoToAuthTokens(data));
        } catch (e) {
          clearTokensAndRedirect();
          throw e;
        } finally {
          refreshPromise = null;
        }
      })();
    }

    try {
      await refreshPromise;
      const newToken = localStorage.getItem(AUTH_TOKEN_KEY);
      originalRequest.headers.Authorization = `Bearer ${newToken}`;
      originalRequest._retried = true;
      return apiClient(originalRequest);
    } catch (e) {
      return Promise.reject(e);
    }
  },
);
