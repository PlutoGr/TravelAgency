import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { apiClient } from '@/api/client';
import Header from '@/components/layout/Header';
import AuthModal from '@/components/layout/AuthModal';
import AuthRedirectHandler from '@/components/layout/AuthRedirectHandler';
import ProtectedRoute from '@/components/layout/ProtectedRoute';
import { ACCOUNT_TEXT } from '@/domain/accountLinks';
import { PASSWORD_HINT } from '@/domain/profileForm';
import ProfilePage from '@/pages/ProfilePage';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import type { User } from '@/types';
import CancelEmailPage from './CancelEmailPage';
import ConfirmEmailPage from './ConfirmEmailPage';
import NewPasswordPage from './NewPasswordPage';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

vi.mock('framer-motion', async () => {
  const actual = await vi.importActual<typeof import('framer-motion')>('framer-motion');
  return {
    ...actual,
    AnimatePresence: ({ children }: { children?: ReactNode }) => children ?? null,
  };
});

const person = (role: User['role'], email = 'anna@example.com'): User => ({
  id: 'user-1',
  email,
  firstName: 'Анна',
  lastName: 'Смирнова',
  phone: '+79990000000',
  role,
  createdAt: '2026-01-01T00:00:00Z',
});

function Location() {
  const location = useLocation();
  return <div data-testid="loc">{`${location.pathname}${location.search}`}</div>;
}

function renderAt(path: string, node: ReactNode) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Location />
      <Routes>
        <Route path="*" element={node} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ТК-1 восстановление пароля', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    useUIStore.setState({ isAuthModalOpen: false, authModalTab: 'login', authIntent: null });
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
  });

  function open() {
    useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'login' });
    return render(
      <MemoryRouter>
        <AuthModal />
      </MemoryRouter>,
    );
  }

  it('opens the step, validates the address and returns it to login', async () => {
    const user = userEvent.setup();
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { message: ACCOUNT_TEXT.resetSent } });
    open();

    const password = screen.getByLabelText('Пароль');
    const forgot = screen.getByRole('button', { name: 'Забыли пароль?' });
    const submit = screen.getByRole('button', { name: 'Войти' });
    expect(password.compareDocumentPosition(forgot) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(forgot.compareDocumentPosition(submit) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    await user.click(forgot);
    expect(screen.getByRole('heading', { name: 'Восстановление пароля' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Вход' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Регистрация' })).not.toBeInTheDocument();
    const send = screen.getByRole('button', { name: 'Отправить ссылку' });
    expect(send.className).toContain('w-full');

    await user.click(send);
    expect(screen.getByText(ACCOUNT_TEXT.emailInvalid)).toBeInTheDocument();
    expect(post).not.toHaveBeenCalled();

    await user.type(screen.getByLabelText('Email'), 'not-an-email');
    await user.click(send);
    expect(post).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText('Email'));
    await user.type(screen.getByLabelText('Email'), `${'a'.repeat(250)}@example.com`);
    await user.click(send);
    expect(post).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText('Email'));
    await user.type(screen.getByLabelText('Email'), '  Anna@Example.com  ');
    let resolvePost: (value: { data: { message: string } }) => void = () => undefined;
    post.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePost = resolve;
        }),
    );
    await user.click(send);
    await user.click(send);
    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith('/auth/password-resets/request', { email: 'Anna@Example.com' });
    resolvePost({ data: { message: ACCOUNT_TEXT.resetSent } });
    expect(await screen.findByText(ACCOUNT_TEXT.resetSent)).toBeInTheDocument();
    expect(screen.queryByText(/account\/password\/new/)).not.toBeInTheDocument();
    expect(screen.queryByText('Неверный email или пароль.')).not.toBeInTheDocument();
    expect(screen.queryByText('Слишком много попыток входа. Подождите немного и попробуйте снова.')).not.toBeInTheDocument();
    expect(screen.queryByText(/Доступ к аккаунту ограничен/)).not.toBeInTheDocument();

    post.mockRejectedValueOnce(new Error('offline'));
    await user.click(screen.getByRole('button', { name: 'Отправить ссылку' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(ACCOUNT_TEXT.requestFailed);
    expect(screen.getByRole('alert')).not.toHaveTextContent(ACCOUNT_TEXT.resetSent);

    await user.click(screen.getByRole('button', { name: 'Вернуться ко входу' }));
    expect(screen.getByLabelText('Email')).toHaveValue('Anna@Example.com');
    expect(screen.getByLabelText('Пароль')).toHaveValue('');
  });

  it('opens the same step from /?auth=forgot', async () => {
    useUIStore.setState({ isAuthModalOpen: false, authModalTab: 'login' });
    render(
      <MemoryRouter initialEntries={['/?auth=forgot']}>
        <AuthRedirectHandler />
        <AuthModal />
      </MemoryRouter>,
    );
    expect(await screen.findByRole('heading', { name: 'Восстановление пароля' })).toBeInTheDocument();
  });

  it('hides the reset link on the registration tab', () => {
    useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'register' });
    render(
      <MemoryRouter>
        <AuthModal />
      </MemoryRouter>,
    );
    expect(screen.getByRole('button', { name: 'Регистрация' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Забыли пароль?' })).not.toBeInTheDocument();
  });
});

describe('ТК-2 новый пароль', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    useAuthStore.setState({
      user: person('client'),
      isAuthenticated: true,
      isLoading: false,
    });
  });

  function renderReset() {
    return renderAt('/account/password/new?token=secret-value', <NewPasswordPage />);
  }

  it('strips the secret and follows inspect and complete outcomes', async () => {
    const user = userEvent.setup();
    const post = vi.spyOn(apiClient, 'post').mockImplementation(async (url: string) => {
      if (url.endsWith('/inspect')) return { data: { outcome: 'ready' } };
      if (url.endsWith('/logout')) return { data: {} };
      return {
        data: {
          outcome: 'changed',
          session: 'current',
          detail: 'Password was changed.',
          errors: { Password: ['weak'] },
        },
      };
    });

    renderReset();
    await waitFor(() => expect(screen.getByTestId('loc')).toHaveTextContent('/account/password/new'));
    expect(screen.getByTestId('loc').textContent).not.toContain('token');
    expect(post).toHaveBeenCalledWith('/auth/password-resets/inspect', { token: 'secret-value' });
    expect(screen.getByLabelText('Новый пароль')).toHaveAccessibleDescription(ACCOUNT_TEXT.passwordHint);
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Текущий пароль')).not.toBeInTheDocument();
    expect(document.querySelector('meta[name="referrer"]')).toHaveAttribute('content', 'no-referrer');
    expect(document.querySelector('img')).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Задать пароль' }));
    expect(screen.getByText(ACCOUNT_TEXT.passwordEmpty)).toBeInTheDocument();
    expect(post).toHaveBeenCalledTimes(1);

    await user.type(screen.getByLabelText('Новый пароль'), 'Password1');
    await user.type(screen.getByLabelText('Повторите пароль'), 'Password2');
    await user.click(screen.getByRole('button', { name: 'Задать пароль' }));
    expect(screen.getByText(ACCOUNT_TEXT.passwordMismatch)).toBeInTheDocument();
    expect(post).toHaveBeenCalledTimes(1);

    post.mockImplementation(async (url: string) => {
      if (String(url).includes('logout')) return { data: {} };
      return {
        data: {
          code: 'PasswordRule',
          detail: 'Password must contain at least one uppercase letter.',
          errors: { Password: ['too short'] },
        },
      };
    });
    post.mockRejectedValueOnce({
      response: {
        status: 400,
        data: {
          code: 'PasswordRule',
          detail: 'Password must contain at least one uppercase letter.',
          errors: { Password: ['too short'] },
        },
      },
    });
    await user.clear(screen.getByLabelText('Повторите пароль'));
    await user.type(screen.getByLabelText('Повторите пароль'), 'short');
    await user.clear(screen.getByLabelText('Новый пароль'));
    await user.type(screen.getByLabelText('Новый пароль'), 'short');
    await user.click(screen.getByRole('button', { name: 'Задать пароль' }));
    expect(await screen.findByText(ACCOUNT_TEXT.passwordRule)).toBeInTheDocument();
    expect(screen.queryByText(/Password must/)).not.toBeInTheDocument();
    expect(screen.queryByText('too short')).not.toBeInTheDocument();

    post.mockResolvedValue({ data: { outcome: 'changed', session: 'current' } });
    await user.clear(screen.getByLabelText('Новый пароль'));
    await user.type(screen.getByLabelText('Новый пароль'), 'Password1');
    await user.clear(screen.getByLabelText('Повторите пароль'));
    await user.type(screen.getByLabelText('Повторите пароль'), 'Password1');
    await user.click(screen.getByRole('button', { name: 'Задать пароль' }));
    expect(await screen.findByText(ACCOUNT_TEXT.resetChanged)).toBeInTheDocument();
    expect(post).toHaveBeenCalledWith('/auth/logout', {});
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});

describe('ТК-24 nginx token mask and referrer', () => {
  it('masks token the same way as access_token and sets no-referrer on the three pages', () => {
    const logging = readFileSync(resolve(process.cwd(), 'nginx-logging.conf'), 'utf8');
    const app = readFileSync(resolve(process.cwd(), 'nginx-app.conf'), 'utf8');
    const request = /"~(?<pattern>[^"]+)" "\$\{ta_pre\}\*\*\*"/.exec(logging);
    const referer = /"~(?<pattern>[^"]+)" "\$\{ta_rpre\}\*\*\*"/.exec(logging);
    expect(request).toBeTruthy();
    expect(referer).toBeTruthy();

    const mask = (pattern: string, value: string) => {
      const source = pattern.startsWith('*') ? pattern.slice(1) : pattern;
      const match = value.match(new RegExp(source, 'i'));
      if (!match?.groups) return value;
      const pre = match.groups.ta_pre ?? match.groups.ta_rpre;
      return `${pre}***`;
    };

    const requestMask = (value: string) => mask(request!.groups!.pattern, value);
    const refererMask = (value: string) => mask(referer!.groups!.pattern, value);
    expect(requestMask('/account/password/new?token=SECRET&next=1')).toBe('/account/password/new?token=***');
    expect(requestMask('/hub?access_token=SECRET&x=1')).toBe('/hub?access_token=***');
    expect(requestMask('/hub?foo=1&TOKEN=SECRET')).toBe('/hub?foo=1&TOKEN=***');
    expect(requestMask('/hub?access%5Ftoken=SECRET')).toBe('/hub?access%5Ftoken=***');
    expect(requestMask('/hub?foo=1')).toBe('/hub?foo=1');
    expect(refererMask('https://travelagency.example/account/email/confirm?token=SECRET')).toBe(
      'https://travelagency.example/account/email/confirm?token=***',
    );

    for (const path of ['/account/password/new', '/account/email/confirm', '/account/email/cancel']) {
      expect(app).toContain(`location = ${path}`);
    }
    const account = app.slice(app.indexOf('location = /account/password/new'));
    expect(account).toContain('Referrer-Policy "no-referrer"');
    expect(account.match(/Referrer-Policy "no-referrer"/g)?.length).toBe(3);
  });
});

describe('account pages and settings', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    useAuthStore.setState({ user: person('client'), isAuthenticated: true, isLoading: false });
    useUIStore.setState({ isAuthModalOpen: false, authModalTab: 'login', authIntent: null });
  });

  it('ТК-3 confirms email for the signed-in account, a guest and someone else', async () => {
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({
      data: { outcome: 'changed', session: 'current' },
    });
    const get = vi.spyOn(apiClient, 'get').mockResolvedValue({
      data: {
        id: 'user-1',
        email: 'new@example.com',
        firstName: 'Анна',
        lastName: 'Смирнова',
        phone: '+79990000000',
        role: 'Client',
        createdAt: '2026-01-01T00:00:00Z',
      },
    });
    const view = render(
      <MemoryRouter initialEntries={['/account/email/confirm?token=confirm-secret']}>
        <Location />
        <Header />
        <Routes>
          <Route path="*" element={<ConfirmEmailPage />} />
        </Routes>
      </MemoryRouter>,
    );
    expect(await screen.findByText(ACCOUNT_TEXT.emailChanged)).toBeInTheDocument();
    await waitFor(() => expect(useAuthStore.getState().user?.email).toBe('new@example.com'));
    await userEvent.click(screen.getByRole('button', { name: /Анна/ }));
    expect(screen.getByText('new@example.com')).toBeInTheDocument();
    expect(post).toHaveBeenCalledWith('/auth/email-confirmations', { token: 'confirm-secret' });
    expect(get).toHaveBeenCalledWith('/auth/me');
    expect(screen.getByTestId('loc').textContent).not.toContain('token');
    expect(useAuthStore.getState().user?.email).toBe('new@example.com');
    view.unmount();

    useAuthStore.setState({ user: person('client'), isAuthenticated: true, isLoading: false });
    post.mockResolvedValue({ data: { outcome: 'changed', session: 'current' } });
    get.mockRejectedValueOnce(new Error('profile'));
    const profileFails = renderAt('/account/email/confirm?token=profile-fails', <ConfirmEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.emailChanged)).toBeInTheDocument();
    expect(screen.queryByText(ACCOUNT_TEXT.linkInvalid)).not.toBeInTheDocument();
    expect(document.querySelector('meta[name="referrer"]')).toHaveAttribute('content', 'no-referrer');
    profileFails.unmount();

    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    post.mockResolvedValue({ data: { outcome: 'changed', session: 'none' } });
    renderAt('/account/email/confirm?token=other', <ConfirmEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.emailChanged)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Войти' })).toBeInTheDocument();

    useAuthStore.setState({ user: person('client', 'other@example.com'), isAuthenticated: true, isLoading: false });
    post.mockResolvedValue({ data: { outcome: 'changed', session: 'other' } });
    renderAt('/account/email/confirm?token=foreign', <ConfirmEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.emailChangedOther)).toBeInTheDocument();
    expect(useAuthStore.getState().user?.email).toBe('other@example.com');

    post.mockResolvedValue({ data: { outcome: 'taken' } });
    renderAt('/account/email/confirm?token=taken', <ConfirmEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.emailTaken)).toBeInTheDocument();

    post.mockResolvedValue({ data: { outcome: 'invalid', detail: 'Token is invalid.' } });
    renderAt('/account/email/confirm?token=dead', <ConfirmEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.linkInvalid)).toBeInTheDocument();
    expect(screen.queryByText('Token is invalid.')).not.toBeInTheDocument();
  });

  it('ТК-4 cancels or rejects the email link', async () => {
    const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { outcome: 'cancelled' } });
    const view = renderAt('/account/email/cancel?token=cancel-secret', <CancelEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.emailCancelled)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'На главную' })).toHaveAttribute('href', '/');
    expect(screen.getByTestId('loc').textContent).not.toContain('token');
    expect(post).toHaveBeenCalledWith('/auth/email-cancellations', { token: 'cancel-secret' });
    expect(document.querySelector('img')).toBeNull();
    view.unmount();

    post.mockResolvedValue({ data: { outcome: 'invalid' } });
    renderAt('/account/email/cancel?token=dead', <CancelEmailPage />);
    expect(await screen.findByText(ACCOUNT_TEXT.linkInvalid)).toBeInTheDocument();
  });

  it('ТК-5 changes the password from settings for every role', async () => {
    const user = userEvent.setup();
    for (const role of ['client', 'manager', 'admin'] as const) {
      useAuthStore.setState({ user: person(role), isAuthenticated: true, isLoading: false });
      const post = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: { success: true } });
      const view = renderAt('/dashboard/profile?tab=settings', <ProfilePage />);
      const password = screen.getByRole('region', { name: 'Пароль' });
      expect(within(password).queryByText('Появится позже')).not.toBeInTheDocument();
      expect(within(password).getByText(PASSWORD_HINT)).toBeInTheDocument();
      expect(within(password).queryByRole('button', { name: 'Забыли пароль?' })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Как меня видит клиент' })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Сохранить' })).not.toBeInTheDocument();

      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(within(password).getByText(ACCOUNT_TEXT.fillAll)).toBeInTheDocument();
      expect(post).not.toHaveBeenCalled();

      await user.type(within(password).getByLabelText('Текущий пароль'), 'OldPassword1');
      await user.type(within(password).getByLabelText('Новый пароль'), 'NewPassword1');
      await user.type(within(password).getByLabelText('Повторите пароль'), 'OtherPassword1');
      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(within(password).getByText(ACCOUNT_TEXT.passwordMismatch)).toBeInTheDocument();
      expect(post).not.toHaveBeenCalled();

      await user.clear(within(password).getByLabelText('Новый пароль'));
      await user.type(within(password).getByLabelText('Новый пароль'), 'short');
      await user.clear(within(password).getByLabelText('Повторите пароль'));
      await user.type(within(password).getByLabelText('Повторите пароль'), 'short');
      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(within(password).getByText(ACCOUNT_TEXT.passwordRule)).toBeInTheDocument();
      expect(post).not.toHaveBeenCalled();

      post.mockRejectedValueOnce({
        response: {
          status: 400,
          data: { code: 'CurrentPasswordInvalid', detail: 'Current password is wrong.', errors: { Password: ['no'] } },
        },
      });
      await user.clear(within(password).getByLabelText('Новый пароль'));
      await user.type(within(password).getByLabelText('Новый пароль'), 'NewPassword1');
      await user.clear(within(password).getByLabelText('Повторите пароль'));
      await user.type(within(password).getByLabelText('Повторите пароль'), 'NewPassword1');
      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(await within(password).findByText(ACCOUNT_TEXT.currentInvalid)).toBeInTheDocument();
      expect(within(password).queryByText('Current password is wrong.')).not.toBeInTheDocument();

      post.mockResolvedValueOnce({ data: { success: true } });
      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(await within(password).findByText(ACCOUNT_TEXT.passwordChanged)).toBeInTheDocument();
      expect(within(password).getByLabelText('Текущий пароль')).toHaveValue('');
      expect(useAuthStore.getState().isAuthenticated).toBe(true);

      post.mockRejectedValueOnce({ response: { status: 502, data: { detail: 'bad gateway' } } });
      await user.type(within(password).getByLabelText('Текущий пароль'), 'OldPassword1');
      await user.type(within(password).getByLabelText('Новый пароль'), 'NewPassword1');
      await user.type(within(password).getByLabelText('Повторите пароль'), 'NewPassword1');
      await user.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
      expect(await within(password).findByText(ACCOUNT_TEXT.requestFailed)).toBeInTheDocument();
      expect(within(password).queryByText(ACCOUNT_TEXT.passwordChanged)).not.toBeInTheDocument();
      view.unmount();
      post.mockRestore();
    }
  });

  it('ТК-6 changes email and hints at it from the profile', async () => {
    const user = userEvent.setup();
    const post = vi.spyOn(apiClient, 'post');
    for (const role of ['client', 'manager', 'admin'] as const) {
      useAuthStore.setState({ user: person(role), isAuthenticated: true, isLoading: false });
      post.mockClear();
      const view = renderAt('/dashboard/profile?tab=settings', <ProfilePage />);
      const email = screen.getByRole('region', { name: 'Email' });
      const fields = within(email).getAllByLabelText(/email|пароль/i);
      expect(fields.map((field) => field.getAttribute('aria-label') || (field as HTMLInputElement).labels?.[0]?.textContent)).toEqual([
        'Текущий email',
        'Новый email',
        'Текущий пароль',
      ]);
      expect(within(email).getByLabelText('Текущий email')).toBeDisabled();
      expect(within(email).getByLabelText('Текущий email')).toHaveValue('anna@example.com');

      await user.click(within(email).getByRole('button', { name: 'Сменить email' }));
      expect(within(email).getByText(ACCOUNT_TEXT.emailInvalid)).toBeInTheDocument();
      expect(post).not.toHaveBeenCalled();

      post.mockRejectedValueOnce({
        response: { status: 400, data: { code: 'EmailUnchanged', detail: 'Email is unchanged.', errors: { Email: ['same'] } } },
      });
      await user.type(within(email).getByLabelText('Новый email'), 'anna@example.com');
      await user.type(within(email).getByLabelText('Текущий пароль'), 'Password1');
      await user.click(within(email).getByRole('button', { name: 'Сменить email' }));
      expect(await within(email).findByText(ACCOUNT_TEXT.emailUnchanged)).toBeInTheDocument();
      expect(within(email).queryByText('same')).not.toBeInTheDocument();

      post.mockRejectedValueOnce({
        response: { status: 400, data: { code: 'CurrentPasswordInvalid', detail: 'Wrong password.' } },
      });
      await user.clear(within(email).getByLabelText('Новый email'));
      await user.type(within(email).getByLabelText('Новый email'), 'next@example.com');
      await user.clear(within(email).getByLabelText('Текущий пароль'));
      await user.type(within(email).getByLabelText('Текущий пароль'), 'Password1');
      await user.click(within(email).getByRole('button', { name: 'Сменить email' }));
      expect(await within(email).findByText(ACCOUNT_TEXT.currentInvalid)).toBeInTheDocument();

      post.mockResolvedValueOnce({ data: { message: ACCOUNT_TEXT.emailSent } });
      await user.clear(within(email).getByLabelText('Новый email'));
      await user.type(within(email).getByLabelText('Новый email'), 'next@example.com');
      await user.clear(within(email).getByLabelText('Текущий пароль'));
      await user.type(within(email).getByLabelText('Текущий пароль'), 'Password1');
      await user.click(within(email).getByRole('button', { name: 'Сменить email' }));
      expect(await within(email).findByText(ACCOUNT_TEXT.emailSent)).toBeInTheDocument();
      expect(within(email).getByLabelText('Текущий email')).toHaveValue('anna@example.com');
      expect(within(email).getByLabelText('Новый email')).toHaveValue('');
      expect(within(email).getByLabelText('Текущий пароль')).toHaveValue('');
      expect(within(email).queryByText(/ожидает подтверждения/i)).not.toBeInTheDocument();
      view.unmount();
    }

    renderAt('/dashboard/profile', <ProfilePage />);
    expect(screen.getByLabelText('Email')).toBeDisabled();
    expect(screen.getByText(ACCOUNT_TEXT.emailHint)).toBeInTheDocument();
    for (const label of ['Город', 'О себе', 'В туризме с', 'Языки', 'Специализация', 'Паспорт']) {
      expect(screen.queryByLabelText(label)).not.toBeInTheDocument();
    }
    expect(screen.getByLabelText('Имя')).toBeInTheDocument();
    expect(screen.getByLabelText('Фамилия')).toBeInTheDocument();
    expect(screen.getByLabelText('Телефон')).toBeInTheDocument();
  });

  it('meets a guest on settings with the login window', async () => {
    useAuthStore.setState({ user: null, isAuthenticated: false, isLoading: false });
    render(
      <MemoryRouter initialEntries={['/dashboard/profile?tab=settings']}>
        <AuthRedirectHandler />
        <AuthModal />
        <Routes>
          <Route element={<ProtectedRoute allowedRoles={['client', 'manager', 'admin']} />}>
            <Route path="dashboard/profile" element={<ProfilePage />} />
          </Route>
          <Route path="/" element={<div>Главная</div>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(await screen.findByRole('button', { name: 'Войти' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Сменить пароль' })).not.toBeInTheDocument();
  });

  it('ТК-7 prefers code, message and outcome over English detail and maps 429', async () => {
    const user = userEvent.setup();
    useUIStore.setState({ isAuthModalOpen: true, authModalTab: 'forgot' });
    const post = vi.spyOn(apiClient, 'post').mockRejectedValue({
      response: {
        status: 429,
        data: {
          message: ACCOUNT_TEXT.emailSent,
          detail: 'Too many attempts.',
          errors: { Email: ['limit'] },
        },
      },
    });
    render(
      <MemoryRouter>
        <AuthModal />
      </MemoryRouter>,
    );
    await user.type(screen.getByLabelText('Email'), 'anna@example.com');
    await user.click(screen.getByRole('button', { name: 'Отправить ссылку' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(ACCOUNT_TEXT.requestFailed);
    expect(screen.getByRole('alert')).not.toHaveTextContent(ACCOUNT_TEXT.emailSent);
    expect(screen.queryByText('Too many attempts.')).not.toBeInTheDocument();
    expect(screen.queryByText('limit')).not.toBeInTheDocument();
    expect(screen.queryByText('Слишком много попыток входа. Подождите немного и попробуйте снова.')).not.toBeInTheDocument();
    expect(post).toHaveBeenCalled();
  });
});
