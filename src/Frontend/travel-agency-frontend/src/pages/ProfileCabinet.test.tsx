import { useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import * as accountApi from '@/api/account';
import * as authApi from '@/api/auth';
import AdminLayout from '@/components/layout/AdminLayout';
import DashboardLayout from '@/components/layout/DashboardLayout';
import Header from '@/components/layout/Header';
import ManagerLayout from '@/components/layout/ManagerLayout';
import { ACCOUNT_TEXT } from '@/domain/accountLinks';
import ProfilePage from '@/pages/ProfilePage';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import type { User } from '@/types';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

const user: User = {
  id: 'user-1',
  email: 'anna@example.com',
  firstName: 'Анна',
  lastName: 'Смирнова',
  phone: '+79990001122',
  role: 'client',
  createdAt: '2026-01-01T00:00:00Z',
};

function signIn(person: User) {
  useAuthStore.setState({ user: person, isAuthenticated: true, isLoading: false });
}

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function renderProfile(path = '/dashboard/profile') {
  return renderPage(
    <>
      <LocationProbe />
      <ProfilePage />
    </>,
    path,
  );
}

function openPersonal() {
  const account = screen.queryByRole('tab', { name: 'Аккаунт' });
  if (account) fireEvent.click(account);
  fireEvent.click(screen.getByRole('button', { name: 'Изменить' }));
  return screen.getByRole('dialog', { name: 'Личные данные' });
}

function backdropOf(dialog: HTMLElement): HTMLElement {
  const backdrop = dialog.parentElement?.querySelector('[data-overlay]');
  if (!(backdrop instanceof HTMLElement)) throw new Error('backdrop missing');
  return backdrop;
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => {
    resolve = res;
  });
  return { promise, resolve };
}

describe('profile cabinet', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    signIn(user);
  });

  it('ТК-2 keeps the blocks in a wrapping row from the left', () => {
    renderProfile();
    const personal = screen.getByRole('region', { name: 'Личные данные' });
    const account = screen.getByRole('region', { name: 'Вход в аккаунт' });
    const row = personal.parentElement;
    expect(row).toBe(account.parentElement);
    expect(row?.className).toContain('flex');
    expect(row?.className).toContain('flex-wrap');
    expect(row?.className).not.toContain('mx-auto');
    expect(personal.className).toContain('shrink-0');
    expect(account.className).toContain('shrink-0');
    expect(personal.compareDocumentPosition(account) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(document.querySelector('.mx-auto.max-w-2xl')).toBeNull();
    expect(screen.getByRole('heading', { level: 1, name: 'Профиль' }).parentElement?.className).not.toContain(
      'mx-auto',
    );
  });

  it('ТК-4 sends the header and cabinet profile items to the cabinet and leaves staff menus alone', () => {
    signIn({ ...user, role: 'manager' });
    const header = renderPage(
      <>
        <LocationProbe />
        <Header />
      </>,
      '/tours',
    );
    fireEvent.click(screen.getByRole('button', { name: /Анна/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Профиль' }));
    expect(screen.getByTestId('location')).toHaveTextContent('/dashboard/profile');
    header.unmount();

    signIn({ ...user, role: 'admin' });
    const cabinet = renderPage(<DashboardLayout />, '/dashboard');
    expect(screen.getByRole('heading', { name: 'Личный кабинет' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Профиль' })).toHaveAttribute('href', '/dashboard/profile');
    cabinet.unmount();

    const manager = renderPage(<ManagerLayout />, '/manager');
    const managerAside = screen.getByRole('complementary');
    expect(within(managerAside).getByRole('heading', { name: 'Панель менеджера' })).toBeInTheDocument();
    expect(within(managerAside).queryByRole('link', { name: 'Профиль' })).not.toBeInTheDocument();
    manager.unmount();

    renderPage(<AdminLayout />, '/admin');
    const adminAside = screen.getByRole('complementary');
    expect(within(adminAside).getByRole('link', { name: 'Настройки сайта' })).toHaveAttribute(
      'href',
      '/admin/settings',
    );
    expect(within(adminAside).queryByRole('link', { name: 'Профиль' })).not.toBeInTheDocument();
  });

  it('ТК-5 reopens the personal dialog on the saved card without a photo or email', () => {
    signIn({ ...user, phone: '' });
    renderProfile();
    const personal = screen.getByRole('region', { name: 'Личные данные' });
    expect(within(personal).getByText('Телефон')).toBeInTheDocument();
    expect(within(personal).queryByText('не указано')).not.toBeInTheDocument();
    expect(within(personal).queryByText('Появится позже')).not.toBeInTheDocument();

    const dialog = openPersonal();
    expect(within(dialog).getByLabelText('Имя')).toHaveValue('Анна');
    expect(within(dialog).getByLabelText('Фамилия')).toHaveValue('Смирнова');
    expect(within(dialog).getByLabelText('Телефон')).toHaveValue('');
    expect(within(dialog).getByRole('button', { name: 'Сохранить' })).toBeInTheDocument();
    expect(within(dialog).getByRole('button', { name: 'Отмена' })).toBeInTheDocument();
    expect(within(dialog).queryByRole('img')).not.toBeInTheDocument();
    expect(within(dialog).queryByText(user.email)).not.toBeInTheDocument();
    expect(within(dialog).queryByLabelText('Email')).not.toBeInTheDocument();

    fireEvent.change(within(dialog).getByLabelText('Имя'), { target: { value: 'Черновик' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Отмена' }));
    const again = openPersonal();
    expect(within(again).getByLabelText('Имя')).toHaveValue('Анна');
    expect(within(again).getByLabelText('Телефон')).toHaveValue('');
  });

  it('ТК-8 closes the personal dialog without writing and without a discard prompt', async () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();
    const closers = [
      () => fireEvent.click(screen.getByRole('button', { name: 'Отмена' })),
      () => fireEvent.click(screen.getByRole('button', { name: 'Закрыть' })),
      () => fireEvent.click(backdropOf(screen.getByRole('dialog', { name: 'Личные данные' }))),
      () => fireEvent.keyDown(document, { key: 'Escape' }),
    ];

    for (const close of closers) {
      const dialog = openPersonal();
      fireEvent.change(within(dialog).getByLabelText('Имя'), { target: { value: 'Черновик' } });
      fireEvent.change(within(dialog).getByLabelText('Телефон'), { target: { value: '+70000000000' } });
      close();
      await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument());
      expect(updateProfile).not.toHaveBeenCalled();
      expect(screen.getByText('Анна Смирнова')).toBeInTheDocument();
      expect(screen.getByText('+79990001122')).toBeInTheDocument();
      expect(screen.queryByText(/уйти без сохранения/i)).not.toBeInTheDocument();
    }

    const dialog = openPersonal();
    expect(within(dialog).getByLabelText('Имя')).toHaveValue('Анна');
    expect(within(dialog).getByLabelText('Телефон')).toHaveValue('+79990001122');
  });

  it('ТК-9 ignores a second save and close until the profile response arrives', async () => {
    const gate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(gate.promise);
    renderProfile();
    openPersonal();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeDisabled();

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }));
    fireEvent.click(backdropOf(screen.getByRole('dialog', { name: 'Личные данные' })));
    fireEvent.keyDown(document, { key: 'Escape' });

    expect(updateProfile).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('dialog', { name: 'Личные данные' })).toBeInTheDocument();

    gate.resolve({ ...user, firstName: 'Мария' });
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument());
    expect(screen.getByText('Мария Смирнова')).toBeInTheDocument();
  });

  it('keeps the password mask out of the tab order', () => {
    renderProfile();
    const mask = within(screen.getByRole('region', { name: 'Вход в аккаунт' })).getByText('••••••••');
    expect(mask.textContent).toBe('••••••••');
    expect(mask.tagName).not.toBe('INPUT');
    expect(mask.getAttribute('tabindex')).toBeNull();
    fireEvent.click(mask);
    expect(document.activeElement).not.toBe(mask);
    expect(screen.queryByDisplayValue('••••••••')).not.toBeInTheDocument();
  });

  it('ТК-15 drops the account notice on refresh and on the next sign-in dialog', async () => {
    const changePassword = vi.spyOn(accountApi, 'changePassword').mockResolvedValue();
    const requestEmailChange = vi
      .spyOn(accountApi, 'requestEmailChange')
      .mockResolvedValue({ outcome: 'sent', message: ACCOUNT_TEXT.emailSent });
    const view = renderProfile();
    const account = screen.getByRole('region', { name: 'Вход в аккаунт' });

    fireEvent.click(within(account).getByRole('button', { name: 'Сменить пароль' }));
    const password = await screen.findByRole('dialog', { name: 'Сменить пароль' });
    fireEvent.change(within(password).getByLabelText('Текущий пароль'), { target: { value: 'OldPassword1' } });
    fireEvent.change(within(password).getByLabelText('Новый пароль'), { target: { value: 'NewPassword1' } });
    fireEvent.change(within(password).getByLabelText('Повторите пароль'), { target: { value: 'NewPassword1' } });
    fireEvent.click(within(password).getByRole('button', { name: 'Сменить пароль' }));
    expect(await within(account).findByText(ACCOUNT_TEXT.passwordChanged)).toBeInTheDocument();
    expect(changePassword).toHaveBeenCalledTimes(1);
    expect(within(account).getByText('••••••••').textContent).toHaveLength(8);

    view.unmount();
    renderProfile();
    const fresh = screen.getByRole('region', { name: 'Вход в аккаунт' });
    expect(within(fresh).queryByText(ACCOUNT_TEXT.passwordChanged)).not.toBeInTheDocument();
    expect(within(fresh).getByText(user.email)).toBeInTheDocument();
    expect(within(fresh).getByText('••••••••').textContent).toHaveLength(8);

    fireEvent.click(within(fresh).getByRole('button', { name: 'Сменить пароль' }));
    expect(within(fresh).queryByText(ACCOUNT_TEXT.passwordChanged)).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    expect(within(fresh).queryByRole('button', { name: 'Сменить email' })).not.toBeInTheDocument();
    expect(within(fresh).getByText(user.email)).toBeInTheDocument();
    expect(requestEmailChange).not.toHaveBeenCalled();
  });

  it('ТК-17 opens only one window and does not publish the draft', async () => {
    signIn({ ...user, role: 'manager' });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const header = renderPage(
      <>
        <Header />
        <ProfilePage />
      </>,
      '/dashboard/profile',
    );

    const dialog = openPersonal();
    fireEvent.change(within(dialog).getByLabelText('Имя'), { target: { value: 'Черновик' } });
    expect(screen.getByRole('button', { name: /Анна/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Черновик/ })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Сменить пароль' }));
    expect(screen.queryByRole('button', { name: 'Сменить email' })).not.toBeInTheDocument();
    const input = document.querySelector('input[type="file"]');
    if (!(input instanceof HTMLInputElement)) throw new Error('file input missing');
    fireEvent.change(input, { target: { files: [new File(['x'], 'face.jpg', { type: 'image/jpeg' })] } });

    expect(screen.getAllByRole('dialog')).toHaveLength(1);
    expect(screen.getByRole('dialog', { name: 'Личные данные' })).toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Карточка менеджера' })).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: /Анна/ })).toBeInTheDocument();
    expect(screen.queryByText('Черновик')).not.toBeInTheDocument();
    header.unmount();
  });
});
