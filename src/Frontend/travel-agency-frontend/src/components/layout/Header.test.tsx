import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LayoutDashboard } from 'lucide-react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import * as authApi from '@/api/auth';
import Header from './Header';
import Footer from './Footer';
import Sidebar from './Sidebar';
import { EMPTY_SITE_SETTINGS } from '@/domain/siteSettings';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useSiteSettingsStore } from '@/store/siteSettingsStore';
import { useUIStore } from '@/store/uiStore';

// Exit-анимация меню держит пункты в DOM после закрытия. В тесте смотрим
// состояние меню, а не длительность transition.
vi.mock('framer-motion', async () => {
  const actual = await vi.importActual<typeof import('framer-motion')>('framer-motion');
  return {
    ...actual,
    AnimatePresence: ({ children }: { children?: ReactNode }) => children ?? null,
  };
});

function LocationProbe() {
  const location = useLocation();
  return (
    <div data-testid="location">{`${location.pathname}${location.hash}`}</div>
  );
}

function renderHeader(path = '/') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="*"
          element={
            <>
              <Header />
              <div id="reviews" />
              <div id="contacts" />
              <LocationProbe />
            </>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

function menuToggle(): HTMLElement {
  const icon = document.querySelector('button svg.lucide-menu');
  const button = icon?.closest('button');
  if (!(button instanceof HTMLElement)) {
    throw new Error('кнопка мобильного меню не найдена');
  }
  return button;
}

function locationText(): string {
  return screen.getByTestId('location').textContent ?? '';
}

describe('Header', () => {
  beforeEach(() => {
    useUIStore.setState({
      isMobileMenuOpen: false,
      isAuthModalOpen: false,
      authModalTab: 'login',
    });
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });
    useFavoritesStore.setState({ count: 0 });
    useSiteSettingsStore.setState({ settings: EMPTY_SITE_SETTINGS });
    window.history.pushState({}, '', '/');
    HTMLElement.prototype.scrollIntoView = vi.fn();
  });

  it('does not show directions and keeps tours, reviews and contacts', async () => {
    const user = userEvent.setup();
    renderHeader();

    expect(screen.queryByRole('button', { name: 'Направления' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Туры' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Отзывы' })).toHaveAttribute('href', '/#reviews');
    expect(screen.getByRole('link', { name: 'Контакты' })).toHaveAttribute('href', '/#contacts');

    await user.click(menuToggle());

    expect(screen.queryByRole('button', { name: 'Направления' })).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Туры' })).toHaveLength(2);
    expect(screen.getAllByRole('link', { name: 'Отзывы' })).toHaveLength(2);
    expect(screen.getAllByRole('link', { name: 'Контакты' })).toHaveLength(2);
    for (const link of screen.getAllByRole('link', { name: 'Отзывы' })) {
      expect(link).toHaveAttribute('href', '/#reviews');
    }
    for (const link of screen.getAllByRole('link', { name: 'Контакты' })) {
      expect(link).toHaveAttribute('href', '/#contacts');
    }
  });

  it('navigates tours, reviews and contacts from the desktop menu', async () => {
    const user = userEvent.setup();
    renderHeader('/start');

    await user.click(screen.getByRole('button', { name: 'Туры' }));
    expect(locationText()).toBe('/tours');

    await user.click(screen.getByRole('link', { name: 'Отзывы' }));
    expect(locationText()).toBe('/#reviews');

    await user.click(screen.getByRole('link', { name: 'Контакты' }));
    expect(locationText()).toBe('/#contacts');
    expect(HTMLElement.prototype.scrollIntoView).not.toHaveBeenCalled();
  });

  it('navigates from the mobile menu and closes it', async () => {
    const user = userEvent.setup();
    window.history.pushState({}, '', '/start');
    renderHeader('/start');

    await user.click(menuToggle());
    const tours = screen.getAllByRole('button', { name: 'Туры' });
    await user.click(tours[tours.length - 1]);

    expect(locationText()).toBe('/tours');
    expect(useUIStore.getState().isMobileMenuOpen).toBe(false);
    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: 'Туры' })).toHaveLength(1);
    });
  });
});

function signIn(role: 'client' | 'manager' | 'admin') {
  useAuthStore.setState({
    user: {
      id: 'user-1',
      email: 'marina@example.com',
      firstName: 'Марина',
      lastName: 'Иванова',
      phone: '',
      role,
      createdAt: '2026-01-01T00:00:00Z',
    },
    isAuthenticated: true,
    isLoading: false,
  });
}

function setPhone(phone: string | null) {
  useSiteSettingsStore.setState({
    settings: { ...EMPTY_SITE_SETTINGS, supportPhone: phone },
  });
}

function favoriteLinks(): HTMLElement[] {
  return screen.getAllByRole('link').filter((link) => link.getAttribute('href') === '/dashboard/favorites');
}

describe('Header pointer', () => {
  beforeEach(() => {
    useUIStore.setState({
      isMobileMenuOpen: false,
      isAuthModalOpen: false,
      authModalTab: 'login',
    });
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });
    useFavoritesStore.setState({ count: 3 });
    useSiteSettingsStore.setState({ settings: EMPTY_SITE_SETTINGS });
    window.history.pushState({}, '', '/');
    HTMLElement.prototype.scrollIntoView = vi.fn();
  });

  it('ТК-10 на широкой полосе курсор ссылки только у нажимаемых пунктов', async () => {
    const user = userEvent.setup();
    renderHeader();

    expect(screen.getByRole('link', { name: 'TravelAgency' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('button', { name: 'Туры' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('link', { name: 'Отзывы' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('link', { name: 'Контакты' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('button', { name: 'Войти' })).toHaveClass('cursor-pointer');
    const hearts = favoriteLinks();
    expect(hearts).toHaveLength(2);
    for (const heart of hearts) {
      expect(heart).toHaveClass('cursor-pointer');
      expect(heart).toHaveTextContent('3');
    }
    expect(screen.queryByRole('button', { name: 'Профиль' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Профиль' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Туры' }));
    expect(locationText()).toBe('/tours');
  });

  it('ТК-10 телефон со ссылкой получает курсор, телефон без вызова остаётся текстом', async () => {
    const user = userEvent.setup();
    setPhone('+79991234567');
    const view = renderHeader();
    const tel = screen.getByRole('link', { name: '+79991234567' });
    expect(tel).toHaveAttribute('href', 'tel:+79991234567');
    expect(tel).toHaveClass('cursor-pointer');
    view.unmount();

    setPhone('12345');
    renderHeader();
    const plain = screen.getByText('12345');
    expect(plain.tagName).toBe('SPAN');
    expect(plain).not.toHaveClass('cursor-pointer');
    expect(document.querySelector('a[href^="tel:"]')).toBeNull();
    await user.click(menuToggle());
    const phones = screen.getAllByText('12345');
    expect(phones).toHaveLength(2);
    for (const phone of phones) {
      expect(phone.tagName).toBe('SPAN');
      expect(phone).not.toHaveClass('cursor-pointer');
    }
  });

  it('ТК-10 зона вошедшего показывает короткое имя и открывает прежнее меню', async () => {
    const user = userEvent.setup();
    signIn('client');
    renderHeader();

    const zone = screen.getByRole('button', { name: /Марина/ });
    expect(zone).toHaveClass('cursor-pointer');
    expect(zone).toHaveTextContent('Марина');
    expect(zone.textContent).not.toContain('Иванова');
    expect(screen.queryByRole('button', { name: 'Профиль' })).not.toBeInTheDocument();

    await user.click(zone);
    expect(screen.getByRole('button', { name: 'Личный кабинет' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('button', { name: 'Мои бронирования' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('button', { name: 'Профиль' })).toHaveClass('cursor-pointer');
    expect(screen.getByRole('button', { name: 'Выход' })).toHaveClass('cursor-pointer');
    expect(screen.queryByRole('button', { name: 'Панель менеджера' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Администрирование' })).not.toBeInTheDocument();
    const email = screen.getByText('marina@example.com');
    expect(email.tagName).toBe('P');
    expect(email).not.toHaveClass('cursor-pointer');
    expect(email.closest('button')).toBeNull();

    await user.click(favoriteLinks()[0]);
    expect(locationText()).toBe('/dashboard/favorites');
  });

  it.each([
    ['manager', 'Панель менеджера'],
    ['admin', 'Администрирование'],
  ] as const)('ТК-11 пункт %s есть только у своей роли и с курсором ссылки', async (role, label) => {
    const user = userEvent.setup();
    signIn(role);
    renderHeader();
    await user.click(screen.getByRole('button', { name: /Марина/ }));
    expect(screen.getByRole('button', { name: label })).toHaveClass('cursor-pointer');
    if (role === 'manager') {
      expect(screen.queryByRole('button', { name: 'Администрирование' })).not.toBeInTheDocument();
    } else {
      expect(screen.getByRole('button', { name: 'Панель менеджера' })).toHaveClass('cursor-pointer');
    }
    await user.click(screen.getByRole('button', { name: 'Личный кабинет' }));
    expect(locationText()).toBe('/dashboard');
  });

  it('ТК-11 узкое меню и выход держат курсор, почта и блок имени остаются текстом', async () => {
    const logout = vi.spyOn(authApi, 'logout').mockResolvedValue(undefined);
    const user = userEvent.setup();
    signIn('admin');
    setPhone('+79991234567');
    renderHeader('/start');

    const toggle = menuToggle();
    expect(toggle).toHaveClass('cursor-pointer');
    await user.click(toggle);
    for (const label of ['Туры', 'Личный кабинет', 'Мои бронирования', 'Панель менеджера', 'Администрирование', 'Выход']) {
      const buttons = screen.getAllByRole('button', { name: label });
      expect(buttons.length).toBeGreaterThan(0);
      for (const button of buttons) expect(button).toHaveClass('cursor-pointer');
    }
    for (const label of ['Отзывы', 'Контакты']) {
      for (const link of screen.getAllByRole('link', { name: label })) {
        expect(link).toHaveClass('cursor-pointer');
      }
    }
    for (const tel of screen.getAllByRole('link', { name: '+79991234567' })) {
      expect(tel).toHaveAttribute('href', 'tel:+79991234567');
      expect(tel).toHaveClass('cursor-pointer');
    }
    expect(screen.queryByRole('button', { name: 'Профиль' })).not.toBeInTheDocument();
    const email = screen.getByText('marina@example.com');
    expect(email.tagName).toBe('P');
    expect(email).not.toHaveClass('cursor-pointer');
    const fullName = screen.getByText('Марина Иванова');
    expect(fullName.closest('button')).toBeNull();
    expect(fullName).not.toHaveClass('cursor-pointer');

    await user.click(screen.getByRole('button', { name: 'Выход' }));
    expect(locationText()).toBe('/');
    expect(logout).toHaveBeenCalled();
    logout.mockRestore();
  });

  it('ТК-12 боковое меню и подвал не получают курсор ссылки этого среза', () => {
    render(
      <MemoryRouter>
        <Sidebar title="Кабинет" items={[{ icon: LayoutDashboard, label: 'Обзор', path: '/dashboard' }]} />
        <Footer />
      </MemoryRouter>,
    );

    const sidebar = document.querySelector('aside');
    expect(sidebar).not.toBeNull();
    expect(sidebar?.querySelector('.cursor-pointer')).toBeNull();
    const toggle = document.querySelector('button svg.lucide-menu')?.closest('button');
    expect(toggle).not.toBeNull();
    expect(toggle?.className.split(' ')).not.toContain('cursor-pointer');

    const footer = document.querySelector('footer');
    expect(footer?.querySelector('.cursor-pointer')).toBeNull();
    const subscribe = within(footer as HTMLElement).getByRole('button', { name: /Подписаться/ });
    expect(subscribe.className).toContain('disabled:cursor-not-allowed');
    expect(subscribe.className.split(' ')).not.toContain('cursor-pointer');
    expect(subscribe).toBeDisabled();
  });
});
