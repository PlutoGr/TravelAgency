import { useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { User as UserIcon } from 'lucide-react';
import toast from 'react-hot-toast';
import ProfilePage from './ProfilePage';
import * as authApi from '@/api/auth';
import { apiClient, setAuthSessionHandler } from '@/api/client';
import { clearSessionSignal, markSessionPresent } from '@/api/sessionSignal';
import Header from '@/components/layout/Header';
import Sidebar from '@/components/layout/Sidebar';
import { AVATAR_MAX_BYTES, PASSWORD_HINT } from '@/domain/profileForm';
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

const MEDIA = '/api/v1/media/files/11111111-1111-1111-1111-111111111111/a256';

const PHONE_E164_ERROR =
  'Телефон должен быть в формате E.164, например +79991234567, или пустым.';

const managerUser: User = {
  ...user,
  role: 'manager',
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

function renderShell(path = '/dashboard/profile') {
  return renderPage(
    <>
      <LocationProbe />
      <Header />
      <Sidebar
        items={[{ icon: UserIcon, label: 'Профиль', path: '/dashboard/profile' }]}
        title="Личный кабинет"
      />
      <ProfilePage />
    </>,
    path,
  );
}

function setField(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

function fileInput(): HTMLInputElement {
  const input = document.querySelector('input[type="file"]');
  if (!(input instanceof HTMLInputElement)) throw new Error('file input missing');
  return input;
}

function chooseFile(file: File) {
  fireEvent.change(fileInput(), { target: { files: [file] } });
}

function imageFile(name: string, type: string, size = 8): File {
  const file = new File([Uint8Array.from({ length: Math.min(size, 16) }, () => 1)], name, { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => {
    resolve = res;
  });
  return { promise, resolve };
}

function httpResponse(
  config: InternalAxiosRequestConfig,
  status: number,
  data: unknown,
): AxiosResponse {
  return { data, status, statusText: String(status), headers: {}, config };
}

function httpError(config: InternalAxiosRequestConfig, status: number, data: unknown = {}): AxiosError {
  return new AxiosError(
    'request failed',
    String(status),
    config,
    null,
    httpResponse(config, status, data),
  );
}

function contentType(config: InternalAxiosRequestConfig): string {
  const headers = config.headers;
  if (headers && typeof headers.get === 'function') {
    const value = headers.get('Content-Type');
    return value == null ? '' : String(value);
  }
  return '';
}

const profileDto = {
  id: user.id,
  email: user.email,
  firstName: user.firstName,
  lastName: user.lastName,
  phone: user.phone,
  role: 'Client',
  createdAt: user.createdAt,
  avatarUrl: null as string | null,
};

describe('ProfilePage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
  });

  
  it('ТК-2 saves trimmed card fields once and updates the session, header and sidebar', async () => {
    const gate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(gate.promise);
    const getMe = vi.spyOn(authApi, 'getMe');
    renderShell();

    setField('Имя', '  Мария  ');
    setField('Фамилия', '  Ковалева  ');
    setField('Телефон', '+79991234567');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeDisabled();
    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991234567',
    });
    expect(Object.keys(updateProfile.mock.calls[0][0]).sort()).toEqual([
      'firstName',
      'lastName',
      'phone',
    ]);

    gate.resolve({
      ...user,
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991234567',
    });

    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Ковалева',
    });
    expect(screen.getAllByText('Мария Ковалева').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Мария')).toBeInTheDocument();
    expect(getMe).not.toHaveBeenCalled();
  });

  it('ТК-3 sends empty phone and city and shows empty fields when the profile comes back blank', async () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockResolvedValue({
      ...user,
      phone: '',
    });
    renderProfile();
    setField('Телефон', '   ');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '',
    });
    await waitFor(() => {
      expect(screen.getByLabelText('Телефон')).toHaveValue('');
      expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    });
  });

  it('ТК-4 blocks save when the name or last name is empty or longer than 100 characters', () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();

    setField('Имя', '   ');
    setField('Фамилия', 'Смирнова');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(updateProfile).not.toHaveBeenCalled();
    expect(screen.getByText('Укажите имя длиной от 1 до 100 символов.')).toBeInTheDocument();
    expect(screen.queryByText('Укажите фамилию длиной от 1 до 100 символов.')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();

    setField('Имя', 'А');
    setField('Фамилия', ` ${'Я'.repeat(101)} `);
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    expect(updateProfile).not.toHaveBeenCalled();
    expect(screen.queryByText('Укажите имя длиной от 1 до 100 символов.')).not.toBeInTheDocument();
    expect(screen.getByText('Укажите фамилию длиной от 1 до 100 символов.')).toBeInTheDocument();

    setField('Имя', '');
    setField('Фамилия', 'Я'.repeat(101));
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    expect(screen.getByText('Укажите имя длиной от 1 до 100 символов.')).toBeInTheDocument();
    expect(screen.getByText('Укажите фамилию длиной от 1 до 100 символов.')).toBeInTheDocument();
    expect(updateProfile).not.toHaveBeenCalled();
  });

  it('ТК-5 blocks a phone that is not E.164', () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();

    for (const phone of ['89991234567', '+7 999 123-45-67']) {
      setField('Телефон', phone);
      fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
      expect(updateProfile).not.toHaveBeenCalled();
      expect(
        screen.getByText('Телефон должен быть в формате E.164, например +79991234567, или пустым.'),
      ).toBeInTheDocument();
    }
  });

  
  it('ТК-7 shows Russian field errors and hides the English detail', async () => {
    vi.spyOn(authApi, 'updateProfile').mockRejectedValue({
      response: {
        status: 400,
        data: {
          detail: 'Phone number format is invalid.',
          errors: {
            firstName: ['Укажите имя длиной от 1 до 100 символов.'],
            phone: ['Телефон должен быть в формате E.164, например +79991234567, или пустым.'],
          },
        },
      },
    });
    renderProfile();
    setField('Имя', 'Анна');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText('Укажите имя длиной от 1 до 100 символов.')).toBeInTheDocument();
    expect(
      screen.getByText('Телефон должен быть в формате E.164, например +79991234567, или пустым.'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Phone number format is invalid.')).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79990001122');
  });

  it('ТК-8 keeps the entered card when save fails without a Russian field error', async () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    updateProfile.mockRejectedValueOnce(new Error('network'));
    renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Не удалось выполнить запрос. Попробуйте ещё раз.',
    );
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
    expect(toast.success).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();

    updateProfile.mockRejectedValueOnce({
      response: {
        status: 400,
        data: {
          code: 'NotAKnownCode',
          detail: 'Phone number format is invalid.',
        },
      },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Не удалось выполнить запрос. Попробуйте ещё раз.',
    );
    expect(screen.queryByText('Phone number format is invalid.')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
    expect(toast.error).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
  });

  
  it('ТК-9 uploads or replaces a picture and refreshes the session without a second read', async () => {
    const gate = deferred<User>();
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(gate.promise);
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    const getMe = vi.spyOn(authApi, 'getMe');
    renderShell();

    const file = imageFile('face.jpg', 'image/jpeg');
    chooseFile(file);

    await waitFor(() => expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeDisabled());
    expect(uploadAvatar).toHaveBeenCalledWith(file);
    expect(deleteAvatar).not.toHaveBeenCalled();

    gate.resolve({ ...user, avatar: MEDIA });

    await waitFor(() => expect(screen.getByRole('button', { name: 'Заменить' })).toBeEnabled());
    expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeEnabled();
    for (const image of screen.getAllByRole('img', { name: 'Анна Смирнова' })) {
      expect(image).toHaveAttribute('src', MEDIA);
    }
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('blob.core');
    expect(useAuthStore.getState().user?.avatar).toBe(MEDIA);
    expect(getMe).not.toHaveBeenCalled();
    expect(toast.success).not.toHaveBeenCalled();

    const replacement = imageFile('next.png', 'image/png');
    uploadAvatar.mockResolvedValue({ ...user, avatar: `${MEDIA}-2` });
    chooseFile(replacement);
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(2));
    expect(uploadAvatar).toHaveBeenLastCalledWith(replacement);
    expect(deleteAvatar).not.toHaveBeenCalled();
    expect(fileInput()).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp');
  });

  it('ТК-9 accepts webp up to 5 MiB and does not crop the file', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({
      ...user,
      avatar: MEDIA,
    });
    renderProfile();
    const webp = imageFile('face.webp', 'image/webp', AVATAR_MAX_BYTES);
    chooseFile(webp);
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledWith(webp));
    expect(screen.queryByText('Обрезать')).not.toBeInTheDocument();
  });

  it('ТК-10 deletes the photo and shows initials without a second read', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar').mockResolvedValue({
      ...user,
      avatar: undefined,
    });
    const getMe = vi.spyOn(authApi, 'getMe');
    renderShell();

    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));

    await waitFor(() => expect(deleteAvatar).toHaveBeenCalledTimes(1));
    expect(deleteAvatar).toHaveBeenCalledWith();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeEnabled());
    expect(screen.queryByRole('img', { name: 'Анна Смирнова' })).not.toBeInTheDocument();
    expect(screen.getAllByText('АС').length).toBeGreaterThanOrEqual(1);
    expect(useAuthStore.getState().user?.avatar).toBeUndefined();
    expect(getMe).not.toHaveBeenCalled();
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('keeps unsaved card edits when avatar upload or delete returns another profile', async () => {
    const uploadGate = deferred<User>();
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(uploadGate.promise);
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderShell();

    setField('Имя', 'Мария');
    setField('Фамилия', 'Ковалева');
    setField('Телефон', '+79991112233');
    chooseFile(imageFile('face.jpg', 'image/jpeg'));

    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    uploadGate.resolve({
      ...user,
      firstName: 'Старое',
      lastName: 'Имя',
      phone: '',
      avatar: MEDIA,
    });

    await waitFor(() => expect(screen.getByRole('button', { name: 'Заменить' })).toBeEnabled());
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    for (const image of screen.getAllByRole('img', { name: 'Анна Смирнова' })) {
      expect(image).toHaveAttribute('src', MEDIA);
    }
    expect(screen.getAllByText('Анна Смирнова').length).toBeGreaterThanOrEqual(1);
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });

    deleteAvatar.mockResolvedValue({
      ...user,
      firstName: 'Старое',
      lastName: 'Имя',
      phone: '+70000000000',
      avatar: undefined,
    });
    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeEnabled());
    expect(screen.queryByRole('img', { name: 'Анна Смирнова' })).not.toBeInTheDocument();
    expect(screen.getAllByText('АС').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
    });
    expect(useAuthStore.getState().user?.avatar).toBeUndefined();

    updateProfile.mockResolvedValue({
      ...user,
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
    });
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
    });
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Мария Ковалева' })).toBeInTheDocument());
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
  });

  it('keeps the edited card and the newer avatar when save and avatar overlap', async () => {
    const saveGate = deferred<User>();
    const uploadGate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(saveGate.promise);
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(uploadGate.promise);
    renderProfile();

    setField('Имя', 'Мария');
    setField('Фамилия', 'Ковалева');
    setField('Телефон', '+79991112233');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    chooseFile(imageFile('face.jpg', 'image/jpeg'));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(uploadAvatar).toHaveBeenCalledTimes(1);

    uploadGate.resolve({
      ...user,
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });
    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA),
    );
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      avatar: MEDIA,
    });

    saveGate.resolve({
      ...user,
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
      avatar: undefined,
    });
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Мария Ковалева' })).toHaveAttribute('src', MEDIA);
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
      avatar: MEDIA,
    });

    const lateAvatar = deferred<User>();
    uploadAvatar.mockReturnValue(lateAvatar.promise);
    chooseFile(imageFile('next.png', 'image/png'));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(2));
    lateAvatar.resolve({
      ...user,
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '',
      avatar: `${MEDIA}-2`,
    });

    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'Мария Ковалева' })).toHaveAttribute('src', `${MEDIA}-2`),
    );
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
      avatar: `${MEDIA}-2`,
    });
  });

  it('keeps the newer photo when the avatar request starts before save and returns first', async () => {
    const saveGate = deferred<User>();
    const uploadGate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(saveGate.promise);
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(uploadGate.promise);
    renderProfile();

    setField('Имя', 'Мария');
    setField('Фамилия', 'Ковалева');
    setField('Телефон', '+79991112233');
    chooseFile(imageFile('face.jpg', 'image/jpeg'));

    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));

    uploadGate.resolve({
      ...user,
      firstName: 'Старое',
      lastName: 'Имя',
      phone: '',
      avatar: MEDIA,
    });
    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA),
    );
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });

    saveGate.resolve({
      ...user,
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
      avatar: undefined,
    });
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Ковалева');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79991112233');
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Мария Ковалева' })).toHaveAttribute('src', MEDIA);
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Ковалева',
      phone: '+79991112233',
      avatar: MEDIA,
    });
  });

  it('ТК-11 rejects an empty file, a foreign type and a file over 5 MiB before upload', () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();

    chooseFile(imageFile('empty.jpg', 'image/jpeg', 0));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);

    chooseFile(imageFile('anim.gif', 'image/gif', 20));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');

    chooseFile(imageFile('big.jpg', 'image/jpeg', AVATAR_MAX_BYTES + 1));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Файл больше 5 МБ.');
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
  });

  it('ТК-12 shows the server file error and keeps the previous photo', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    uploadAvatar.mockRejectedValueOnce({
      response: {
        status: 400,
        data: {
          detail: 'File content is not an image.',
          errors: { file: ['Подходят JPG, PNG или WebP.'] },
        },
      },
    });
    renderProfile();
    chooseFile(imageFile('face.jpg', 'image/jpeg'));

    expect(await screen.findByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');
    expect(screen.queryByText('File content is not an image.')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);

    uploadAvatar.mockRejectedValueOnce({
      response: {
        status: 400,
        data: { detail: 'File content is not an image.' },
      },
    });
    chooseFile(imageFile('face.png', 'image/png'));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Не удалось загрузить аватар. Попробуйте ещё раз.',
    );
    expect(screen.queryByText('File content is not an image.')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
  });

  it('ТК-13 shows the avatar fallback when upload fails without a file error', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();

    for (const error of [
      new Error('network'),
      { response: { status: 400, data: { detail: 'Upload failed upstream.' } } },
      { response: { status: 403, data: { detail: 'Forbidden.' } } },
    ]) {
      uploadAvatar.mockRejectedValueOnce(error);
      chooseFile(imageFile('face.webp', 'image/webp'));
      expect(await screen.findByRole('alert')).toHaveTextContent(
        'Не удалось загрузить аватар. Попробуйте ещё раз.',
      );
      expect(screen.queryByText('Upload failed upstream.')).not.toBeInTheDocument();
      expect(screen.queryByText('Forbidden.')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Заменить' })).toBeEnabled();
      expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeEnabled();
    }
  });

  it('ТК-14 keeps the photo when delete fails', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    renderProfile();

    for (const error of [
      new Error('network'),
      { response: { status: 400, data: { detail: 'Cannot delete.' } } },
      { response: { status: 403, data: { detail: 'Forbidden.' } } },
    ]) {
      deleteAvatar.mockRejectedValueOnce(error);
      fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
      expect(await screen.findByRole('alert')).toHaveTextContent(
        'Не удалось выполнить запрос. Попробуйте ещё раз.',
      );
      expect(screen.queryByText('Cannot delete.')).not.toBeInTheDocument();
      expect(screen.queryByText('Forbidden.')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(useAuthStore.getState().user?.avatar).toBe(MEDIA);
      expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
      expect(toast.success).not.toHaveBeenCalled();
      expect(screen.getByRole('button', { name: 'Заменить' })).toBeEnabled();
      expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeEnabled();
    }
  });

  it('ТК-17 opens settings from the query and returns to the card without it', async () => {
    const view = renderProfile('/dashboard/profile?tab=settings');
    const crumbs = screen.getByRole('navigation', { name: 'Breadcrumb' });
    expect(within(crumbs).getByText('Личный кабинет')).toBeInTheDocument();
    expect(within(crumbs).getByText('Профиль')).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Пароль' })).getByLabelText('Текущий пароль')).toBeInTheDocument();
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile?tab=settings');

    await userEvent.click(screen.getByRole('button', { name: 'Профиль' }));
    await waitFor(() => {
      expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile');
    });
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(within(crumbs).getByText('Профиль')).toBeInTheDocument();
    view.unmount();

    renderProfile('/dashboard/profile');
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.queryByLabelText('Текущий пароль')).not.toBeInTheDocument();
    expect(screen.getByTestId('location').textContent).not.toContain(user.id);
  });

  it('ТК-18 keeps the password block active', async () => {
    renderProfile('/dashboard/profile?tab=settings');
    const password = screen.getByRole('region', { name: 'Пароль' });

    expect(within(password).getByLabelText('Текущий пароль')).toBeEnabled();
    expect(within(password).getByLabelText('Новый пароль')).toBeEnabled();
    expect(within(password).getByLabelText('Повторите пароль')).toBeEnabled();
    expect(within(password).getByRole('button', { name: 'Сменить пароль' })).toBeEnabled();
    expect(within(password).queryByText('Появится позже')).not.toBeInTheDocument();
    expect(within(password).getByText(PASSWORD_HINT)).toBeInTheDocument();
    expect(within(password).getByLabelText('Новый пароль')).toHaveAccessibleDescription(PASSWORD_HINT);
    expect(within(password).queryByRole('link', { name: 'Забыли пароль?' })).not.toBeInTheDocument();
  });

  
  
  
  
  
  
  
  
  
  
  
  it('профиль менеджера ТК-11: неверный телефон блокирует сохранение целиком', () => {
    signIn(managerUser);
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();
    setField('Телефон', '89991234567');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(updateProfile).not.toHaveBeenCalled();
    expect(screen.getAllByText(PHONE_E164_ERROR)).toHaveLength(1);
    const phone = screen.getByLabelText('Телефон');
    expect(phone.parentElement?.parentElement).toHaveTextContent(PHONE_E164_ERROR);
    for (const label of ['О себе', 'В туризме с', 'Языки', 'Специализация', 'Город', 'Паспорт']) {
      expect(screen.queryByLabelText(label)).not.toBeInTheDocument();
    }
  });

  
  
  
  it('профиль менеджера ТК-16: смена пароля активна', async () => {
    signIn(managerUser);
    renderProfile('/dashboard/profile?tab=settings');
    const password = screen.getByRole('region', { name: 'Пароль' });

    expect(within(password).getByLabelText('Текущий пароль')).toBeEnabled();
    expect(within(password).getByRole('button', { name: 'Сменить пароль' })).toBeEnabled();
    expect(within(password).queryByText('Появится позже')).not.toBeInTheDocument();
    expect(within(password).getByText(PASSWORD_HINT)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Как меня видит клиент' })).not.toBeInTheDocument();
  });

  
  });

describe('ProfilePage session', () => {
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    clearSessionSignal();
    markSessionPresent();
    useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
    setAuthSessionHandler(() => {
      useAuthStore.getState().markSessionExpired();
    });
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
    clearSessionSignal();
    setAuthSessionHandler(null);
  });

  it('ТК-3 shows empty phone and city when the server returns null', async () => {
    apiClient.defaults.adapter = async (config) => {
      if ((config.url ?? '') === '/auth/me' && config.method === 'patch') {
        return httpResponse(config, 200, { ...profileDto, phone: null, city: null });
      }
      throw httpError(config, 404);
    };
    renderProfile();
    setField('Телефон', '   ');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(screen.getByLabelText('Телефон')).toHaveValue(''));
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({ phone: '' });
  });

  it('ТК-15 retries a profile save after refresh and treats the second 200 as success', async () => {
    let patches = 0;
    let refresh = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refresh += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url === '/auth/me' && config.method === 'patch') {
        patches += 1;
        if (patches === 1) throw httpError(config, 401);
        return httpResponse(config, 200, {
          ...profileDto,
          firstName: 'Мария',
          lastName: 'Ковалева',
        });
      }
      throw httpError(config, 404);
    };

    renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    expect(refresh).toBe(1);
    expect(patches).toBe(2);
    expect(useAuthStore.getState().user).toMatchObject({ firstName: 'Мария' });
    expect(screen.getByRole('heading', { name: 'Мария Ковалева' })).toBeInTheDocument();
  });

  it('ТК-15 retries an avatar upload after refresh', async () => {
    let posts = 0;
    let refresh = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refresh += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url === '/auth/me/avatar' && config.method === 'post') {
        posts += 1;
        if (posts === 1) throw httpError(config, 401);
        expect(contentType(config).toLowerCase()).not.toContain('application/json');
        return httpResponse(config, 200, { ...profileDto, avatarUrl: MEDIA });
      }
      throw httpError(config, 404);
    };

    renderProfile();
    chooseFile(imageFile('face.jpg', 'image/jpeg'));

    await waitFor(() => expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA));
    expect(refresh).toBe(1);
    expect(posts).toBe(2);
    expect(useAuthStore.getState().user?.avatar).toBe(MEDIA);
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('ТК-15 does not show another person when refresh fails', async () => {
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) throw httpError(config, 401);
      if (url.includes('/auth/me')) {
        if (config.method === 'patch' || config.method === 'post') {
          throw httpError(config, 401);
        }
        return httpResponse(config, 200, {
          ...profileDto,
          id: 'other-user',
          firstName: 'Чужой',
          lastName: 'Профиль',
        });
      }
      throw httpError(config, 404);
    };

    renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(useAuthStore.getState().user).toBeNull());
    expect(screen.queryByText('Чужой')).not.toBeInTheDocument();
    expect(screen.queryByText('Чужой Профиль')).not.toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('ТК-16 calls only the current session profile and avatar routes', async () => {
    const calls: string[] = [];
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      const method = (config.method ?? '').toLowerCase();
      calls.push(`${method} ${url}`);
      expect(url).not.toContain(user.id);
      expect(config.baseURL ?? '').not.toContain(user.id);
      if (url === '/auth/me/avatar' && method === 'post') {
        expect(contentType(config).toLowerCase()).not.toContain('application/json');
        expect(config.data).toBeInstanceOf(FormData);
        return httpResponse(config, 200, { ...profileDto, avatarUrl: MEDIA });
      }
      if (url === '/auth/me/avatar' && method === 'delete') {
        return httpResponse(config, 200, { ...profileDto, avatarUrl: null });
      }
      if (url === '/auth/me' && method === 'patch') {
        return httpResponse(config, 200, { ...profileDto });
      }
      throw httpError(config, 404);
    };

    const settings = renderProfile('/dashboard/profile?tab=settings');
    expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile?tab=settings');
    expect(screen.getByTestId('location').textContent).not.toContain(user.id);
    settings.unmount();

    renderProfile('/dashboard/profile');
    expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(calls).toContain('patch /auth/me'));
    chooseFile(imageFile('face.jpg', 'image/jpeg'));
    await waitFor(() => expect(calls).toContain('post /auth/me/avatar'));
    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
    await waitFor(() => expect(calls).toContain('delete /auth/me/avatar'));

    expect(calls.every((call) => call.endsWith('/auth/me') || call.endsWith('/auth/me/avatar'))).toBe(true);
    expect(calls.some((call) => call.includes(user.id))).toBe(false);
  });

  
  it('профиль менеджера ТК-19: неудачный refresh не показывает чужой профиль', async () => {
    signIn(managerUser);
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) throw httpError(config, 401);
      if (url.includes('/auth/me')) {
        if (config.method === 'patch' || config.method === 'post') {
          throw httpError(config, 401);
        }
        return httpResponse(config, 200, {
          ...profileDto,
          id: 'other-user',
          firstName: 'Чужой',
          lastName: 'Профиль',
          role: 'Manager',
        });
      }
      throw httpError(config, 404);
    };

    renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(useAuthStore.getState().user).toBeNull());
    expect(screen.queryByText('Чужой')).not.toBeInTheDocument();
    expect(screen.queryByText('Чужой текст')).not.toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
  });
});
