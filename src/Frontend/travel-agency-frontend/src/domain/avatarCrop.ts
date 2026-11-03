import {
  avatarFileError,
  AVATAR_RATIO_ERROR,
  AVATAR_TYPE_ERROR,
} from '@/domain/profileForm';

export const AVATAR_MIN_DIAMETER = 64;
export const JPEG_QUALITY = 0.92;
export const CROP_PREVIEW_PAD = 16;

const JPEG = [0xff, 0xd8, 0xff];
const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const RIFF = [0x52, 0x49, 0x46, 0x46];
const WEBP = [0x57, 0x45, 0x42, 0x50];

export type Circle = {
  diameter: number;
  cx: number;
  cy: number;
};

export type Square = {
  x: number;
  y: number;
  side: number;
};

export type AvatarBitmap = {
  width: number;
  height: number;
  source: CanvasImageSource;
  close?: () => void;
};

export type BitmapLoader = (file: Blob) => Promise<AvatarBitmap>;

export type InspectedAvatar =
  | { ok: false; message: string }
  | { ok: true; image: AvatarBitmap };

function sameBytes(bytes: Uint8Array, signature: number[], offset = 0): boolean {
  if (bytes.length < offset + signature.length) return false;
  for (let i = 0; i < signature.length; i += 1) {
    if (bytes[offset + i] !== signature[i]) return false;
  }
  return true;
}

export function signatureMatches(type: string, bytes: Uint8Array): boolean {
  if (type === 'image/jpeg') return sameBytes(bytes, JPEG);
  if (type === 'image/png') return sameBytes(bytes, PNG);
  if (type === 'image/webp') return sameBytes(bytes, RIFF) && sameBytes(bytes, WEBP, 8);
  return false;
}

export function longestSideExceeds(width: number, height: number): boolean {
  const longSide = Math.max(width, height);
  const shortSide = Math.min(width, height);
  return longSide > shortSide * 3;
}

export function initialCircle(width: number, height: number): Circle {
  return { diameter: Math.min(width, height), cx: width / 2, cy: height / 2 };
}

export function lowerDiameter(width: number, height: number): number {
  return Math.min(Math.min(width, height), AVATAR_MIN_DIAMETER);
}

export function clampCircle(circle: Circle, width: number, height: number): Circle {
  const minCx = circle.diameter / 2;
  const maxCx = width - circle.diameter / 2;
  const minCy = circle.diameter / 2;
  const maxCy = height - circle.diameter / 2;
  return {
    diameter: circle.diameter,
    cx: Math.min(Math.max(circle.cx, minCx), maxCx),
    cy: Math.min(Math.max(circle.cy, minCy), maxCy),
  };
}

export function withDiameter(circle: Circle, diameter: number, width: number, height: number): Circle {
  const minD = lowerDiameter(width, height);
  const maxD = Math.min(width, height);
  const next = Math.min(Math.max(Math.round(diameter), minD), maxD);
  return clampCircle({ diameter: next, cx: circle.cx, cy: circle.cy }, width, height);
}

export function squareOf(circle: Circle, width: number, height: number): Square {
  const clamped = clampCircle(circle, width, height);
  const side = clamped.diameter;
  return {
    side,
    x: Math.min(Math.max(Math.floor(clamped.cx - side / 2), 0), width - side),
    y: Math.min(Math.max(Math.floor(clamped.cy - side / 2), 0), height - side),
  };
}

export function circleScreenPx(previewPx: number): number {
  return Math.max(previewPx - CROP_PREVIEW_PAD * 2, 0);
}

export function panCircle(
  circle: Circle,
  dx: number,
  dy: number,
  circleScreen: number,
  width: number,
  height: number,
): Circle {
  if (circleScreen <= 0 || circle.diameter <= 0) return clampCircle(circle, width, height);
  const scale = circleScreen / circle.diameter;
  return clampCircle(
    {
      diameter: circle.diameter,
      cx: circle.cx - dx / scale,
      cy: circle.cy - dy / scale,
    },
    width,
    height,
  );
}

/** Стороны bitmap уже прямые: `createImageBitmap` без аргумента берёт ориентацию `from-image`. */
export async function loadStraight(file: Blob): Promise<AvatarBitmap> {
  const bitmap = await createImageBitmap(file);
  return {
    width: bitmap.width,
    height: bitmap.height,
    source: bitmap,
    close: () => bitmap.close(),
  };
}

async function readHeader(file: Blob): Promise<Uint8Array> {
  return new Uint8Array(await file.slice(0, 16).arrayBuffer());
}

export async function inspectAvatarFile(
  file: File,
  load: BitmapLoader = loadStraight,
): Promise<InspectedAvatar> {
  const early = avatarFileError(file);
  if (early) return { ok: false, message: early };

  const header = await readHeader(file);
  if (!signatureMatches(file.type, header)) return { ok: false, message: AVATAR_TYPE_ERROR };

  let image: AvatarBitmap;
  try {
    image = await load(file);
  } catch {
    return { ok: false, message: AVATAR_TYPE_ERROR };
  }

  if (image.width <= 0 || image.height <= 0) {
    image.close?.();
    return { ok: false, message: AVATAR_TYPE_ERROR };
  }

  if (longestSideExceeds(image.width, image.height)) {
    image.close?.();
    return { ok: false, message: AVATAR_RATIO_ERROR };
  }

  return { ok: true, image };
}

export function encodeAvatarJpeg(source: CanvasImageSource, square: Square): Promise<File | null> {
  const canvas = document.createElement('canvas');
  canvas.width = square.side;
  canvas.height = square.side;
  const ctx = canvas.getContext('2d');
  if (!ctx || typeof canvas.toBlob !== 'function') return Promise.resolve(null);

  ctx.drawImage(
    source,
    square.x,
    square.y,
    square.side,
    square.side,
    0,
    0,
    square.side,
    square.side,
  );

  return new Promise((resolve) => {
    canvas.toBlob((blob) => {
      if (!blob) {
        resolve(null);
        return;
      }
      resolve(new File([blob], 'avatar.jpg', { type: 'image/jpeg' }));
    }, 'image/jpeg', JPEG_QUALITY);
  });
}
