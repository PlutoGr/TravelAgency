import { act, useEffect, type ReactNode } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { User } from '@/types';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';
import AuthModal from './AuthModal';
import AuthRedirectHandler from './AuthRedirectHandler';
import DashboardLayout from './DashboardLayout';
import ProtectedRoute from './ProtectedRoute';
import FavoritesPage from '@/pages/FavoritesPage';
import MyBookingsPage from '@/pages/MyBookingsPage';

// React 19 only flushes act() when the test runtime opts in.
(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const { mockGetMe, mockLogin } = vi.hoisted(() => ({
  mockGetMe: vi.fn(),
  mockLogin: vi.fn(),
}));

vi.mock('@/api/auth', () => ({
  login: (...args: unknown[]) => mockLogin(...args),
  register: vi.fn(),
  logout: vi.fn(),
  getMe: (...args: unknown[]) => mockGetMe(...args),
  updateProfile: vi.fn(),
}));

vi.mock('@/api/favorites', () => ({
  getFavorites: vi.fn(() => Promise.resolve([])),
  addFavorite: vi.fn(() => Promise.resolve()),
  removeFavorite: vi.fn(() => Promise.resolve()),
}));

vi.mock('@/api/bookings', () => ({
  getMyBookings: vi.fn(() => Promise.resolve([])),
  createBooking: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

const clientUser: User = {
  id: 'client-1',
  email: 'client@test.com',
  firstName: 'Анна',
  lastName: 'Клиент',
  phone: '',
  role: 'client',
  createdAt: '2025-01-01T00:00:00Z',
};

/** Snapshot of the store as the module created it, before any test mutates it. */
const initialAuthLoading = useAuthStore.getState().isLoading;

const seenLocations: string[] = [];

function LocationProbe() {
  const location = useLocation();
  const href = `${location.pathname}${location.search}`;
  seenLocations.push(href);
  return <div data-testid="location">{href}</div>;
}

function AuthBootstrap({ children }: { children: ReactNode }) {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  useEffect(() => {
    checkAuth();
  }, [checkAuth]);
  return children;
}

function Harness({ path }: { path: string }) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <AuthBootstrap>
        <LocationProbe />
        <AuthRedirectHandler />
        <Routes>
          <Route path="/" element={<h1>Главная страница</h1>} />
          <Route element={<ProtectedRoute allowedRoles={['client', 'manager', 'admin']} />}>
            <Route element={<DashboardLayout />}>
              <Route path="dashboard/favorites" element={<FavoritesPage />} />
              <Route path="dashboard/bookings" element={<MyBookingsPage />} />
            </Route>
          </Route>
          <Route element={<ProtectedRoute allowedRoles={['manager', 'admin']} />}>
            <Route path="manager" element={<h1>Кабинет менеджера</h1>} />
          </Route>
        </Routes>
        <AuthModal />
      </AuthBootstrap>
    </MemoryRouter>
  );
}

let container: HTMLDivElement;
let root: Root;
let resolveMe: (user: User) => void;
let rejectMe: (error: unknown) => void;

function locationText(): string {
  return document.querySelector('[data-testid="location"]')?.textContent ?? '';
}

function deferMe() {
  mockGetMe.mockImplementation(
    () =>
      new Promise<User>((resolve, reject) => {
        resolveMe = resolve;
        rejectMe = reject;
      }),
  );
}

function resetStores() {
  useAuthStore.setState({
    user: null,
    isAuthenticated: false,
    isLoading: initialAuthLoading,
  });
  useUIStore.setState({
    isAuthModalOpen: false,
    authModalTab: 'login',
    isMobileMenuOpen: false,
  });
  useFavoritesStore.setState({
    favoriteIds: [],
    favoriteTours: [],
    isLoading: false,
    count: 0,
  });
}

async function renderAt(path: string) {
  await act(async () => {
    root.render(<Harness path={path} />);
  });
}

function setNativeValue(input: HTMLInputElement, value: string) {
  const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set;
  setter?.call(input, value);
  input.dispatchEvent(new Event('input', { bubbles: true }));
}

async function submitLogin() {
  await act(async () => {
    const email = document.getElementById('email');
    const password = document.getElementById('пароль');
    if (!(email instanceof HTMLInputElement) || !(password instanceof HTMLInputElement)) {
      throw new Error('login form is not open');
    }
    setNativeValue(email, 'client@test.com');
    setNativeValue(password, 'secret');
  });
  const form = document.querySelector('form');
  if (!form) throw new Error('login form is not open');
  await act(async () => {
    form.requestSubmit();
  });
}

describe('dashboard direct entry', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    seenLocations.length = 0;
    resetStores();
    deferMe();
    mockLogin.mockResolvedValue({ user: clientUser });

    window.matchMedia = vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));

    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
  });

  afterEach(() => {
    act(() => {
      root.unmount();
    });
    container.remove();
    document.body.innerHTML = '';
  });

  it('keeps the first auth/me check pending before checkAuth runs', () => {
    expect(initialAuthLoading).toBe(true);
  });

  it.each([
    ['/dashboard/favorites', 'Избранное'],
    ['/dashboard/bookings', 'Мои бронирования'],
  ])(
    'logged-in client direct load of %s shows loading, then the page, and does not go home',
    async (path, heading) => {
      await renderAt(path);

      expect(locationText()).toBe(path);
      expect(document.querySelector('[role="status"]')).not.toBeNull();
      expect(document.body.textContent).not.toContain(heading);
      expect(seenLocations.every((href) => href === path)).toBe(true);

      await act(async () => {
        resolveMe(clientUser);
      });

      await vi.waitFor(() => {
        expect(document.body.textContent).toContain(heading);
      });

      expect(locationText()).toBe(path);
      expect(document.querySelector('[role="status"]')).toBeNull();
      expect(seenLocations.every((href) => href === path)).toBe(true);
      expect(document.body.textContent).not.toContain('Главная страница');
    },
  );

  it.each(['/dashboard/favorites', '/dashboard/bookings'])(
    'guest direct load of %s opens login with returnTo and lands there after login',
    async (path) => {
      const heading = path.endsWith('favorites') ? 'Избранное' : 'Мои бронирования';
      await renderAt(path);

      expect(locationText()).toBe(path);
      expect(document.querySelector('[role="status"]')).not.toBeNull();

      await act(async () => {
        rejectMe({ response: { status: 401 } });
      });

      const returnTo = encodeURIComponent(path);
      await vi.waitFor(() => {
        expect(seenLocations).toContain(`/?auth=login&returnTo=${returnTo}`);
        expect(locationText()).toBe(`/?returnTo=${returnTo}`);
      });

      expect(document.body.textContent).toContain('Войти');
      expect(document.body.textContent).not.toContain(heading);

      await act(async () => {
        useUIStore.getState().openAuthModal('login');
      });
      expect(locationText()).toBe(`/?returnTo=${returnTo}`);

      await submitLogin();

      await vi.waitFor(() => {
        expect(locationText()).toBe(path);
        expect(document.body.textContent).toContain(heading);
      });
    },
  );

  it('guest stays on the requested page when the login tab is activated again after redirect', async () => {
    await renderAt('/dashboard/bookings');

    await act(async () => {
      rejectMe({ response: { status: 401 } });
    });

    await vi.waitFor(() => {
      expect(locationText()).toBe('/?returnTo=%2Fdashboard%2Fbookings');
      expect(useUIStore.getState().isAuthModalOpen).toBe(true);
    });

    const loginTab = [...document.querySelectorAll('button')].find(
      (button) => button.textContent === 'Вход',
    );
    if (!(loginTab instanceof HTMLButtonElement)) throw new Error('login tab is not open');
    await act(async () => {
      loginTab.click();
    });

    await submitLogin();

    await vi.waitFor(() => {
      expect(locationText()).toBe('/dashboard/bookings');
      expect(document.body.textContent).toContain('Мои бронирования');
    });
  });

  it('direct /?auth=login&returnTo=/dashboard/favorites returns there after login', async () => {
    const returnTo = encodeURIComponent('/dashboard/favorites');
    await renderAt(`/?auth=login&returnTo=${returnTo}`);

    await act(async () => {
      rejectMe({ response: { status: 401 } });
    });

    await vi.waitFor(() => {
      expect(seenLocations).toContain(`/?auth=login&returnTo=${returnTo}`);
      expect(locationText()).toBe(`/?returnTo=${returnTo}`);
      expect(document.body.textContent).toContain('Войти');
    });

    await submitLogin();

    await vi.waitFor(() => {
      expect(locationText()).toBe('/dashboard/favorites');
      expect(document.body.textContent).toContain('Избранное');
    });
  });

  it.each(['//evil.com', '/\\evil.com', 'https://evil.com', 'javascript:alert(1)'])(
    'rejects returnTo %s and stays on the home page after login',
    async (unsafe) => {
      await renderAt(`/?auth=login&returnTo=${encodeURIComponent(unsafe)}`);

      await act(async () => {
        rejectMe({ response: { status: 401 } });
      });

      await vi.waitFor(() => {
        expect(useUIStore.getState().isAuthModalOpen).toBe(true);
        const params = new URLSearchParams(locationText().split('?')[1] ?? '');
        expect(params.get('auth')).toBeNull();
        expect(params.get('returnTo')).toBe(unsafe);
      });

      expect(seenLocations.every((href) => href.split('?')[0] === '/')).toBe(true);

      await submitLogin();

      await vi.waitFor(() => {
        expect(useAuthStore.getState().isAuthenticated).toBe(true);
      });

      expect(locationText()).toBe('/');
      expect(document.body.textContent).toContain('Главная страница');
      expect(window.location.href).not.toContain('evil.com');
      expect(window.location.href).not.toContain('javascript:');
    },
  );

  it('sends a logged-in client away from a manager route only after auth/me resolves', async () => {
    await renderAt('/manager');

    expect(locationText()).toBe('/manager');
    expect(document.querySelector('[role="status"]')).not.toBeNull();
    expect(document.body.textContent).not.toContain('Главная страница');

    await act(async () => {
      resolveMe(clientUser);
    });

    await vi.waitFor(() => {
      expect(locationText()).toBe('/');
    });

    expect(document.body.textContent).toContain('Главная страница');
    expect(document.body.textContent).not.toContain('Кабинет менеджера');
  });
});
