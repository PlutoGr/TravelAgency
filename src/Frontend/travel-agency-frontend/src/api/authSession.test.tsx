import { useEffect } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import toast from 'react-hot-toast';
import { apiClient, setAuthSessionHandler } from './client';
import {
  clearSessionSignal,
  hasSessionSignal,
  markSessionPresent,
} from './sessionSignal';
import { afterSessionEstablished } from '@/store/authIntent';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';
import ProtectedRoute from '@/components/layout/ProtectedRoute';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

const profile = {
  id: 'u1',
  email: 'a@b.com',
  firstName: 'John',
  lastName: 'Doe',
  phone: '',
  role: 'Client',
  createdAt: '2025-01-01T00:00:00Z',
};

function httpResponse(
  config: InternalAxiosRequestConfig,
  status: number,
  data: unknown,
): AxiosResponse {
  return { data, status, statusText: String(status), headers: {}, config };
}

function httpError(config: InternalAxiosRequestConfig, status: number): AxiosError {
  return new AxiosError(
    'request failed',
    String(status),
    config,
    null,
    httpResponse(config, status, {}),
  );
}

describe('auth session refresh', () => {
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    vi.clearAllMocks();
    clearSessionSignal();
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    setAuthSessionHandler(() => {
      useAuthStore.getState().markSessionExpired();
    });
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
    clearSessionSignal();
    setAuthSessionHandler(null);
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: null,
      isMobileMenuOpen: false,
    });
    useFavoritesStore.setState({
      favoriteIds: [],
      favoriteTours: [],
      isLoading: false,
      error: null,
      count: 0,
    });
  });

  it('refreshes once after an expired session and then loads the user', async () => {
    markSessionPresent();
    let meCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes('/auth/me')) {
        meCalls += 1;
        if (meCalls === 1) throw httpError(config, 401);
        return httpResponse(config, 200, profile);
      }
      throw httpError(config, 404);
    };

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });

    expect(useAuthStore.getState().user?.email).toBe('a@b.com');
    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('does not refresh or retry when login or register returns 401', async () => {
    for (const path of ['/auth/login', '/auth/register']) {
      let calls = 0;
      let refreshCalls = 0;
      apiClient.defaults.adapter = async (config) => {
        const url = config.url ?? '';
        if (url.includes('/auth/refresh')) {
          refreshCalls += 1;
          return httpResponse(config, 200, { success: true });
        }
        if (url.includes(path)) {
          calls += 1;
          throw httpError(config, 401);
        }
        throw httpError(config, 404);
      };

      await expect(apiClient.post(path, { email: 'a@b.com', password: 'x' })).rejects.toBeTruthy();
      expect(calls).toBe(1);
      expect(refreshCalls).toBe(0);
      expect(toast.error).not.toHaveBeenCalled();
    }
  });

  it('does not refresh when auth/me returns 401 without a session signal', async () => {
    let meCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes('/auth/me')) {
        meCalls += 1;
        throw httpError(config, 401);
      }
      throw httpError(config, 404);
    };

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
    expect(refreshCalls).toBe(0);
    expect(meCalls).toBe(1);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('refreshes once when a session signal is present and retries the original request', async () => {
    markSessionPresent();
    let meCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes('/auth/me')) {
        meCalls += 1;
        if (meCalls === 1) throw httpError(config, 401);
        return httpResponse(config, 200, profile);
      }
      throw httpError(config, 404);
    };

    await expect(apiClient.get('/auth/me')).resolves.toMatchObject({
      status: 200,
      data: profile,
    });

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
    expect(hasSessionSignal()).toBe(true);
  });

  it('stays a guest after one failed refresh and does not retry', async () => {
    markSessionPresent();
    let meCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        throw httpError(config, 401);
      }
      if (url.includes('/auth/me')) {
        meCalls += 1;
        throw httpError(config, 401);
      }
      throw httpError(config, 404);
    };

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(1);
    expect(hasSessionSignal()).toBe(false);
    expect(toast.error).not.toHaveBeenCalled();

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
  });

  it('clears the session signal when refresh returns 400 and does not call it again', async () => {
    markSessionPresent();
    let refreshCalls = 0;
    let meCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        throw httpError(config, 400);
      }
      if (url.includes('/auth/me')) {
        meCalls += 1;
        throw httpError(config, 401);
      }
      throw httpError(config, 404);
    };

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(1);
    expect(hasSessionSignal()).toBe(false);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(toast.error).not.toHaveBeenCalled();

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
  });

  it('opens login without a toast when startup refresh returns 502 on a protected page', async () => {
    markSessionPresent();
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        throw httpError(config, 502);
      }
      if (url.includes('/auth/me')) throw httpError(config, 401);
      throw httpError(config, 404);
    };

    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });
    render(protectedTree('/dashboard'));

    await waitFor(() => {
      expect(screen.getByTestId('loc').textContent).toContain('auth=login');
    });

    expect(refreshCalls).toBe(1);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('opens login without a toast when a background refresh returns 502', async () => {
    markSessionPresent();
    let bookingCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        throw httpError(config, 502);
      }
      if (url.includes('/bookings')) {
        bookingCalls += 1;
        throw httpError(config, 401);
      }
      throw httpError(config, 404);
    };

    useAuthStore.setState({
      user: {
        id: 'u1',
        email: 'a@b.com',
        firstName: 'John',
        lastName: 'Doe',
        phone: '',
        role: 'client',
        createdAt: '2025-01-01T00:00:00Z',
      },
      isAuthenticated: true,
      isLoading: false,
    });

    render(protectedTree('/dashboard', true));

    await waitFor(() => {
      expect(screen.getByTestId('loc').textContent).toContain('auth=login');
    });

    expect(refreshCalls).toBe(1);
    expect(bookingCalls).toBe(1);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('adds a pending favorite after startup refresh and closes the login window', async () => {
    markSessionPresent();
    let meCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) return httpResponse(config, 200, { success: true });
      if (url.includes('/auth/me')) {
        meCalls += 1;
        if (meCalls === 1) throw httpError(config, 401);
        return httpResponse(config, 200, profile);
      }
      if (url.includes('/favorites')) return httpResponse(config, 200, []);
      throw httpError(config, 404);
    };

    useUIStore.setState({
      isAuthModalOpen: true,
      authModalTab: 'login',
      authIntent: { type: 'favorite', tourId: 'tour-1' },
    });
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });

    render(<SessionIntentBridge />);

    await waitFor(() => {
      expect(useFavoritesStore.getState().favoriteIds).toContain('tour-1');
    });

    expect(meCalls).toBe(2);
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(useUIStore.getState().authIntent).toBeNull();
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('keeps the login window and the intent when startup refresh returns 502', async () => {
    markSessionPresent();
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) throw httpError(config, 502);
      if (url.includes('/auth/me')) throw httpError(config, 401);
      throw httpError(config, 404);
    };

    useUIStore.setState({
      isAuthModalOpen: true,
      authModalTab: 'login',
      authIntent: { type: 'request', tourId: 'tour-1', offerId: 'offer-1' },
    });
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: true });

    render(<SessionIntentBridge />);

    await waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useUIStore.getState().isAuthModalOpen).toBe(true);
    expect(useUIStore.getState().authIntent).toEqual({
      type: 'request',
      tourId: 'tour-1',
      offerId: 'offer-1',
    });
    expect(useFavoritesStore.getState().favoriteIds).toEqual([]);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('remembers a session after login or register and forgets it after logout', async () => {
    apiClient.defaults.adapter = async (config) =>
      httpResponse(config, 200, { success: true });

    for (const path of ['/auth/login', '/auth/register']) {
      clearSessionSignal();
      await apiClient.post(path, {});
      expect(hasSessionSignal()).toBe(true);
    }

    await apiClient.post('/auth/logout', {});
    expect(hasSessionSignal()).toBe(false);
  });

  it('does not refresh again after logout', async () => {
    markSessionPresent();
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/logout')) return httpResponse(config, 204, null);
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes('/auth/me')) throw httpError(config, 401);
      throw httpError(config, 404);
    };

    await useAuthStore.getState().logout();
    expect(hasSessionSignal()).toBe(false);

    useAuthStore.getState().checkAuth();

    await vi.waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
    });

    expect(refreshCalls).toBe(0);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="loc">{`${location.pathname}${location.search}`}</div>;
}

function BackgroundCall() {
  useEffect(() => {
    apiClient.get('/bookings').catch(() => undefined);
  }, []);
  return <div>Кабинет</div>;
}

function protectedTree(path: string, background = false) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <Startup check={!background} />
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="dashboard" element={background ? <BackgroundCall /> : <div>Кабинет</div>} />
        </Route>
        <Route path="/" element={<div>Главная</div>} />
      </Routes>
    </MemoryRouter>
  );
}

function SessionIntentBridge() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const checkAuth = useAuthStore((s) => s.checkAuth);
  useEffect(() => {
    checkAuth();
  }, [checkAuth]);
  useEffect(() => {
    if (!isAuthenticated) return;
    let cancelled = false;
    void afterSessionEstablished(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [isAuthenticated]);
  return null;
}

function Startup({ check }: { check: boolean }) {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  useEffect(() => {
    if (check) checkAuth();
  }, [check, checkAuth]);
  return null;
}
