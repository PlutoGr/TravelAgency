import { afterAll, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  circleScreenPx,
  encodeAvatarJpeg,
  initialCircle,
  inspectAvatarFile,
  loadStraight,
  lowerDiameter,
  panCircle,
  squareOf,
  withDiameter,
  type AvatarBitmap,
} from './avatarCrop';
import {
  AVATAR_MAX_BYTES,
  AVATAR_RATIO_ERROR,
  AVATAR_SIZE_ERROR,
  AVATAR_TYPE_ERROR,
  avatarUploadMessage,
} from './profileForm';
import {
  clearAvatarDraws,
  installAvatarCanvas,
  lastJpegQuality,
  setAvatarToBlob,
} from '@/test/avatarCanvas';

const JPEG = [0xff, 0xd8, 0xff, 0x00];
const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const WEBP = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];

const restoreCanvas = installAvatarCanvas();

afterAll(() => {
  restoreCanvas();
});

beforeEach(() => {
  clearAvatarDraws();
  setAvatarToBlob('jpeg');
  vi.unstubAllGlobals();
});

function fileOf(type: string, bytes: number[], size = bytes.length, name = 'photo.bin'): File {
  const file = new File([Uint8Array.from(bytes)], name, { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

function bitmap(width: number, height: number, rgba?: Uint8ClampedArray): AvatarBitmap {
  const source = { width, height, rgba } as CanvasImageSource & {
    width: number;
    height: number;
    rgba?: Uint8ClampedArray;
  };
  return { width, height, source, close: vi.fn() };
}

function loader(image: AvatarBitmap) {
  return vi.fn(async () => image);
}

describe('inspectAvatarFile', () => {
  it('ТК-6 and ТК-7 reject type before size, signature and a bitmap that does not open', async () => {
    const opened = loader(bitmap(32, 32));
    const cases: Array<{ file: File; message: string }> = [
      { file: fileOf('image/jpeg', JPEG, 0), message: AVATAR_TYPE_ERROR },
      { file: fileOf('application/pdf', [0x25, 0x50, 0x44, 0x46], 20), message: AVATAR_TYPE_ERROR },
      { file: fileOf('video/mp4', [0, 0, 0, 0], 20), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/gif', [0x47, 0x49, 0x46, 0x38, 0x39, 0x61], 20), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/heic', [0, 0, 0, 0], 20), message: AVATAR_TYPE_ERROR },
      { file: fileOf('', JPEG, 20), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/jpeg', JPEG, AVATAR_MAX_BYTES + 1), message: AVATAR_SIZE_ERROR },
      { file: fileOf('application/pdf', [0x25, 0x50, 0x44, 0x46], AVATAR_MAX_BYTES + 1), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/jpeg', PNG, 32), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/png', JPEG, 32), message: AVATAR_TYPE_ERROR },
      { file: fileOf('image/webp', JPEG, 32), message: AVATAR_TYPE_ERROR },
    ];

    for (const item of cases) {
      const result = await inspectAvatarFile(item.file, opened);
      expect(result).toEqual({ ok: false, message: item.message });
    }
    expect(opened).not.toHaveBeenCalled();

    const broken = vi.fn(async () => {
      throw new Error('decode');
    });
    const undecoded = await inspectAvatarFile(fileOf('image/jpeg', JPEG, 32), broken);
    expect(undecoded).toEqual({ ok: false, message: AVATAR_TYPE_ERROR });

    const empty = bitmap(0, 10);
    const zero = await inspectAvatarFile(fileOf('image/png', PNG, 32), loader(empty));
    expect(zero).toEqual({ ok: false, message: AVATAR_TYPE_ERROR });
    expect(empty.close).toHaveBeenCalled();
  });

  it('ТК-8 accepts 3000×1000 and 5 MiB and rejects 3001×1000 as integers', async () => {
    const wide = bitmap(3001, 1000);
    const rejected = await inspectAvatarFile(
      fileOf('image/jpeg', JPEG, AVATAR_MAX_BYTES),
      loader(wide),
    );
    expect(rejected).toEqual({ ok: false, message: AVATAR_RATIO_ERROR });
    expect(wide.close).toHaveBeenCalled();

    const fit = bitmap(3000, 1000);
    const accepted = await inspectAvatarFile(fileOf('image/webp', WEBP, AVATAR_MAX_BYTES), loader(fit));
    expect(accepted.ok).toBe(true);
    if (accepted.ok) expect(accepted.image).toBe(fit);
    expect(fit.close).not.toHaveBeenCalled();
  });

  it('ТК-9 uses upright bitmap sides, not the raw header', async () => {
    const sideways = bitmap(1000, 4000);
    const tooLong = await inspectAvatarFile(fileOf('image/jpeg', JPEG, 32), loader(sideways));
    expect(tooLong).toEqual({ ok: false, message: AVATAR_RATIO_ERROR });

    const upright = bitmap(1500, 4000);
    const opens = await inspectAvatarFile(fileOf('image/jpeg', JPEG, 32), loader(upright));
    expect(opens.ok).toBe(true);

    const rotated = { width: 1500, height: 4000, close: vi.fn() };
    vi.stubGlobal('createImageBitmap', vi.fn(async () => rotated));
    const loaded = await loadStraight(fileOf('image/jpeg', JPEG, 32));
    expect(createImageBitmap).toHaveBeenCalledTimes(1);
    expect(vi.mocked(createImageBitmap).mock.calls[0]).toHaveLength(1);
    expect(loaded.width).toBe(1500);
    expect(loaded.height).toBe(4000);
    const inspected = await inspectAvatarFile(fileOf('image/png', PNG, 32));
    expect(inspected.ok).toBe(true);
    if (inspected.ok) {
      expect(inspected.image.width).toBe(1500);
      expect(squareOf(initialCircle(inspected.image.width, inspected.image.height), 1500, 4000).side).toBe(1500);
    }
  });
});

describe('circle geometry', () => {
  it('ТК-10 starts at the short side and keeps the square on the photo', () => {
    const start = initialCircle(2000, 3000);
    expect(start).toEqual({ diameter: 2000, cx: 1000, cy: 1500 });
    expect(squareOf(start, 2000, 3000)).toEqual({ x: 0, y: 500, side: 2000 });
    expect(lowerDiameter(2000, 3000)).toBe(64);

    const across = panCircle(start, 40, 0, circleScreenPx(320), 2000, 3000);
    expect(squareOf(across, 2000, 3000)).toEqual({ x: 0, y: 500, side: 2000 });
    const toEdge = panCircle(start, 0, -500, circleScreenPx(320), 2000, 3000);
    expect(toEdge.cy).toBe(2000);
    expect(squareOf(toEdge, 2000, 3000)).toEqual({ x: 0, y: 1000, side: 2000 });

    const shrunk = withDiameter(start, 1000, 2000, 3000);
    expect(shrunk).toEqual({ diameter: 1000, cx: 1000, cy: 1500 });
    const bottom = panCircle(shrunk, 0, -288, 288, 2000, 3000);
    expect(bottom.cy).toBe(2500);
    expect(squareOf(bottom, 2000, 3000)).toEqual({ x: 500, y: 2000, side: 1000 });

    expect(withDiameter(start, 2001, 2000, 3000).diameter).toBe(2000);
    expect(withDiameter(shrunk, 63, 2000, 3000).diameter).toBe(64);

    const blocked = withDiameter({ diameter: 1000, cx: 1000, cy: 2500 }, 2000, 2000, 3000);
    expect(blocked.cy).toBe(2000);
    expect(blocked.diameter).toBe(2000);

    const square = initialCircle(1000, 1000);
    expect(squareOf(panCircle(square, 30, -20, 200, 1000, 1000), 1000, 1000)).toEqual({
      x: 0,
      y: 0,
      side: 1000,
    });

    const almost = initialCircle(65, 65);
    expect(withDiameter(almost, 64, 65, 65).diameter).toBe(64);
    expect(lowerDiameter(65, 65)).toBe(64);
  });

  it('ТК-11 keeps a frame shorter than 64 px and does not slide off the short side', () => {
    const tiny = initialCircle(40, 80);
    expect(tiny.diameter).toBe(40);
    expect(lowerDiameter(40, 80)).toBe(40);
    expect(withDiameter(tiny, 10, 40, 80).diameter).toBe(40);
    expect(withDiameter(tiny, 80, 40, 80).diameter).toBe(40);

    const slid = panCircle(tiny, 25, -50, 100, 40, 80);
    expect(slid.cx).toBe(20);
    expect(slid.cy).toBe(60);
    expect(squareOf(slid, 40, 80)).toEqual({ x: 0, y: 40, side: 40 });

    expect(initialCircle(64, 64).diameter).toBe(64);
    expect(withDiameter(initialCircle(64, 64), 32, 64, 64).diameter).toBe(64);
  });
});

describe('encodeAvatarJpeg', () => {
  it('ТК-2 and ТК-3 write the square corner into a jpeg and leave the circle mask out', async () => {
    const width = 100;
    const height = 160;
    const rgba = new Uint8ClampedArray(width * height * 4);
    for (let i = 0; i < rgba.length; i += 4) {
      rgba[i] = 10;
      rgba[i + 1] = 20;
      rgba[i + 2] = 220;
      rgba[i + 3] = 255;
    }
    const shrunk = withDiameter(initialCircle(width, height), 64, width, height);
    const moved = panCircle(shrunk, 0, -48, 64, width, height);
    const square = squareOf(moved, width, height);
    expect(square).toEqual({ x: 18, y: 96, side: 64 });
    const corner = (square.y * width + square.x) * 4;
    rgba[corner] = 240;
    rgba[corner + 1] = 10;
    rgba[corner + 2] = 10;
    rgba[corner + 3] = 255;

    const source = { width, height, rgba } as CanvasImageSource & {
      width: number;
      height: number;
      rgba: Uint8ClampedArray;
    };
    const file = await encodeAvatarJpeg(source, square);
    expect(file).not.toBeNull();
    expect(file?.name).toBe('avatar.jpg');
    expect(file?.type).toBe('image/jpeg');
    expect(lastJpegQuality()).toBe(0.92);

    const bytes = new Uint8Array(await file!.arrayBuffer());
    expect(Array.from(bytes.slice(0, 3))).toEqual([0xff, 0xd8, 0xff]);
    const pixels = bytes.slice(3);
    expect(pixels[0]).toBe(240);
    expect(pixels[1]).toBe(10);
    expect(pixels[2]).toBe(10);
    const inside = (32 * square.side + 32) * 4;
    expect(pixels[inside + 2]).toBe(220);

    const whole = squareOf(initialCircle(64, 64), 64, 64);
    expect(whole).toEqual({ x: 0, y: 0, side: 64 });

    setAvatarToBlob('empty');
    expect(await encodeAvatarJpeg(source, square)).toBeNull();
  });
});

describe('avatarUploadMessage', () => {
  it('ТК-13 takes the first cyrillic string from errors, then detail', () => {
    expect(
      avatarUploadMessage({
        response: {
          data: {
            detail: 'File content is not an image.',
            errors: { file: ['Подходят JPG, PNG или WebP.'] },
          },
        },
      }),
    ).toBe('Подходят JPG, PNG или WebP.');

    expect(
      avatarUploadMessage({
        response: {
          data: {
            errors: {
              note: ['Not an image.'],
              SizeBytes: ['', 'Файл больше 5 МБ.'],
            },
          },
        },
      }),
    ).toBe('Файл больше 5 МБ.');

    expect(
      avatarUploadMessage({
        response: { data: { detail: 'Upload failed upstream.' } },
      }),
    ).toBe('Не удалось загрузить аватар. Попробуйте ещё раз.');

    expect(avatarUploadMessage(new Error('network'))).toBe(
      'Не удалось загрузить аватар. Попробуйте ещё раз.',
    );

    expect(
      avatarUploadMessage({
        response: {
          status: 403,
          data: { detail: 'Forbidden.', errors: { file: ['Forbidden.'] } },
        },
      }),
    ).toBe('Не удалось загрузить аватар. Попробуйте ещё раз.');
  });
});
