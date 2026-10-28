import { createElement, type ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { AppRoutes } from '@/App';
import Header from '@/components/layout/Header';
import ProtectedRoute from '@/components/layout/ProtectedRoute';
import type { User } from '@/types';
import { ADMIN_ROUTE_DEFS, MANAGER_ROUTE_DEFS } from '@/routes/staffRoutes';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';

vi.mock('framer-motion', async () => {
  const actual = await vi.importActual<typeof import('framer-motion')>('framer-motion');
  return {
    ...actual,
    AnimatePresence: ({ children }: { children?: ReactNode }) => children ?? null,
  };
});

vi.mock('@/pages/HomePage', () => ({
  default: () => createElement('h1', null, 'Главная страница'),
}));

vi.mock('@/pages/ManagerDashboardPage', () => ({
  default: () => createElement('h1', null, 'Дашборд менеджера'),
}));

vi.mock('@/pages/ManagerBookingsPage', () => ({
  default: () => createElement('h1', null, 'Бронирования менеджера'),
}));

vi.mock('@/pages/ManagerBookingDetailPage', () => ({
  default: () => createElement('h1', null, 'Заявка менеджера'),
}));

vi.mock('@/pages/ManagerToursPage', () => ({
  default: () => createElement('h1', null, 'Туры менеджера'),
}));

vi.mock('@/pages/TourWizardPage', () => ({
  default: () => createElement('h1', null, 'Мастер тура'),
}));

vi.mock('@/pages/ManagerClientsPage', () => ({
  default: () => createElement('h1', null, 'Клиенты менеджера'),
}));

vi.mock('@/api/adminUsers', () => ({
  getAdminUsers: vi.fn().mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 20 }),
  getAdminUser: vi.fn(),
  changeAdminUserRole: vi.fn(),
  blockAdminUser: vi.fn(),
  unblockAdminUser: vi.fn(),
  adminUserErrorMessage: () => 'Не удалось выполнить действие',
}));

vi.mock('@/api/siteSettings', () => ({
  getAdminSiteSettings: vi.fn().mockResolvedValue({
    siteName: 'TravelAgency',
    supportPhone: null,
    contactEmail: null,
    officeAddress: null,
    workingHours: null,
    socialLinks: [],
    bannerText: null,
    bannerEnabled: false,
    updatedAt: '2026-10-26T00:00:00.000Z',
    updatedByUserId: null,
  }),
  getSiteSettingsAudit: vi.fn().mockResolvedValue([]),
  updateSiteSettings: vi.fn(),
  getPublicSiteSettings: vi.fn(),
  siteSettingsFieldErrors: vi.fn().mockReturnValue({}),
}));

const MANAGER_PAGE_TITLES: Record<string, string> = {
  '/manager': 'Дашборд менеджера',
  '/manager/bookings': 'Бронирования менеджера',
  '/manager/bookings/1': 'Заявка менеджера',
  '/manager/tours': 'Туры менеджера',
  '/manager/tours/new': 'Мастер тура',
  '/manager/tours/tour-1': 'Мастер тура',
  '/manager/clients': 'Клиенты менеджера',
};

function person(role: User['role']): User {
  return {
    id: `${role}-1`,
    email: `${role}@test.com`,
    firstName: 'Тест',
    lastName: 'Пользователь',
    phone: '',
    city: '',
    role,
    createdAt: '2025-01-01T00:00:00Z',
  };
}

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function renderAt(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <LocationProbe />
      <AppRoutes />
    </MemoryRouter>,
  );
}

function signIn(role: User['role'] | null) {
  useAuthStore.setState({
    user: role ? person(role) : null,
    isAuthenticated: role !== null,
    isLoading: false,
  });
}

describe('role access', () => {
  beforeEach(() => {
    signIn(null);
    useUIStore.setState({
      isAuthModalOpen: false,
      authModalTab: 'login',
      authIntent: null,
      isMobileMenuOpen: false,
    });
    useFavoritesStore.setState({ count: 0 });
  });

  it('covers every manager route from the shared list', () => {
    const urls = MANAGER_ROUTE_DEFS.map((route) => route.url);
    expect(new Set(urls)).toEqual(new Set(Object.keys(MANAGER_PAGE_TITLES)));
    expect(urls).toHaveLength(Object.keys(MANAGER_PAGE_TITLES).length);
  });

  it.each(MANAGER_ROUTE_DEFS.map((route) => route.url))(
    'admin opens manager route %s',
    async (url) => {
      signIn('admin');
      renderAt(url);

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1, name: MANAGER_PAGE_TITLES[url] })).toBeInTheDocument();
      });
      expect(screen.getByTestId('location')).toHaveTextContent(url);
      expect(screen.getByRole('link', { name: 'Администрирование' })).toBeInTheDocument();
    },
  );

  it.each(ADMIN_ROUTE_DEFS.map((route) => route.url))(
    'manager cannot open admin route %s',
    async (url) => {
      signIn('manager');
      renderAt(url);

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1, name: 'Главная страница' })).toBeInTheDocument();
      });
      expect(screen.getByTestId('location')).toHaveTextContent('/');
      expect(screen.queryByRole('link', { name: 'Администрирование' })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: 'Пользователи' })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: 'Настройки сайта' })).not.toBeInTheDocument();
    },
  );

  it('admin reaches a manager-only list and manager does not reach an admin-only list', async () => {
    function Guard({ url, allowed, title }: { url: string; allowed: 'manager' | 'admin'; title: string }) {
      return (
        <MemoryRouter initialEntries={[url]}>
          <Routes>
            <Route path="/" element={<h1>Главная страница</h1>} />
            <Route element={<ProtectedRoute allowedRoles={[allowed]} />}>
              <Route path={url.slice(1)} element={<h1>{title}</h1>} />
            </Route>
          </Routes>
        </MemoryRouter>
      );
    }

    signIn('admin');
    const adminView = render(<Guard url="/manager" allowed="manager" title="Кабинет менеджера" />);
    expect(screen.getByRole('heading', { name: 'Кабинет менеджера' })).toBeInTheDocument();
    adminView.unmount();

    signIn('manager');
    render(<Guard url="/admin" allowed="admin" title="Раздел админа" />);
    expect(screen.getByRole('heading', { name: 'Главная страница' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Раздел админа' })).not.toBeInTheDocument();
  });

  it('client cannot open a manager route or the admin section', async () => {
    signIn('client');
    const managerView = renderAt('/manager');

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Главная страница' })).toBeInTheDocument();
    });
    expect(screen.queryByRole('heading', { level: 1, name: 'Дашборд менеджера' })).not.toBeInTheDocument();

    managerView.unmount();
    renderAt('/admin');

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Главная страница' })).toBeInTheDocument();
    });
    expect(screen.queryByRole('heading', { level: 1, name: 'Администрирование' })).not.toBeInTheDocument();
  });

  it('admin sees the section shell and subsection placeholders', async () => {
    signIn('admin');
    const indexView = renderAt('/admin');

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Администрирование' })).toBeInTheDocument();
    });
    const sectionNav = screen.getByRole('navigation', { name: 'Подразделы администрирования' });
    expect(within(sectionNav).getByRole('link', { name: 'Пользователи' })).toHaveAttribute('href', '/admin/users');
    expect(within(sectionNav).getByRole('link', { name: 'Настройки сайта' })).toHaveAttribute(
      'href',
      '/admin/settings',
    );
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();

    indexView.unmount();
    const usersView = renderAt('/admin/users');
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Пользователи' })).toBeInTheDocument();
    });
    expect(screen.getByLabelText('Поиск')).toBeInTheDocument();
    expect(screen.queryByText('Раздел в разработке.')).not.toBeInTheDocument();

    usersView.unmount();
    renderAt('/admin/settings');
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Настройки сайта' })).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeInTheDocument();
    expect(screen.getByLabelText('Название сайта')).toBeInTheDocument();
    expect(screen.queryByText('Раздел в разработке.')).not.toBeInTheDocument();
  });

  it('guest opening /admin is sent to login and keeps the return path', async () => {
    signIn(null);
    renderAt('/admin');

    await waitFor(() => {
      expect(screen.getByTestId('location')).toHaveTextContent('/?auth=login&returnTo=%2Fadmin');
    });
    expect(screen.queryByRole('heading', { level: 1, name: 'Администрирование' })).not.toBeInTheDocument();
  });

  it('shows the admin section in the user menu only for admin, and the manager panel for manager and admin', async () => {
    const user = userEvent.setup();

    signIn('manager');
    const managerView = render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>,
    );
    await user.click(screen.getByRole('button', { name: 'ТП Тест' }));
    expect(screen.getByRole('button', { name: 'Панель менеджера' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Администрирование' })).not.toBeInTheDocument();
    managerView.unmount();

    signIn('admin');
    const adminView = render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>,
    );
    await user.click(screen.getByRole('button', { name: 'ТП Тест' }));
    expect(screen.getByRole('button', { name: 'Панель менеджера' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Администрирование' })).toBeInTheDocument();
    adminView.unmount();

    signIn('client');
    render(
      <MemoryRouter>
        <Header />
      </MemoryRouter>,
    );
    await user.click(screen.getByRole('button', { name: 'ТП Тест' }));
    expect(screen.queryByRole('button', { name: 'Панель менеджера' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Администрирование' })).not.toBeInTheDocument();
  });
});
