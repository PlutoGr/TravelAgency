import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import ProfilePage from './ProfilePage';
import ManagerClientCardPage from './ManagerClientCardPage';
import * as authApi from '@/api/auth';
import { apiClient } from '@/api/client';
import { getClientCard } from '@/api/clientCards';
import ClientShortCard from '@/components/booking/ClientShortCard';
import ManagerShortCard from '@/components/booking/ManagerShortCard';
import Header from '@/components/layout/Header';
import Sidebar from '@/components/layout/Sidebar';
import { RangeSlider } from '@/components/ui';
import { User as UserIcon } from 'lucide-react';
import { AVATAR_OPEN_ERROR } from '@/domain/profileForm';
import { useAuthStore } from '@/store/authStore';
import { renderPage } from '@/test/render';
import { avatarDraws, clearAvatarDraws, installAvatarCanvas, setAvatarToBlob } from '@/test/avatarCanvas';
import type { User } from '@/types';

vi.mock('@/api/clientCards', () => ({
  getClientCard: vi.fn(),
  addClientNote: vi.fn(),
  clientBookingTitles: vi.fn(async () => new Map()),
  clientNoteFieldError: () => null,
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
const NEXT = '/api/v1/media/files/22222222-2222-2222-2222-222222222222/a256';
const OPEN_ERROR = 'Не удалось открыть фото. Попробуйте ещё раз.';
const TYPE_ERROR = 'Подходят JPG, PNG или WebP.';
const JPEG = [0xff, 0xd8, 0xff, 0x00];

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

function jpegFile(name = 'face.jpg'): File {
  return fileOf(name, 'image/jpeg', JPEG);
}

function fileInput(): HTMLInputElement {
  const input = document.querySelector('input[type="file"]');
  if (!(input instanceof HTMLInputElement)) throw new Error('file input missing');
  return input;
}

function chooseFile(file: File) {
  fireEvent.change(fileInput(), { target: { files: [file] } });
}

function bitmap(width: number, height: number, close: () => void = () => {}) {
  return { width, height, close };
}

function encodeDraws() {
  return avatarDraws().filter((draw) => draw.args.length >= 8);
}

function dialog() {
  return screen.getByRole('dialog', { name: 'Обрезка фото' });
}

function sliderOf(node: HTMLElement = dialog()) {
  return within(node).getByRole('slider', { name: 'Масштаб' }) as HTMLInputElement;
}

function previewOf(node: HTMLElement = dialog()) {
  const preview = within(node).getByTestId('avatar-crop-preview');
  Object.defineProperty(preview, 'clientWidth', { configurable: true, value: 320 });
  return preview;
}

function spin(target: Element, init: WheelEventInit = {}) {
  const event = new WheelEvent('wheel', {
    bubbles: true,
    cancelable: true,
    clientX: 160,
    clientY: 160,
    ...init,
  });
  const heard = vi.fn();
  document.addEventListener('wheel', heard);
  act(() => {
    target.dispatchEvent(event);
  });
  document.removeEventListener('wheel', heard);
  return { event, heard };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => {
    resolve = res;
  });
  return { promise, resolve };
}

function profileDto(avatarUrl: string | null, role = 'Client') {
  return {
    id: user.id,
    email: user.email,
    firstName: user.firstName,
    lastName: user.lastName,
    phone: user.phone,
    role,
    createdAt: user.createdAt,
    avatarUrl,
  };
}

beforeEach(() => {
  vi.restoreAllMocks();
  vi.clearAllMocks();
  clearAvatarDraws();
  setAvatarToBlob('jpeg');
  vi.unstubAllGlobals();
  vi.stubGlobal('createImageBitmap', async () => bitmap(64, 64));
  useAuthStore.setState({ user, isAuthenticated: true, isLoading: false });
});

describe('Avatar crop polish', () => {
  it('ТК-1 styles the scale and the ring for every entry and keeps the price slider', async () => {
    const listen = vi.spyOn(EventTarget.prototype, 'addEventListener');
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));

    async function expectLook(node: HTMLElement) {
      const scale = sliderOf(node);
      expect(scale).toHaveAttribute('type', 'range');
      expect(scale).toHaveAttribute('min', '0');
      expect(scale).toHaveAttribute('max', '1936');
      expect(scale).toHaveAttribute('step', '1');
      expect(scale).toHaveValue('0');
      expect(scale.className).toContain('appearance-none');
      expect(scale.className).toContain('h-2');
      expect(scale.className).toContain('rounded-full');
      expect(scale.className).toContain('cursor-pointer');
      expect(scale.className).not.toContain('cursor-grab');
      expect(scale.className).toContain('bg-white');
      expect(scale.className).toContain('border-primary');
      expect(scale.className).toContain('shadow-md');
      expect(scale.className).toContain('shadow-lg');
      expect(scale.className).toContain('ring-primary/30');
      expect(scale.className).toContain('disabled:opacity-40');
      expect(scale.className).toContain('h-5');
      const track = scale.getAttribute('style') ?? '';
      expect(track).toContain('rgb(44, 82, 130) 0%');
      expect(track).toContain('rgb(245, 240, 232) 0%');
      expect(within(node).getByText('Масштаб').textContent?.trim()).toBe('Масштаб');
      expect(within(node).queryByRole('button', { name: '+' })).not.toBeInTheDocument();
      expect(within(node).queryByRole('button', { name: '−' })).not.toBeInTheDocument();
      expect(within(node).queryByText(/%/)).not.toBeInTheDocument();

      const preview = previewOf(node);
      expect(preview.className).toContain('cursor-grab');
      expect(preview.className).toContain('bg-sand');
      expect(preview.className.split(/\s+/).some((name) => name.startsWith('border'))).toBe(false);
      fireEvent.pointerDown(preview, { clientX: 160, clientY: 160, pointerId: 1 });
      expect(preview.className).toContain('cursor-grabbing');
      fireEvent.pointerUp(preview, { pointerId: 1 });
      expect(preview.className).toContain('cursor-grab');
      expect(preview.className).not.toContain('cursor-grabbing');

      const ring = preview.querySelector('.border-warm-white');
      expect(ring).toBeInstanceOf(HTMLElement);
      expect(ring?.className).toContain('border-2');
      const ringStyle = ring?.getAttribute('style') ?? '';
      expect(ringStyle).toMatch(/1px (?:#1A1A2E|rgb\(26, 26, 46\))/);
      expect(ringStyle).toContain('rgba(20, 16, 12, 0.45)');
      expect(ringStyle).toContain('calc(100% - 32px)');
      expect(preview.querySelectorAll('[style*="9999px"]')).toHaveLength(1);

      scale.focus();
      fireEvent.change(scale, { target: { value: '1' } });
      expect(scale).toHaveValue('1');
    }

    const fresh = renderProfile();
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    chooseFile(jpegFile());
    await expectLook(await screen.findByRole('dialog', { name: 'Обрезка фото' }));
    const wheel = listen.mock.calls.find(
      (call) => call[0] === 'wheel' && (call[2] as AddEventListenerOptions | undefined)?.passive === false,
    );
    expect(wheel).toBeTruthy();
    fresh.unmount();

    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const replaced = renderProfile();
    chooseFile(jpegFile('next.jpg'));
    await expectLook(await screen.findByRole('dialog', { name: 'Обрезка фото' }));
    replaced.unmount();

    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response(Uint8Array.from([1, 2, 3, 4]), { status: 200 })),
    );
    const moved = renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
    await expectLook(await screen.findByRole('dialog', { name: 'Обрезка фото' }));
    moved.unmount();

    const filter = render(
      <RangeSlider min={0} max={100000} value={[10000, 80000]} onChange={() => {}} />,
    );
    expect(filter.getAllByRole('slider')).toHaveLength(2);
    expect(filter.getByText('10000')).toBeInTheDocument();
    expect(filter.getByText('80000')).toBeInTheDocument();
    filter.unmount();
  });

  it('ТК-2 turns one wheel click into the same diameter, including ctrl and a drag', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();
    chooseFile(jpegFile());
    const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const preview = previewOf(node);
    const first = spin(preview, { deltaY: -1, deltaMode: 1 });
    expect(first.event.defaultPrevented).toBe(true);
    expect(first.heard).not.toHaveBeenCalled();
    expect(sliderOf(node)).toHaveValue('193');
    const second = spin(preview, { deltaY: -1, deltaMode: 1 });
    expect(second.event.defaultPrevented).toBe(true);
    expect(sliderOf(node)).toHaveValue('387');
    expect(uploadAvatar).not.toHaveBeenCalled();

    fireEvent.click(within(node).getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    chooseFile(jpegFile('ctrl.jpg'));
    const ctrlDialog = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const ctrl = spin(previewOf(ctrlDialog), { deltaY: -100, deltaMode: 0, ctrlKey: true });
    expect(ctrl.event.defaultPrevented).toBe(true);
    expect(ctrl.heard).not.toHaveBeenCalled();
    expect(sliderOf(ctrlDialog)).toHaveValue('193');

    fireEvent.click(within(ctrlDialog).getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    chooseFile(jpegFile('drag.jpg'));
    const dragDialog = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const dragPreview = previewOf(dragDialog);
    fireEvent.pointerDown(dragPreview, { clientX: 160, clientY: 160, pointerId: 1 });
    fireEvent.pointerMove(dragPreview, { clientX: 200, clientY: 160, pointerId: 1 });
    const during = spin(dragPreview, { deltaY: -1, deltaMode: 1, clientX: 200, clientY: 160 });
    expect(during.event.defaultPrevented).toBe(true);
    expect(sliderOf(dragDialog)).toHaveValue('193');
    fireEvent.pointerMove(dragPreview, { clientX: 230, clientY: 160, pointerId: 1 });
    expect(sliderOf(dragDialog)).toHaveValue('193');
    expect(uploadAvatar).not.toHaveBeenCalled();
  });

  it('ТК-3 grows back to the short side and steps a 70 px frame by one pixel', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    const view = renderProfile();
    chooseFile(jpegFile());
    let node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    fireEvent.change(sliderOf(node), { target: { value: '193' } });
    expect(sliderOf(node)).toHaveValue('193');
    const preview = previewOf(node);
    const down = spin(preview, { deltaY: 1, deltaMode: 1 });
    expect(down.event.defaultPrevented).toBe(true);
    expect(sliderOf(node)).toHaveValue('0');
    spin(preview, { deltaY: 1, deltaMode: 1 });
    expect(sliderOf(node)).toHaveValue('0');
    view.unmount();

    vi.stubGlobal('createImageBitmap', async () => bitmap(70, 70));
    renderProfile();
    chooseFile(jpegFile('small.jpg'));
    node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const small = previewOf(node);
    expect(sliderOf(node)).toHaveAttribute('max', '6');
    spin(small, { deltaY: -1, deltaMode: 1 });
    expect(sliderOf(node)).toHaveValue('1');
    for (let i = 0; i < 8; i += 1) spin(small, { deltaY: -1, deltaMode: 1 });
    expect(sliderOf(node)).toHaveValue('6');
    const stuck = spin(small, { deltaY: -1, deltaMode: 1 });
    expect(stuck.event.defaultPrevented).toBe(true);
    expect(sliderOf(node)).toHaveValue('6');
  });

  it('ТК-4 leaves the diameter alone outside the circle', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    renderProfile();
    chooseFile(jpegFile());
    const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const preview = previewOf(node);
    const veil = spin(preview, { deltaY: -1, deltaMode: 1, clientX: 10, clientY: 10, ctrlKey: true });
    expect(veil.event.defaultPrevented).toBe(false);
    expect(veil.heard).toHaveBeenCalled();
    expect(sliderOf(node)).toHaveValue('0');

    for (const name of ['Отмена', 'Сохранить фото'] as const) {
      const button = within(node).getByRole('button', { name });
      const spun = spin(button, { deltaY: -100, deltaMode: 0, ctrlKey: true });
      expect(spun.event.defaultPrevented).toBe(false);
    }
    const onSlider = spin(sliderOf(node), { deltaY: -1, deltaMode: 1 });
    expect(onSlider.event.defaultPrevented).toBe(false);
    expect(sliderOf(node)).toHaveValue('0');
  });

  it('ТК-5 swallows the wheel when the frame cannot shrink or a save is in flight', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(40, 80));
    const view = renderProfile();
    chooseFile(jpegFile());
    let node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const tiny = previewOf(node);
    const scale = sliderOf(node);
    expect(scale).toHaveAttribute('max', '0');
    expect(scale).toHaveValue('0');
    expect(scale).toBeEnabled();
    const swallowed = spin(tiny, { deltaY: -1, deltaMode: 1 });
    expect(swallowed.event.defaultPrevented).toBe(true);
    expect(scale).toHaveValue('0');
    fireEvent.change(scale, { target: { value: '5' } });
    expect(scale).toHaveValue('0');
    view.unmount();

    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    const gate = deferred<User>();
    vi.spyOn(authApi, 'uploadAvatar').mockReturnValue(gate.promise);
    renderProfile();
    chooseFile(jpegFile('save.jpg'));
    node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    fireEvent.click(within(node).getByRole('button', { name: 'Сохранить фото' }));
    const saving = sliderOf(node);
    await waitFor(() => expect(saving).toBeDisabled());
    expect(saving.className).toContain('appearance-none');
    expect(saving.className).toContain('disabled:opacity-40');
    expect(saving.className).toContain('bg-white');
    const held = spin(previewOf(node), { deltaY: -1, deltaMode: 1 });
    expect(held.event.defaultPrevented).toBe(true);
    expect(saving).toHaveValue('0');
    fireEvent.change(saving, { target: { value: '10' } });
    expect(saving).toHaveValue('0');
  });

  it('ТК-6 accumulates a short pixel scroll and ignores a horizontal wheel', async () => {
    vi.stubGlobal('createImageBitmap', async () => bitmap(2000, 3000));
    renderProfile();
    chooseFile(jpegFile());
    const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    const preview = previewOf(node);
    spin(preview, { deltaY: -10, deltaMode: 0 });
    expect(sliderOf(node)).toHaveValue('19');
    spin(preview, { deltaY: -10, deltaMode: 0 });
    expect(sliderOf(node)).toHaveValue('38');
    const flat = spin(preview, { deltaY: 0, deltaX: 40, deltaMode: 0 });
    expect(flat.event.defaultPrevented).toBe(true);
    expect(flat.heard).not.toHaveBeenCalled();
    expect(sliderOf(node)).toHaveValue('38');
  });

  it('ТК-7 opens the saved avatar for every role without a file dialog or the new-file checks', async () => {
    const roles = ['client', 'manager', 'admin'] as const;
    for (const role of roles) {
      let release!: (response: Response) => void;
      const pending = new Promise<Response>((resolve) => {
        release = resolve;
      });
      const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        expect(String(input)).toBe(MEDIA);
        expect(init).toEqual({ credentials: 'same-origin' });
        return pending;
      });
      vi.stubGlobal('fetch', fetchMock);
      vi.stubGlobal('createImageBitmap', async () => bitmap(256, 256));
      useAuthStore.setState({
        user: { ...user, role, avatar: MEDIA },
        isAuthenticated: true,
        isLoading: false,
      });
      const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
      const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
      const getMe = vi.spyOn(authApi, 'getMe');
      const view = renderProfile();
      const input = fileInput();
      const click = vi.spyOn(input, 'click');
      const replace = screen.getByRole('button', { name: 'Заменить' });
      const move = screen.getByRole('button', { name: 'Изменить положение фото' });
      const remove = screen.getByRole('button', { name: 'Удалить фото' });
      expect(replace.compareDocumentPosition(move) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
      expect(move.compareDocumentPosition(remove) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
      expect(replace.parentElement).toBe(move.parentElement);
      expect(move.parentElement?.className).toContain('flex-wrap');
      expect(move.parentElement?.className).toContain('gap-2');
      expect(replace.className).toContain('border-primary');
      expect(move.className).toContain('border-primary');
      expect(move.className).toContain('text-sm');
      expect(remove.className).not.toContain('border-primary');
      expect(move.querySelector('svg')).toBeNull();

      fireEvent.click(move);
      await waitFor(() => expect(screen.getByRole('button', { name: 'Заменить' })).toBeDisabled());
      expect(screen.getByRole('button', { name: 'Изменить положение фото' })).toBeDisabled();
      expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeDisabled();
      fireEvent.click(screen.getByRole('button', { name: 'Заменить' }));
      fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
      fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
      expect(click).not.toHaveBeenCalled();
      expect(uploadAvatar).not.toHaveBeenCalled();
      expect(deleteAvatar).not.toHaveBeenCalled();
      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();

      const host = HTMLElement.prototype;
      const width = Object.getOwnPropertyDescriptor(host, 'clientWidth');
      Object.defineProperty(host, 'clientWidth', {
        configurable: true,
        get() {
          return 320;
        },
      });
      try {
        release(new Response(Uint8Array.from([1, 2, 3, 4]), { status: 200 }));
        const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
        await waitFor(() => {
          expect(node.querySelector('canvas')).toHaveStyle({
            left: '16px',
            top: '16px',
            width: '288px',
            height: '288px',
          });
        });
        expect(screen.getByTestId('location').textContent).toBe('/dashboard/profile');
        expect(sliderOf(node)).toHaveValue('0');
        expect(sliderOf(node)).toHaveAttribute('max', '192');
        expect(screen.getByRole('button', { name: 'Заменить' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Изменить положение фото' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Удалить фото' })).toBeDisabled();
        expect(getMe).not.toHaveBeenCalled();
      } finally {
        if (width) Object.defineProperty(host, 'clientWidth', width);
        else Reflect.deleteProperty(host, 'clientWidth');
      }
      view.unmount();
    }
  });

  it('ТК-8 hides the action when there is no photo and after a delete', async () => {
    const view = renderProfile();
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeEnabled();
    view.unmount();

    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    vi.spyOn(authApi, 'deleteAvatar').mockResolvedValue({ ...user, avatar: undefined });
    renderProfile();
    expect(screen.getByRole('button', { name: 'Изменить положение фото' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Загрузить фото' })).toBeEnabled());
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.getByText('АС')).toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
  });

  it('ТК-9 saves the new square from the saved photo and reads that photo next time', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    vi.stubGlobal('createImageBitmap', async () => bitmap(256, 256));
    const urls: string[] = [];
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        urls.push(String(input));
        expect(init).toEqual({ credentials: 'same-origin' });
        return new Response(Uint8Array.from([9, 9, 9, 9]), { status: 200 });
      }),
    );
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
          data: profileDto(NEXT),
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
      fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
      const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
      fireEvent.change(sliderOf(node), { target: { value: '128' } });
      clearAvatarDraws();
      fireEvent.click(within(node).getByRole('button', { name: 'Сохранить фото' }));
      await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument());
      expect(encodeDraws().at(-1)?.args.slice(0, 4)).toEqual([64, 64, 128, 128]);
      expect(deleteAvatar).not.toHaveBeenCalled();
      expect(updateProfile).not.toHaveBeenCalled();
      expect(getMe).not.toHaveBeenCalled();
      expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
      expect(useAuthStore.getState().user?.avatar).toBe(NEXT);
      for (const image of screen.getAllByRole('img', { name: 'Анна Смирнова' })) {
        expect(image).toHaveAttribute('src', NEXT);
      }

      fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
      const again = await screen.findByRole('dialog', { name: 'Обрезка фото' });
      expect(urls).toEqual([MEDIA, NEXT]);
      expect(sliderOf(again)).toHaveValue('0');
      expect(sliderOf(again)).toHaveAttribute('max', '192');
      expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    } finally {
      apiClient.defaults.adapter = original;
    }
  });

  it('ТК-10 closes the reframed photo without writing it', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const fetchMock = vi.fn(async () => new Response(Uint8Array.from([1, 2, 3, 4]), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('createImageBitmap', async () => bitmap(256, 256));
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar');
    const updateProfile = vi.spyOn(authApi, 'updateProfile');
    renderProfile();
    fireEvent.change(screen.getByLabelText('Имя'), { target: { value: 'Мария' } });

    const closers = [
      () => fireEvent.click(screen.getByRole('button', { name: 'Отмена' })),
      () => fireEvent.click(screen.getByRole('button', { name: 'Закрыть' })),
      () => {
        const node = dialog();
        const backdrop = node.parentElement?.querySelector('.backdrop-blur-sm');
        if (!(backdrop instanceof HTMLElement)) throw new Error('backdrop missing');
        fireEvent.click(backdrop);
      },
      () => fireEvent.keyDown(document, { key: 'Escape' }),
    ];

    for (const close of closers) {
      fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
      const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
      fireEvent.change(sliderOf(node), { target: { value: '64' } });
      close();
      await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument());
      expect(uploadAvatar).not.toHaveBeenCalled();
      expect(deleteAvatar).not.toHaveBeenCalled();
      expect(updateProfile).not.toHaveBeenCalled();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(screen.getByLabelText('Имя')).toHaveValue('Мария');
    }
    expect(fetchMock).toHaveBeenCalledTimes(closers.length);
  });

  it('ТК-11 shows the open failure and clears it on the next success', async () => {
    expect(AVATAR_OPEN_ERROR).toBe(OPEN_ERROR);
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    let mode: 'status' | 'empty' | 'throw' | 'ok' = 'status';
    vi.stubGlobal('fetch', async () => {
      if (mode === 'status') return new Response('Not Found', { status: 404, statusText: 'Not Found' });
      if (mode === 'empty') return new Response(new Uint8Array(0), { status: 200 });
      if (mode === 'throw') return new Response(Uint8Array.from([9]), { status: 200 });
      return new Response(Uint8Array.from([1, 2, 3, 4]), { status: 200 });
    });
    vi.stubGlobal('createImageBitmap', async (blob: Blob) => {
      if (blob.size === 1) throw new Error('decode');
      return bitmap(256, 256);
    });
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    renderProfile();

    async function fail() {
      fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
      await waitFor(() => expect(screen.getByRole('button', { name: 'Изменить положение фото' })).toBeEnabled());
      expect(screen.getByRole('alert')).toHaveTextContent(OPEN_ERROR);
      expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
      expect(screen.queryByText('Not Found')).not.toBeInTheDocument();
      expect(screen.queryByText('404')).not.toBeInTheDocument();
      expect(screen.queryByText('decode')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Анна Смирнова' })).toHaveAttribute('src', MEDIA);
      expect(uploadAvatar).not.toHaveBeenCalled();
    }

    await fail();
    mode = 'empty';
    await fail();
    mode = 'throw';
    await fail();
    mode = 'ok';
    fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
    expect(await screen.findByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();
    expect(screen.queryByText(OPEN_ERROR)).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    mode = 'status';
    fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Изменить положение фото' })).toBeEnabled());
    expect(screen.getByRole('alert')).toHaveTextContent(OPEN_ERROR);
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    mode = 'ok';
    chooseFile(jpegFile());
    expect(await screen.findByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();
    expect(screen.queryByText(OPEN_ERROR)).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();
  });

  it('ТК-12 still replaces through the file dialog and deletes without opening the crop', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    vi.stubGlobal('createImageBitmap', async () => bitmap(64, 64));
    const uploadAvatar = vi.spyOn(authApi, 'uploadAvatar');
    const deleteAvatar = vi.spyOn(authApi, 'deleteAvatar').mockResolvedValue({ ...user, avatar: undefined });
    renderProfile();
    const input = fileInput();
    const click = vi.spyOn(input, 'click');
    fireEvent.click(screen.getByRole('button', { name: 'Заменить' }));
    expect(click).toHaveBeenCalled();

    chooseFile(fileOf('note.txt', 'text/plain', [1, 2, 3]));
    expect(await screen.findByRole('alert')).toHaveTextContent(TYPE_ERROR);
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.queryByText(OPEN_ERROR)).not.toBeInTheDocument();

    chooseFile(jpegFile());
    expect(await screen.findByRole('dialog', { name: 'Обрезка фото' })).toBeInTheDocument();
    expect(screen.queryByText(TYPE_ERROR)).not.toBeInTheDocument();
    expect(uploadAvatar).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Удалить фото' }));
    await waitFor(() => expect(deleteAvatar).toHaveBeenCalledTimes(1));
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    expect(screen.getByText('АС')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
  });

  it('ТК-13 keeps the action off settings and off someone else cards', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    const view = renderProfile('/dashboard/profile?tab=settings');
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    view.unmount();

    const manager = renderPage(
      <ManagerShortCard
        userId="other-manager"
        onClose={() => {}}
        loadCard={async () => ({ id: 'other-manager', displayName: 'Иван Петров' })}
      />,
    );
    expect(await screen.findByText('Иван Петров')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    manager.unmount();

    const client = renderPage(
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
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
    client.unmount();

    vi.mocked(getClientCard).mockResolvedValue({
      id: 'anna-id',
      displayName: 'Анна Соколова',
      email: 'anna@example.com',
      avatarUrl: MEDIA,
      phone: '+79991234567',
      bookings: [],
      notes: [],
    });
    render(
      <MemoryRouter initialEntries={['/manager/clients/anna-id']}>
        <Routes>
          <Route path="/manager/clients/:id" element={<ManagerClientCardPage />} />
        </Routes>
      </MemoryRouter>,
    );
    expect(await screen.findByRole('heading', { name: 'Анна Соколова' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Изменить положение фото' })).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog', { name: 'Обрезка фото' })).not.toBeInTheDocument();
  });

  it('ТК-14 drops a late saved-photo bitmap when a file was chosen after it', async () => {
    useAuthStore.setState({
      user: { ...user, avatar: MEDIA },
      isAuthenticated: true,
      isLoading: false,
    });
    let release!: (response: Response) => void;
    vi.stubGlobal(
      'fetch',
      vi.fn(
        () =>
          new Promise<Response>((resolve) => {
            release = resolve;
          }),
      ),
    );
    const lateClose = vi.fn();
    vi.stubGlobal('createImageBitmap', async (blob: Blob) => {
      if (blob.size === 32) return bitmap(256, 256, lateClose);
      return bitmap(150, 100);
    });
    renderProfile();
    fireEvent.click(screen.getByRole('button', { name: 'Изменить положение фото' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Заменить' })).toBeDisabled());
    chooseFile(jpegFile());
    release(new Response(new Uint8Array(32), { status: 200 }));
    const node = await screen.findByRole('dialog', { name: 'Обрезка фото' });
    await waitFor(() => expect(lateClose).toHaveBeenCalled());
    expect(screen.getAllByRole('dialog', { name: 'Обрезка фото' })).toHaveLength(1);
    expect(sliderOf(node)).toHaveValue('0');
    expect(sliderOf(node)).toHaveAttribute('max', '36');
  });
});
