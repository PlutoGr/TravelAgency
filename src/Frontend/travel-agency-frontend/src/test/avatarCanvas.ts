type CanvasWithBuffer = HTMLCanvasElement & { __rgba?: Uint8ClampedArray };

export type DrawCall = { args: number[] };

let draws: DrawCall[] = [];
let quality: number | undefined;
let blobMode: 'jpeg' | 'empty' = 'jpeg';

export function avatarDraws(): DrawCall[] {
  return draws;
}

export function clearAvatarDraws(): void {
  draws = [];
  quality = undefined;
}

export function lastJpegQuality(): number | undefined {
  return quality;
}

export function setAvatarToBlob(mode: 'jpeg' | 'empty'): void {
  blobMode = mode;
}

export function installAvatarCanvas(): () => void {
  const proto = HTMLCanvasElement.prototype;
  const originalGetContext = proto.getContext;
  const originalToBlob = proto.toBlob;

  proto.getContext = function (this: HTMLCanvasElement, type: string) {
    if (type !== '2d') return originalGetContext.call(this, type as '2d');
    const canvas = this as CanvasWithBuffer;
    const target = {
      drawImage(source: CanvasImageSource & { rgba?: Uint8ClampedArray; width?: number; height?: number }, ...args: number[]) {
        draws.push({ args });
        const buffer = new Uint8ClampedArray(canvas.width * canvas.height * 4);
        const rgba = source.rgba;
        const sourceWidth = source.width ?? 0;
        if (rgba && args.length >= 8 && sourceWidth > 0) {
          const [sx, sy, sw, sh] = args;
          for (let y = 0; y < canvas.height; y += 1) {
            for (let x = 0; x < canvas.width; x += 1) {
              const srcX = sx + Math.min(sw - 1, Math.floor((x * sw) / canvas.width));
              const srcY = sy + Math.min(sh - 1, Math.floor((y * sh) / canvas.height));
              const si = (srcY * sourceWidth + srcX) * 4;
              const di = (y * canvas.width + x) * 4;
              buffer[di] = rgba[si] ?? 0;
              buffer[di + 1] = rgba[si + 1] ?? 0;
              buffer[di + 2] = rgba[si + 2] ?? 0;
              buffer[di + 3] = rgba[si + 3] ?? 0;
            }
          }
        }
        canvas.__rgba = buffer;
      },
    };
    return new Proxy(target, {
      get(current, prop, receiver) {
        if (prop in current || typeof prop === 'symbol') return Reflect.get(current, prop, receiver);
        throw new Error(`canvas 2d ${String(prop)} не используется для квадрата аватара`);
      },
      set(_current, prop) {
        throw new Error(`canvas 2d ${String(prop)} не используется для квадрата аватара`);
      },
    }) as unknown as CanvasRenderingContext2D;
  } as typeof proto.getContext;

  proto.toBlob = function (this: HTMLCanvasElement, callback, type, imageQuality) {
    quality = typeof imageQuality === 'number' ? imageQuality : undefined;
    if (blobMode === 'empty') {
      callback(null);
      return;
    }
    const rgba = (this as CanvasWithBuffer).__rgba ?? new Uint8ClampedArray();
    const payload = new Uint8Array(3 + rgba.length);
    payload[0] = 0xff;
    payload[1] = 0xd8;
    payload[2] = 0xff;
    payload.set(rgba, 3);
    callback(new Blob([payload], { type: type ?? 'image/jpeg' }));
  };

  return () => {
    proto.getContext = originalGetContext;
    proto.toBlob = originalToBlob;
    blobMode = 'jpeg';
    draws = [];
    quality = undefined;
  };
}
