import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import userEvent, { PointerEventsCheckLevel } from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { useUIStore } from '@/store/uiStore';
import AuthModal from './AuthModal';

vi.mock('framer-motion', async () => {
  const actual = await vi.importActual<typeof import('framer-motion')>('framer-motion');
  return {
    ...actual,
    AnimatePresence: ({ children }: { children: ReactNode }) => children,
  };
});

function renderLogin() {
  useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'login' });
  return render(
    <MemoryRouter>
      <AuthModal />
    </MemoryRouter>,
  );
}

describe('AuthModal close control', () => {
  beforeEach(() => {
    useUIStore.setState({ isAuthModalOpen: false, authModalTab: 'login', isMobileMenuOpen: false });
  });

  it('closes the login modal from the round button, its edges, and the icon', async () => {
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderLogin();

    const button = screen.getByRole('button', { name: 'Закрыть' });
    const icon = button.querySelector('svg');
    const loginTab = screen.getByRole('button', { name: 'Вход' });
    const tabs = loginTab.parentElement;

    expect(icon).not.toBeNull();
    expect(button).toHaveClass('absolute', 'z-10', 'rounded-full');
    expect(icon).toHaveClass('pointer-events-none');
    expect(tabs).toHaveClass('relative');
    expect(following(button, loginTab)).toBe(true);
    expect(tabs?.parentElement).toHaveClass('pt-14');

    await user.pointer([
      { target: button, coords: { x: 2, y: 2 }, keys: '[MouseLeft]' },
    ]);
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(screen.queryByRole('button', { name: 'Войти' })).not.toBeInTheDocument();

    act(() => {
      useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'login' });
    });
    await user.click(screen.getByRole('button', { name: 'Закрыть' }).querySelector('svg') as Element);
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument();

    act(() => {
      useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'login' });
    });
    await user.click(screen.getByRole('button', { name: 'Закрыть' }));
    expect(useUIStore.getState().isAuthModalOpen).toBe(false);
    expect(screen.queryByRole('button', { name: 'Войти' })).not.toBeInTheDocument();
  });
});

function following(node: Node, later: Node) {
  return (node.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
}
