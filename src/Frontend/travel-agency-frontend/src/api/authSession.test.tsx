import { useEffect } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import toast from 'react-hot-toast';
import { apiClient, setAuthSessionHandler } from './client';
import { useAuthStore } from '@/store/authStore';
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
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    setAuthSessionHandler(() => {
      useAuthStore.getState().markSessionExpired();
    });
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
    setAuthSessionHandler(null);
  });

  it('refreshes once after an expired session and then loads the user', async () => {
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

  it('stays a guest after one failed refresh and does not retry', async () => {
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
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('opens login without a toast when startup refresh returns 502 on a protected page', async () => {
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

function Startup({ check }: { check: boolean }) {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  useEffect(() => {
    if (check) checkAuth();
  }, [check, checkAuth]);
  return null;
}
