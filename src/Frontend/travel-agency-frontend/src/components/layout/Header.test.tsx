import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import Header from './Header';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
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
    window.history.pushState({}, '', '/');
    HTMLElement.prototype.scrollIntoView = vi.fn();
  });

  it('does not show directions and keeps tours, reviews and contacts', async () => {
    const user = userEvent.setup();
    renderHeader();

    expect(screen.queryByRole('button', { name: 'Направления' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Туры' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Отзывы' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Контакты' })).toBeInTheDocument();

    await user.click(menuToggle());

    expect(screen.queryByRole('button', { name: 'Направления' })).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Туры' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Отзывы' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Контакты' })).toHaveLength(2);
  });

  it('navigates tours, reviews and contacts from the desktop menu', async () => {
    const user = userEvent.setup();
    window.history.pushState({}, '', '/start');
    renderHeader('/start');
    const scrollReviews = vi
      .spyOn(document.getElementById('reviews')!, 'scrollIntoView')
      .mockImplementation(() => {});
    const scrollContacts = vi
      .spyOn(document.getElementById('contacts')!, 'scrollIntoView')
      .mockImplementation(() => {});

    await user.click(screen.getByRole('button', { name: 'Туры' }));
    expect(locationText()).toBe('/tours');

    window.history.pushState({}, '', '/tours');
    await user.click(screen.getByRole('button', { name: 'Отзывы' }));
    expect(locationText()).toBe('/');
    await waitFor(() => {
      expect(scrollReviews).toHaveBeenCalled();
    });
    expect(scrollContacts).not.toHaveBeenCalled();

    window.history.pushState({}, '', '/tours');
    await user.click(screen.getByRole('button', { name: 'Контакты' }));
    expect(locationText()).toBe('/');
    await waitFor(() => {
      expect(scrollContacts).toHaveBeenCalled();
    });
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
