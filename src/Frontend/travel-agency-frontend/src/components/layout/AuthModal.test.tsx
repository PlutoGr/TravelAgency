import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import userEvent, { PointerEventsCheckLevel } from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import toast from 'react-hot-toast';
import * as authApi from '@/api/auth';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import AuthModal from './AuthModal';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

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

describe('AuthModal unavailable auth service', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'login', isMobileMenuOpen: false });
  });

  it('keeps email and clears the password when login returns 502', async () => {
    vi.spyOn(authApi, 'login').mockRejectedValue({ response: { status: 502 } });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderLogin();

    await user.type(screen.getByLabelText('Email'), 'a@b.com');
    await user.type(screen.getByLabelText('Пароль'), 'secret');
    await user.click(screen.getByRole('button', { name: 'Войти' }));

    expect(toast.error).toHaveBeenCalledWith('Не удалось войти. Попробуйте ещё раз через минуту');
    expect(screen.getByLabelText('Email')).toHaveValue('a@b.com');
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('shows a wrong password under the form, keeps the email and clears the password', async () => {
    vi.spyOn(authApi, 'login').mockRejectedValue({ response: { status: 401 } });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderLogin();

    await user.type(screen.getByLabelText('Email'), 'a@b.com');
    await user.type(screen.getByLabelText('Пароль'), 'secret');
    await user.click(screen.getByRole('button', { name: 'Войти' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Неверный email или пароль');
    expect(toast.error).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Email')).toHaveValue('a@b.com');
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('shows too many login attempts under the form on 429, keeps the email and clears the password', async () => {
    const login = vi.spyOn(authApi, 'login').mockRejectedValue({ response: { status: 429 } });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderLogin();

    await user.type(screen.getByLabelText('Email'), 'a@b.com');
    await user.type(screen.getByLabelText('Пароль'), 'secret');
    await user.click(screen.getByRole('button', { name: 'Войти' }));

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Слишком много попыток входа. Подождите немного и попробуйте снова.',
    );
    expect(toast.error).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Email')).toHaveValue('a@b.com');
    expect(screen.getByLabelText('Пароль')).toHaveValue('');

    const submit = screen.getByRole('button', { name: 'Войти' });
    expect(submit).toBeEnabled();
    await user.type(screen.getByLabelText('Пароль'), 'secret2');
    await user.click(submit);

    expect(login).toHaveBeenCalledTimes(2);
    expect(submit).toBeEnabled();
    expect(screen.getByLabelText('Email')).toHaveValue('a@b.com');
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('keeps email and name and clears the password when register returns 502', async () => {
    vi.spyOn(authApi, 'register').mockRejectedValue({ response: { status: 502 } });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderRegister();

    await fillRegister(user);
    await user.click(screen.getByRole('button', { name: 'Зарегистрироваться' }));

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Не удалось зарегистрироваться. Попробуйте ещё раз через минуту',
    );
    expect(toast.error).not.toHaveBeenCalled();
    expectKeptRegisterFields();
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('shows a registration error under the form on 401, keeps the fields and clears the password', async () => {
    vi.spyOn(authApi, 'register').mockRejectedValue({ response: { status: 401 } });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderRegister();

    await fillRegister(user);
    await user.click(screen.getByRole('button', { name: 'Зарегистрироваться' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Ошибка регистрации. Попробуйте ещё раз.');
    expect(toast.error).not.toHaveBeenCalled();
    expectKeptRegisterFields();
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('shows the server detail under the form on 409, keeps the fields and clears the password', async () => {
    vi.spyOn(authApi, 'register').mockRejectedValue({
      response: { status: 409, data: { detail: 'A user with this email already exists.' } },
    });
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderRegister();

    await fillRegister(user);
    await user.click(screen.getByRole('button', { name: 'Зарегистрироваться' }));

    expect(screen.getByRole('alert')).toHaveTextContent('A user with this email already exists.');
    expect(toast.error).not.toHaveBeenCalled();
    expectKeptRegisterFields();
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('shows an empty-fields error under the form and does not toast', async () => {
    const register = vi.spyOn(authApi, 'register');
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    renderRegister();

    await user.type(screen.getByLabelText('Имя'), 'Анна');
    await user.type(screen.getByLabelText('Пароль'), 'Password1');
    await user.click(screen.getByRole('button', { name: 'Зарегистрироваться' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Заполните все поля');
    expect(toast.error).not.toHaveBeenCalled();
    expect(register).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
    expect(screen.getByLabelText('Пароль')).toHaveValue('Password1');
  });
});

function renderRegister() {
  useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'register' });
  return render(
    <MemoryRouter>
      <AuthModal />
    </MemoryRouter>,
  );
}

async function fillRegister(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Имя'), 'Анна');
  await user.type(screen.getByLabelText('Фамилия'), 'Смирнова');
  await user.type(screen.getByLabelText('Email'), 'anna@example.com');
  await user.type(screen.getByLabelText('Телефон'), '+79990001122');
  await user.type(screen.getByLabelText('Пароль'), 'Password1');
}

function expectKeptRegisterFields() {
  expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
  expect(screen.getByLabelText('Фамилия')).toHaveValue('Смирнова');
  expect(screen.getByLabelText('Email')).toHaveValue('anna@example.com');
  expect(screen.getByLabelText('Телефон')).toHaveValue('+79990001122');
}

function following(node: Node, later: Node) {
  return (node.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
}
