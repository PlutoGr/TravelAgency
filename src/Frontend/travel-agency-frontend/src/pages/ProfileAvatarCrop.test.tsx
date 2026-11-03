import { useLocation } from 'react-router-dom';
import { afterAll, beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { User as UserIcon } from 'lucide-react';
import toast from 'react-hot-toast';
import ProfilePage from './ProfilePage';
import * as authApi from '@/api/auth';
import { apiClient } from '@/api/client';
import { getMessages, subscribeToMessages } from '@/api/chat';
import Header from '@/components/layout/Header';
import Sidebar from '@/components/layout/Sidebar';
import ChatWindow from '@/components/booking/ChatWindow';
import ClientShortCard from '@/components/booking/ClientShortCard';
import ManagerShortCard from '@/components/booking/ManagerShortCard';
import { AVATAR_MAX_BYTES } from '@/domain/profileForm';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import { avatarDraws, clearAvatarDraws, installAvatarCanvas, setAvatarToBlob } from '@/test/avatarCanvas';
import type { ChatMessage, User } from '@/types';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

vi.mock('@/api/chat', () => ({
  getMessages: vi.fn(async () => []),
  sendMessage: vi.fn(),
  subscribeToMessages: vi.fn(() => () => {}),
  disconnectChat: vi.fn(),
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
const TYPE_ERROR = 'Подходят JPG, PNG или WebP.';
const SIZE_ERROR = 'Файл больше 5 МБ.';
const RATIO_ERROR = 'Фото слишком вытянутое. Длинная сторона не должна быть больше короткой более чем в 3 раза.';
const SAVE_ERROR = 'Не удалось загрузить аватар. Попробуйте ещё раз.';

const JPEG = [0xff, 0xd8, 0xff, 0x00];
const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const WEBP = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];

const restoreCanvas = installAvatarCanvas();

afterAll(() => {
  restoreCanvas();
});

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

function renderShell() {
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
    '/dashboard/profile',
  );
}

function fileOf(name: string, type: string, bytes: number[], size = bytes.length): File {
  const file = new File([Uint8Array.from(bytes)], name, { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

function jpegFile(name = 'face.jpg', size = 32): File {
  return fileOf(name, 'image/jpeg', JPEG, size);
}

function fileInput(): HTMLInputElement {
  const input = document.querySelector('input[type="file"]');
  if (!(input instanceof HTMLInputElement)) throw new Error('file input missing');
  return input;
}

function chooseFile(file: File) {
  fireEvent.change(fileInput(), { target: { files: [file] } });
}

function encodeDraws() {
  return avatarDraws().filter((draw) => draw.args.length >= 8);
}

function formSaveButton() {
  return screen.getByRole('button', { name: (name) => name === 'Сохранить' });
}

async function openCrop(file: File) {
  chooseFile(file);
  return screen.findByRole('dialog', { name: 'Обрезка фото' });
}

function saveButton(dialog: HTMLElement = screen.getByRole('dialog', { name: 'Обрезка фото' })) {
  return within(dialog).getByRole('button', { name: 'Сохранить фото' });
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

function bitmap(width: number, height: number) {
  return { width, height, close: () => {} };
}

beforeEach(() => {
  vi.restoreAllMocks();
  vi.clearAllMocks();
  clearAvatarDraws();
  setAvatarToBlob('jpeg');
  vi.stubGlobal('createImageBitmap', async () => bitmap(64, 64));
  useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
  vi.mocked(getMessages).mockResolvedValue([]);
  vi.mocked(subscribeToMessages).mockImplementation(() => () => {});
});

describe('Profile avatar crop', () => {
  it('ТК-1 opens the crop only for the last file that passes, for every role', async () => {
    const visits: Array<{ role: 'client' | 'manager' | 'admin'; avatar?: string; label: string; bytes: number[]; type: string }> = [
      { role: 'client', label: 'Загрузить фото', bytes: JPEG, type: 'image/jpeg' },
      { role: 'manager', avatar: MEDIA, label: 'Заменить', bytes: PNG, type: 'image/png' },
      { role: 'admin', label: 'Загрузить фото', bytes: WEBP, type: 'image/webp' },
    ];

    for (const visit of visits) {
      let release!: (value: ReturnType<typeof bitmap>) => void;
      const first = new Promise<ReturnType<typeof bitmap>>((resolve) => {
        release = resolve;
      });
      let calls = 0;
      vi.stubGlobal('createImageBitmap', () => {
        calls += 1;
        if (calls === 1) return first;
        return Promise.resolve(bitmap(150, 100));
      });
      useAuthStore.setState({
        user: { ...user, role: visit.role, avatar: visit.avatar },
        isAuthenticated: true,
        isLoading: false,
      });
      const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({
        ...user,
        role: visit.role,
        avatar: MEDIA,
      });
      uploadAvatar.mockClear();
      const view = renderShell();

      chooseFile(fileOf('first', visit.type, visit.bytes, AVATAR_MAX_BYTES));
      await waitFor(() => expect(screen.getByRole('button', { name: visit.label })).toBeDisabled());
      if (visit.avatar) {
        expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeDisabled();
      }
      expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();

      chooseFile(fileOf('second', visit.type, visit.bytes, AVATAR_MAX_BYTES));
      release(bitmap(10, 10));

      const dialog = await screen.findByRole('dialog', { name: 'Обрезка фото' });
      expect(uploadAvatar).not.toHaveBeenCalled();
      expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile');
      expect(within(dialog).getByText('Масштаб')).toBeInTheDocument();
      const cancel = within(dialog).getByRole('button', { name: 'Отмена' });
      const save = saveButton(dialog);
      expect(cancel.compareDocumentPosition(save) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
      expect(cancel.parentElement).toBe(save.parentElement);
      expect(fileInput()).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp');

      clearAvatarDraws();
      fireEvent.click(save);
      await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
      const sent = uploadAvatar.mock.calls[0][0] as File;
      expect(sent.name).toBe('avatar.jpg');
      expect(sent.type).toBe('image/jpeg');
      expect(encodeDraws()[0]?.args.slice(0, 4)).toEqual([25, 0, 100, 100]);
      view.unmount();
    }
  });

  it('ТК-2 posts the chosen square and refreshes the session, header and menu', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    const getMe = vi.spyOn(authApi, 'getMe');
    const original = apiClient.defaults.adapter;
    apiClient.defaults.adapter = async (config) => {
      if ((config.url ?? '') === '/auth/me/avatar' && config.method === 'post') {
        const body = config.data as FormData;
        const posted = body.get('file');
        expect(posted).toBeInstanceOf(File);
        expect((posted as File).name).toBe('avatar.jpg');
        expect((posted as File).type).toBe('image/jpeg');
        return {
          data: {
            id: user.id,
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            phone: user.phone,
            role: 'Client',
            createdAt: user.createdAt,
            avatarUrl: MEDIA,
          },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        };
      }
      if (typeof original !== 'function') throw new Error(`unexpected ${config.method} ${config.url}`);
      return original(config);
    };

    try {
      renderShell();
      fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Мария' } });
      const dialog = await openCrop(jpegFile());
      const preview = within(dialog).getByTestId('avatar-crop-preview');
      Object.defineProperty(preview, 'clientWidth', { configurable: true, value: 320 });
      fireEvent.change(within(dialog).getByRole('slider', { name: 'Масштаб' }), { target: { value: '1000' } });
      fireEvent.pointerDown(preview, { clientX: 100, clientY: 300, pointerId: 1 });
      fireEvent.pointerMove(preview, { clientX: 100, clientY: 300 - 288, pointerId: 1 });
      fireEvent.pointerUp(preview, { pointerId: 1 });
      fireEvent.click(saveButton(dialog));

      await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument());
      expect(encodeDraws().at(-1)?.args.slice(0, 4)).toEqual([500, 2000, 1000, 1000]);
      expect(uploadAvatar).toHaveBeenCalledTimes(1);
      expect(deleteAvatar).not.toHaveBeenCalled();
      expect(updateProfile).not.toHaveBeenCalled();
      expect(getMe).not.toHaveBeenCalled();
      expect(toast.success).not.toHaveBeenCalled();
      expect(useAuthStore.getState().user?.avatar).toBe(MEDIA);
      expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
      for (const image of screen.getAllByRole('img', { name: 'Анна Смирнова' })) {
        expect(image).toHaveAttribute('src', MEDIA);
      }
    } finally {
      apiClient.defaults.adapter = original;
    }
  });

  it('ТК-3 saves the whole square when the circle is still the full frame', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({ ...user, avatar: MEDIA });
    renderProfile();
    await openCrop(jpegFile());
    fireEvent.click(saveButton());
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    expect(encodeDraws()[0]?.args.slice(0, 4)).toEqual([0, 0, 64, 64]);
    expect(useAuthStore.getState().user?.avatar).toBe(MEDIA);
  });

  it('ТК-4 closes without a request and keeps the draft and the focus in the dialog', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    renderProfile();
    fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Мария' } });
    const file = jpegFile();

    const closers = [
      () => fireEvent.click(screen.getByRole('button', { name: 'Отмена' })),
      () => fireEvent.click(screen.getByRole('button', { name: 'Закрыть' })),
      () => {
        const dialog = screen.getByRole('dialog', { name: 'Обрезка фото' });
        const backdrop = dialog.parentElement?.querySelector('.backdrop-blur-sm');
        if (!(backdrop instanceof HTMLElement)) throw new Error('backdrop missing');
        fireEvent.click(backdrop);
      },
      () => fireEvent.keyDown(document, { key: 'Escape' }),
    ];

    for (const close of closers) {
      await openCrop(file);
      expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile');
      close();
      await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument());
      expect(uploadAvatar).not.toHaveBeenCalled();
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    }

    const dialog = await openCrop(file);
    fireEvent.click(formSaveButton());
    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
    chooseFile(jpegFile('other.jpg'));
    expect(updateProfile).not.toHaveBeenCalled();
    expect(deleteAvatar).not.toHaveBeenCalled();
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('dialog', { name: 'Обрезка фото' })).toBe(dialog);

    const keyboard = userEvent.setup();
    for (let step = 0; step < 6; step += 1) {
      await keyboard.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
  });

  it('ТК-5 replaces the photo without deleting first, then delete shows initials', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({
      ...user,
      avatar: `${MEDIA}-2`,
    });
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar').mockResolvedValue({ ...user, avatar: undefined });
    const getMe = vi.spyOn(authApi, 'getMe');
    renderShell();

    await openCrop(jpegFile());
    fireEvent.click(saveButton());
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    expect(deleteAvatar).not.toHaveBeenCalled();
    await waitFor(() => {
      for (const image of screen.getAllByRole('img', { name: 'Анна Смирнова' })) {
        expect(image).toHaveAttribute('src', `${MEDIA}-2`);
      }
    });

    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
    await waitFor(() => expect(deleteAvatar).toHaveBeenCalledTimes(1));
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('img', { name: 'Анна Смирнова' })).not.toBeInTheDocument();
    expect(screen.getAllByText('АС').length).toBeGreaterThanOrEqual(2);
    expect(document.body.textContent).not.toContain('нет фото');
    expect(getMe).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeEnabled();
  });

  it('ТК-6 keeps the previous photo until a readable file clears the phrase', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();
    const bad = [
      fileOf('empty.jpg', 'image/jpeg', JPEG, 0),
      fileOf('doc.pdf', 'application/pdf', [0x25, 0x50, 0x44, 0x46], 40),
      fileOf('clip.mp4', 'video/mp4', [0, 0, 0, 1], 40),
      fileOf('anim.gif', 'image/gif', [0x47, 0x49, 0x46], 40),
      fileOf('phone.heic', 'image/heic', [0, 0, 0, 1], 40),
      fileOf('note.txt', 'text/plain', [1, 2, 3], 40),
      fileOf('wrong.jpg', 'image/jpeg', PNG, 40),
      fileOf('wrong.png', 'image/png', JPEG, 40),
      fileOf('wrong.webp', 'image/webp', JPEG, 40),
    ];
    for (const file of bad) {
      chooseFile(file);
      expect(await screen.findByRole('alert')).toHaveTextContent(TYPE_ERROR);
      expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
      expect(uploadAvatar).not.toHaveBeenCalled();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
    }

    vi.stubGlobal('createImageBitmap', async () => {
      throw new Error('broken');
    });
    chooseFile(jpegFile('broken.jpg'));
    expect(await screen.findByRole('alert')).toHaveTextContent(TYPE_ERROR);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    vi.stubGlobal('createImageBitmap', async () => bitmap(64, 64));
    await openCrop(jpegFile('ok.jpg'));
    expect(screen.queryByText(TYPE_ERROR)).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();
  });

  it('ТК-7 checks the type before the 5 MiB limit', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();

    chooseFile(jpegFile('big.jpg', AVATAR_MAX_BYTES + 1));
    expect(await screen.findByRole('alert')).toHaveTextContent(SIZE_ERROR);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    chooseFile(fileOf('big.pdf', 'application/pdf', [0x25, 0x50, 0x44, 0x46], AVATAR_MAX_BYTES + 1));
    expect(await screen.findByRole('alert')).toHaveTextContent(TYPE_ERROR);
    expect(screen.queryByText(SIZE_ERROR)).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
  });

  it('ТК-8 rejects 3001×1000 and opens 3000×1000', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    let size = { width: 3001, height: 1000 };
    vi.stubGlobal('createImageBitmap', async () => bitmap(size.width, size.height));
    renderProfile();

    chooseFile(jpegFile('long.jpg'));
    expect(await screen.findByRole('alert')).toHaveTextContent(RATIO_ERROR);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);

    size = { width: 3000, height: 1000 };
    await openCrop(jpegFile('fit.jpg'));
    expect(screen.queryByText(RATIO_ERROR)).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();
  });

  it('ТК-10 and ТК-11 move the frame inside the photo and stop the slider at 64 or the short side', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({ ...user, avatar: MEDIA });
    const view = renderProfile();
    let dialog = await openCrop(jpegFile());
    let slider = within(dialog).getByRole('slider', { name: 'Масштаб' });
    expect(slider).toHaveAttribute('min', '0');
    expect(slider).toHaveValue('0');
    fireEvent.change(slider, { target: { value: '1936' } });
    clearAvatarDraws();
    fireEvent.click(saveButton(dialog));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    expect(encodeDraws()[0]?.args.slice(2, 4)).toEqual([64, 64]);
    view.unmount();

    vi.stubGlobal('createImageBitmap', async () => bitmap(40, 80));
    uploadAvatar.mockClear();
    renderProfile();
    dialog = await openCrop(jpegFile('tiny.jpg'));
    slider = within(dialog).getByRole('slider', { name: 'Масштаб' });
    expect(slider).toBeEnabled();
    expect(slider).toHaveAttribute('max', '0');
    expect(saveButton(dialog)).toBeEnabled();
    const preview = within(dialog).getByTestId('avatar-crop-preview');
    Object.defineProperty(preview, 'clientWidth', { configurable: true, value: 320 });
    fireEvent.pointerDown(preview, { clientX: 40, clientY: 40, pointerId: 1 });
    fireEvent.pointerMove(preview, { clientX: 90, clientY: 40, pointerId: 1 });
    clearAvatarDraws();
    fireEvent.click(saveButton(dialog));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));
    expect(encodeDraws()[0]?.args.slice(0, 4)).toEqual([0, 20, 40, 40]);
  });

  it('ТК-12 keeps the circle while the request is open and repeats the same square', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const gate = deferred<User>();
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(gate.promise);
    renderProfile();
    const dialog = await openCrop(jpegFile());
    const preview = within(dialog).getByTestId('avatar-crop-preview');
    Object.defineProperty(preview, 'clientWidth', { configurable: true, value: 320 });
    fireEvent.click(saveButton(dialog));
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(1));

    expect(saveButton(dialog)).toBeDisabled();
    expect(within(dialog).getByRole('button', { name: 'Отмена' })).toBeDisabled();
    expect(within(dialog).getByRole('slider', { name: 'Масштаб' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Закрыть' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Заменить' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeDisabled();
    fireEvent.change(within(dialog).getByRole('slider', { name: 'Масштаб' }), { target: { value: '10' } });
    fireEvent.pointerDown(preview, { clientX: 10, clientY: 10, pointerId: 1 });
    fireEvent.pointerMove(preview, { clientX: 80, clientY: 120, pointerId: 1 });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Отмена' }));
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }));
    fireEvent.keyDown(document, { key: 'Escape' });
    const backdrop = dialog.parentElement?.querySelector('.backdrop-blur-sm');
    if (backdrop instanceof HTMLElement) fireEvent.click(backdrop);
    expect(screen.getByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();

    const first = encodeDraws().at(-1)?.args.slice(0, 4);
    gate.reject({ response: { status: 403, data: { detail: 'Forbidden.' } } });
    expect(await screen.findByRole('alert')).toHaveTextContent(SAVE_ERROR);
    expect(screen.queryByText('Forbidden.')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
    expect(saveButton()).toBeEnabled();

    uploadAvatar.mockRejectedValueOnce(new Error('network'));
    clearAvatarDraws();
    fireEvent.click(saveButton());
    await waitFor(() => expect(uploadAvatar).toHaveBeenCalledTimes(2));
    expect(encodeDraws()[0]?.args.slice(0, 4)).toEqual(first);
    expect(await screen.findByRole('alert')).toHaveTextContent(SAVE_ERROR);

    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.queryByText(SAVE_ERROR)).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
  });

  it('ТК-12 shows the fallback when toBlob returns nothing', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    setAvatarToBlob('empty');
    renderProfile();
    await openCrop(jpegFile());
    fireEvent.click(saveButton());
    expect(await screen.findByRole('alert')).toHaveTextContent(SAVE_ERROR);
    expect(uploadAvatar).not.toHaveBeenCalled();
    expect(screen.getByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();
  });

  it('ТК-13 shows the cyrillic file phrase inside the dialog', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const bodies = [
      {
        detail: 'File content is not an image.',
        errors: { file: ['Подходят JPG, PNG или WebP.'] },
        shown: TYPE_ERROR,
      },
      {
        errors: { SizeBytes: ['Файл больше 5 МБ.'] },
        shown: SIZE_ERROR,
      },
      { detail: 'Upload failed upstream.', shown: SAVE_ERROR },
    ];
    renderProfile();
    await openCrop(jpegFile());
    for (const body of bodies) {
      uploadAvatar.mockRejectedValueOnce({ response: { status: 400, data: body } });
      fireEvent.click(saveButton());
      const alert = await screen.findByRole('alert');
      expect(alert).toHaveTextContent(body.shown);
      expect(alert.compareDocumentPosition(saveButton()) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
      expect(screen.queryByText('File content is not an image.')).not.toBeInTheDocument();
      expect(screen.queryByText('Upload failed upstream.')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
    }
  });

  it('ТК-14 stores only the session a256 and does not pass a separate frame into the chat', async () => {
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar').mockResolvedValue({ ...user, avatar: MEDIA });
    const view = renderProfile();
    await openCrop(jpegFile());
    fireEvent.click(saveButton());
    await waitFor(() => expect(useAuthStore.getState().user?.avatar).toBe(MEDIA));
    const sent = uploadAvatar.mock.calls[0][0] as File;
    expect(sent.name).toBe('avatar.jpg');
    expect(sent.type).toBe('image/jpeg');
    view.unmount();

    const feed: ChatMessage[] = [
      {
        id: 'own',
        bookingId: 'booking-1',
        senderId: user.id,
        senderName: 'Анна Смирнова',
        senderRole: 'client',
        text: 'своё',
        createdAt: '2026-04-02T08:00:00Z',
      },
      {
        id: 'system',
        bookingId: 'booking-1',
        senderId: 'system',
        senderName: 'Сервис',
        senderRole: 'system',
        text: 'служебное',
        createdAt: '2026-04-02T08:01:00Z',
      },
    ];
    expect(feed.every((message) => !('file' in message))).toBe(true);
    vi.mocked(getMessages).mockResolvedValue(feed);
    renderPage(
      <div className="h-[480px]">
        <ChatWindow
          bookingId="booking-1"
          currentUserId={user.id}
          currentUserName="Анна Смирнова"
          currentUserRole="client"
          currentUserAvatar={useAuthStore.getState().user?.avatar}
        />
      </div>,
    );

    const list = await screen.findByTestId('chat-message-list');
    expect(await within(list).findByText('своё')).toBeInTheDocument();
    const images = within(list).getAllByRole('img');
    expect(images).toHaveLength(1);
    expect(images[0]).toHaveAttribute('src', MEDIA);
    expect(images[0].getAttribute('src')).not.toMatch(/^blob:/);
    expect(within(list).getByText('служебное')).toBeInTheDocument();
    expect(within(list).getByText('С')).toBeInTheDocument();
  });

  it('ТК-15 does not mount the crop on settings or on someone else card', async () => {
    const view = renderProfile('/dashboard/profile?tab=settings');
    expect(screen.queryByRole('button', { name: 'Загрузить фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Заменить' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(document.querySelector('input[type="file"]')).toBeNull();
    view.unmount();

    const manager = renderPage(
      <ManagerShortCard
        userId="other-manager"
        onClose={() => {}}
        loadCard={async () => ({ id: 'other-manager', displayName: 'Иван Петров' })}
      />,
    );
    expect(await screen.findByText('Иван Петров')).toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Загрузить фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Заменить' })).not.toBeInTheDocument();
    manager.unmount();


    renderPage(
      <ClientShortCard
        clientId="other-client"
        onClose={() => {}}
        loadCard={async () => ({
          id: 'other-client',
          displayName: 'Ольга Соколова',
          email: 'olga@example.com',
          bookings: [],
          notes: [],
        })}
      />,
    );
    expect(await screen.findByText('Ольга Соколова')).toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Загрузить фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Заменить' })).not.toBeInTheDocument();
  });
});
