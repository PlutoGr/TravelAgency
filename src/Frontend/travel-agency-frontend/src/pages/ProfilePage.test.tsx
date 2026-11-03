import { useLocation } from 'react-router-dom';
import { afterAll, afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
import { AVATAR_MAX_BYTES } from '@/domain/profileForm';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import { clearAvatarDraws, installAvatarCanvas, setAvatarToBlob } from '@/test/avatarCanvas';
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

function openPersonal() {
  fireEvent.click(screen.getByRole('button', { name: 'Редактировать' }));
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

const JPEG_MAGIC = [0xff, 0xd8, 0xff, 0x00];
const PNG_MAGIC = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const WEBP_MAGIC = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];

function imageFile(name: string, type: string, size = 8): File {
  const bytes = type === 'image/png' ? PNG_MAGIC : type === 'image/webp' ? WEBP_MAGIC : JPEG_MAGIC;
  const file = new File([Uint8Array.from(bytes)], name, { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

async function chooseAndSave(file: File) {
  chooseFile(file);
  const dialog = await screen.findByRole('dialog', { name: 'Обрезка фото' });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Сохранить фото' }));
}

const restoreAvatarCanvas = installAvatarCanvas();

afterAll(() => {
  restoreAvatarCanvas();
});

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
    clearAvatarDraws();
    setAvatarToBlob('jpeg');
    vi.stubGlobal('createImageBitmap', async () => ({ width: 64, height: 64, close: () => {} }));
    useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
  });

  
  it('ТК-2 saves trimmed card fields once and updates the session, header and sidebar', async () => {
    const gate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(gate.promise);
    const getMe = vi.spyOn(authApi, 'getMe');
    renderShell();

    openPersonal();
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
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument());
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
    openPersonal();
    setField('Телефон', '   ');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(updateProfile).toHaveBeenCalledWith({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '',
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument();
    });
    const personal = screen.getByRole('region', { name: 'Личные данные' });
    expect(within(personal).getByText('Телефон')).toBeInTheDocument();
    expect(within(personal).queryByText('+79990001122')).not.toBeInTheDocument();
    expect(within(personal).queryByText('не указано')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
  });

  it('ТК-4 blocks save when the name or last name is empty or longer than 100 characters', () => {
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();
    openPersonal();

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
    openPersonal();

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
    openPersonal();
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
    openPersonal();
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
    await chooseAndSave(file);

    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    const sent = uploadAvatar.mock.calls[0][0] as File;
    expect(sent.name).toBe('avatar.jpg');
    expect(sent.type).toBe('image/jpeg');
    expect(sent).not.toBe(file);
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
    await chooseAndSave(replacement);
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(2));
    const resent = uploadAvatar.mock.calls[1][0] as File;
    expect(resent.name).toBe('avatar.jpg');
    expect(resent.type).toBe('image/jpeg');
    expect(resent).not.toBe(replacement);
    expect(deleteAvatar).not.toHaveBeenCalled();
    expect(fileInput()).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp');
  });

  it('ТК-9 opens the crop for a 5 MiB webp and does not post the original file', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({
      ...user,
      avatar: MEDIA,
    });
    renderProfile();
    const webp = imageFile('face.webp', 'image/webp', AVATAR_MAX_BYTES);
    await chooseAndSave(webp);
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    const sent = uploadAvatar.mock.calls[0][0] as File;
    expect(sent).not.toBe(webp);
    expect(sent.name).toBe('avatar.jpg');
    expect(sent.type).toBe('image/jpeg');
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

  it('keeps the saved name when avatar upload or delete returns another profile', async () => {
    const uploadGate = deferred<User>();
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(uploadGate.promise);
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    renderShell();

    await chooseAndSave(imageFile('face.jpg', 'image/jpeg'));

    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    uploadGate.resolve({
      ...user,
      firstName: 'Старое',
      lastName: 'Имя',
      phone: '',
      avatar: MEDIA,
    });

    await waitFor(() => expect(screen.getByRole('button', { name: 'Заменить' })).toBeEnabled());
    expect(screen.queryByText('Старое Имя')).not.toBeInTheDocument();
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
    expect(screen.queryByText('Старое Имя')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
    });
    expect(useAuthStore.getState().user?.avatar).toBeUndefined();

    openPersonal();
    expect(screen.getByLabelText('Имя')).toHaveValue('Анна');
    expect(screen.getByLabelText('Фамилия')).toHaveValue('Смирнова');
    expect(screen.getByLabelText('Телефон')).toHaveValue('+79990001122');
  });

  it('does not open the crop while personal data is saving', async () => {
    const saveGate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile').mockReturnValue(saveGate.promise);
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    renderProfile();

    openPersonal();
    setField('Имя', 'Мария');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    chooseFile(imageFile('face.jpg', 'image/jpeg'));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'Личные данные' })).toBeInTheDocument();

    saveGate.resolve({
      ...user,
      firstName: 'Мария',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument());
    expect(screen.getByRole('img', { name: 'Мария Смирнова' })).toHaveAttribute('src', MEDIA);
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });

    const lateAvatar = deferred<User>();
    uploadAvatar.mockReturnValue(lateAvatar.promise);
    await chooseAndSave(imageFile('next.png', 'image/png'));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    lateAvatar.resolve({
      ...user,
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '',
      avatar: `${MEDIA}-2`,
    });

    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'Мария Смирнова' })).toHaveAttribute('src', `${MEDIA}-2`),
    );
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Мария',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: `${MEDIA}-2`,
    });
  });

  it('does not open personal data while the crop is open', async () => {
    const uploadGate = deferred<User>();
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(uploadGate.promise);
    renderProfile();

    chooseFile(imageFile('face.jpg', 'image/jpeg'));
    expect(await screen.findByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Редактировать' }));
    expect(screen.queryByRole('dialog', { name: 'Личные данные' })).not.toBeInTheDocument();
    expect(updateProfile).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить фото' }));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
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
    expect(screen.queryByText('Старое Имя')).not.toBeInTheDocument();
    expect(useAuthStore.getState().user).toMatchObject({
      firstName: 'Анна',
      lastName: 'Смирнова',
      phone: '+79990001122',
      avatar: MEDIA,
    });
  });

  it('ТК-11 rejects an empty file, a foreign type and a file over 5 MiB before upload', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();

    chooseFile(imageFile('empty.jpg', 'image/jpeg', 0));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);

    chooseFile(imageFile('anim.gif', 'image/gif', 20));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');

    chooseFile(imageFile('big.jpg', 'image/jpeg', AVATAR_MAX_BYTES + 1));
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toHaveTextContent('Файл больше 5 МБ.');
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
    await chooseAndSave(imageFile('face.jpg', 'image/jpeg'));

    expect(await screen.findByRole('alert')).toHaveTextContent('Подходят JPG, PNG или WebP.');
    expect(screen.queryByText('File content is not an image.')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);

    uploadAvatar.mockRejectedValueOnce({
      response: {
        status: 400,
        data: { detail: 'File content is not an image.' },
      },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить фото' }));
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
    const errors = [
      new Error('network'),
      { response: { status: 400, data: { detail: 'Upload failed upstream.' } } },
      { response: { status: 403, data: { detail: 'Forbidden.' } } },
    ];
    renderProfile();
    uploadAvatar.mockRejectedValueOnce(errors[0]);
    await chooseAndSave(imageFile('face.webp', 'image/webp'));

    for (const error of errors) {
      if (error !== errors[0]) {
        uploadAvatar.mockRejectedValueOnce(error);
        fireEvent.click(screen.getByRole('button', { name: 'Сохранить фото' }));
      }
      expect(await screen.findByRole('alert')).toHaveTextContent(
        'Не удалось загрузить аватар. Попробуйте ещё раз.',
      );
      expect(screen.queryByText('Upload failed upstream.')).not.toBeInTheDocument();
      expect(screen.queryByText('Forbidden.')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Сохранить фото' })).toBeEnabled();
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

  it('shows the same screen for the old settings address', () => {
    renderProfile('/dashboard/profile?tab=settings');
    const crumbs = screen.getByRole('navigation', { name: 'Breadcrumb' });
    expect(within(crumbs).getByText('Личный кабинет')).toBeInTheDocument();
    expect(within(crumbs).getByText('Профиль')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1, name: 'Профиль' })).toBeInTheDocument();
    expect(screen.queryByRole('tab')).not.toBeInTheDocument();
    expect(screen.queryByText('Настройки')).not.toBeInTheDocument();
    expect(screen.queryByText('Смена адреса - на вкладке «Настройки».')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Вход в аккаунт' })).toHaveTextContent(user.email);
    expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile?tab=settings');
    expect(screen.queryByLabelText('Текущий пароль')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
  });

  
  
  
  
  
  
  
  
  
  
  
  it('профиль менеджера ТК-11: неверный телефон блокирует сохранение целиком', () => {
    signIn(managerUser);
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();
    openPersonal();
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

  
  
  
  it('показывает кнопку карточки менеджера на том же экране', () => {
    signIn(managerUser);
    renderProfile('/dashboard/profile?tab=settings');
    expect(screen.getByRole('button', { name: 'Как меня видит клиент' })).toBeInTheDocument();
    expect(screen.queryByRole('tab')).not.toBeInTheDocument();
    expect(screen.queryByText('Появится позже')).not.toBeInTheDocument();
  });

  
  });

describe('ProfilePage session', () => {
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    clearSessionSignal();
    markSessionPresent();
    clearAvatarDraws();
    setAvatarToBlob('jpeg');
    vi.stubGlobal('createImageBitmap', async () => ({ width: 64, height: 64, close: () => {} }));
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
    openPersonal();
    setField('Телефон', '   ');
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(useAuthStore.getState().user).toMatchObject({ phone: '' }));
    await waitFor(() => expect(screen.queryByLabelText('Телефон')).not.toBeInTheDocument());
    expect(screen.queryByLabelText('Город')).not.toBeInTheDocument();
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
    openPersonal();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Профиль успешно обновлён'));
    expect(refresh).toBe(1);
    expect(patches).toBe(2);
    expect(useAuthStore.getState().user).toMatchObject({ firstName: 'Мария' });
    expect(screen.getByText('Мария Ковалева')).toBeInTheDocument();
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
    await chooseAndSave(imageFile('face.jpg', 'image/jpeg'));

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
    openPersonal();
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
    openPersonal();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(calls).toContain('patch /auth/me'));
    await chooseAndSave(imageFile('face.jpg', 'image/jpeg'));
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
    openPersonal();
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(useAuthStore.getState().user).toBeNull());
    expect(screen.queryByText('Чужой')).not.toBeInTheDocument();
    expect(screen.queryByText('Чужой текст')).not.toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
  });
});
