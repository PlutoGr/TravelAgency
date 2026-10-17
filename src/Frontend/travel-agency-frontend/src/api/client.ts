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

function isAuthMe(url: string): boolean {
  return url.includes('/auth/me');
}

type SessionHandler = () => void;
let sessionHandler: SessionHandler | null = null;

/** App registers this so a failed refresh marks the visitor as a guest without a toast. */
export function setAuthSessionHandler(handler: SessionHandler | null): void {
  sessionHandler = handler;
}

function notifySessionInvalid(): void {
  sessionHandler?.();
}

let refreshPromise: Promise<void> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retried?: boolean;
    };
    const url = originalRequest?.url ?? '';
    const status = error.response?.status;

    if (url.includes('/auth/refresh')) {
      if (status === 401 || status === 502) notifySessionInvalid();
      return Promise.reject(error);
    }

    if (status !== 401) {
      return Promise.reject(error);
    }

    if (url.includes('/auth/login') || url.includes('/auth/register')) {
      return Promise.reject(error);
    }

    if (originalRequest?._retried) {
      if (!isAuthMe(url)) redirectToLogin();
      return Promise.reject(error);
    }

    if (!refreshPromise) {
      refreshPromise = (async () => {
        try {
          await apiClient.post('/auth/refresh', {});
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
