import axios from 'axios';
import type { InternalAxiosRequestConfig } from 'axios';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

function redirectToLogin(): void {
  window.location.href = '/?auth=login';
}

function shouldSkipRedirect(request: InternalAxiosRequestConfig & { _retried?: boolean }): boolean {
  const url = request?.url ?? '';
  // getMe is used to check auth state; 401 is expected when not logged in — do not redirect
  return url.includes('/auth/me');
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
      redirectToLogin();
      return Promise.reject(error);
    }

    if (originalRequest?._retried) {
      if (!shouldSkipRedirect(originalRequest)) redirectToLogin();
      return Promise.reject(error);
    }

    if (shouldSkipRedirect(originalRequest)) {
      return Promise.reject(error);
    }

    if (!refreshPromise) {
      refreshPromise = (async () => {
        try {
          await apiClient.post('/auth/refresh', {});
        } catch {
          redirectToLogin();
          throw error;
        } finally {
          refreshPromise = null;
        }
      })();
    }

    try {
      await refreshPromise;
      originalRequest._retried = true;
      return apiClient(originalRequest);
    } catch (e) {
      return Promise.reject(e);
    }
  },
);
