import { useEffect, useLayoutEffect, useRef, useState, type PointerEvent } from 'react';
import { Button, Modal } from '@/components/ui';
import {
  circleScreenPx,
  CROP_PREVIEW_PAD,
  encodeAvatarJpeg,
  initialCircle,
  lowerDiameter,
  panCircle,
  pointerOverCropCircle,
  squareOf,
  stepWheelDiameter,
  withDiameter,
  type AvatarBitmap,
  type Circle,
} from '@/domain/avatarCrop';
import { AVATAR_UPLOAD_ERROR } from '@/domain/profileForm';

type AvatarCropDialogProps = {
  image: AvatarBitmap;
  saving: boolean;
  error?: string;
  onCancel: () => void;
  onSave: (file: File) => void | Promise<void>;
};

const DIALOG_NAME = 'Обрезка фото';

const SCALE_SLIDER = [
  'mt-2 h-2 w-full cursor-pointer appearance-none rounded-full border-0 p-0 disabled:opacity-40',
  '[&::-webkit-slider-runnable-track]:h-2 [&::-webkit-slider-runnable-track]:bg-transparent',
  '[&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:appearance-none',
  '[&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-primary',
  '[&::-webkit-slider-thumb]:bg-white [&::-webkit-slider-thumb]:shadow-md [&::-webkit-slider-thumb]:cursor-pointer',
  'hover:[&::-webkit-slider-thumb]:shadow-lg',
  'focus:[&::-webkit-slider-thumb]:ring-2 focus:[&::-webkit-slider-thumb]:ring-primary/30',
  '[&::-moz-range-track]:h-2 [&::-moz-range-track]:rounded-full [&::-moz-range-track]:border-0 [&::-moz-range-track]:bg-transparent',
  '[&::-moz-range-thumb]:h-5 [&::-moz-range-thumb]:w-5 [&::-moz-range-thumb]:appearance-none',
  '[&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-primary',
  '[&::-moz-range-thumb]:bg-white [&::-moz-range-thumb]:shadow-md [&::-moz-range-thumb]:cursor-pointer',
  'hover:[&::-moz-range-thumb]:shadow-lg',
  'focus:[&::-moz-range-thumb]:ring-2 focus:[&::-moz-range-thumb]:ring-primary/30',
].join(' ');

function cropDialog(): HTMLElement | null {
  const node = document.querySelector(`[role="dialog"][aria-label="${DIALOG_NAME}"]`);
  return node instanceof HTMLElement ? node : null;
}

function tabbables(dialog: HTMLElement): HTMLElement[] {
  return [...dialog.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href]')].filter(
    (el) => !el.hasAttribute('disabled'),
  );
}

export default function AvatarCropDialog({
  image,
  saving,
  error,
  onCancel,
  onSave,
}: AvatarCropDialogProps) {
  const previewRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drag = useRef<{ x: number; y: number; circle: Circle } | null>(null);
  const lock = useRef(false);
  const alive = useRef(true);
  const wheelAcc = useRef(0);
  const [circle, setCircle] = useState(() => initialCircle(image.width, image.height));
  const circleRef = useRef(circle);
  const [previewPx, setPreviewPx] = useState(0);
  const [encoding, setEncoding] = useState(false);
  const [grabbing, setGrabbing] = useState(false);
  const [localError, setLocalError] = useState<string | undefined>();
  const locked = saving || encoding;
  const lockedRef = useRef(locked);

  useLayoutEffect(() => {
    circleRef.current = circle;
    lockedRef.current = locked;
  }, [circle, locked]);
  const maxD = Math.min(image.width, image.height);
  const minD = lowerDiameter(image.width, image.height);
  const span = maxD - minD;
  const sliderPos = span === 0 ? 0 : maxD - circle.diameter;
  const screen = circleScreenPx(previewPx);
  const scale = screen > 0 && circle.diameter > 0 ? screen / circle.diameter : 0;
  const message = localError ?? error;

  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
    };
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.width = image.width;
    canvas.height = image.height;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.drawImage(image.source, 0, 0);
  }, [image]);

  useEffect(() => {
    const node = previewRef.current;
    if (!node) return;
    const read = () => setPreviewPx(node.clientWidth);
    read();
    if (typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(read);
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const dialog = cropDialog();
    const close = dialog?.querySelector('button[aria-label="Закрыть"]');
    if (close instanceof HTMLButtonElement) close.disabled = locked;
  }, [locked]);

  useEffect(() => {
    const node = previewRef.current;
    if (!node) return;
    const onWheel = (event: WheelEvent) => {
      const widthPx = node.clientWidth;
      const rect = node.getBoundingClientRect();
      if (!pointerOverCropCircle(event.clientX - rect.left, event.clientY - rect.top, widthPx)) return;
      event.preventDefault();
      event.stopPropagation();
      if (lockedRef.current) return;
      const current = circleRef.current;
      const stepped = stepWheelDiameter(
        current,
        wheelAcc.current,
        event.deltaY,
        event.deltaMode,
        image.width,
        image.height,
      );
      wheelAcc.current = stepped.accumulator;
      if (drag.current) {
        drag.current = { x: event.clientX, y: event.clientY, circle: stepped.circle };
      }
      circleRef.current = stepped.circle;
      setCircle(stepped.circle);
    };
    node.addEventListener('wheel', onWheel, { passive: false });
    return () => node.removeEventListener('wheel', onWheel);
  }, [image.height, image.width]);

  useEffect(() => {
    const dialog = cropDialog();
    if (!dialog) return;
    dialog.tabIndex = -1;

    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Tab' || !dialog) return;
      const nodes = tabbables(dialog);
      if (nodes.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }
      const first = nodes[0];
      const last = nodes[nodes.length - 1];
      const active = document.activeElement;
      const inside = active instanceof Node && dialog.contains(active);
      if (event.shiftKey) {
        if (!inside || active === first) {
          event.preventDefault();
          last.focus();
        }
      } else if (!inside || active === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener('keydown', onKeyDown);
    const nodes = tabbables(dialog);
    (nodes[0] ?? dialog).focus();
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [locked]);

  function commitCircle(next: Circle) {
    circleRef.current = next;
    setCircle(next);
  }

  function onPointerDown(event: PointerEvent<HTMLDivElement>) {
    if (locked) return;
    if (typeof event.currentTarget.setPointerCapture === 'function') {
      try {
        event.currentTarget.setPointerCapture(event.pointerId);
      } catch {
        // В jsdom указатель не захватывается, сдвиг считается по clientX.
      }
    }
    setGrabbing(true);
    drag.current = { x: event.clientX, y: event.clientY, circle: circleRef.current };
  }

  function onPointerMove(event: PointerEvent<HTMLDivElement>) {
    const start = drag.current;
    if (!start || lockedRef.current) return;
    const width = previewRef.current?.clientWidth ?? previewPx;
    commitCircle(
      panCircle(
        start.circle,
        event.clientX - start.x,
        event.clientY - start.y,
        circleScreenPx(width),
        image.width,
        image.height,
      ),
    );
  }

  function onPointerUp() {
    drag.current = null;
    setGrabbing(false);
  }

  async function handleSave() {
    if (lock.current || saving) return;
    lock.current = true;
    setEncoding(true);
    setLocalError(undefined);
    try {
      const file = await encodeAvatarJpeg(image.source, squareOf(circle, image.width, image.height));
      if (!file) {
        setLocalError(AVATAR_UPLOAD_ERROR);
        return;
      }
      await onSave(file);
    } finally {
      lock.current = false;
      if (alive.current) setEncoding(false);
    }
  }

  return (
    <Modal
      isOpen
      size="lg"
      title={DIALOG_NAME}
      dialogLabel={DIALOG_NAME}
      onClose={locked ? () => {} : onCancel}
    >
      <div
        ref={previewRef}
        data-testid="avatar-crop-preview"
        className={`relative aspect-square w-full touch-none overflow-hidden bg-sand ${grabbing ? 'cursor-grabbing' : 'cursor-grab'}`}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerCancel={onPointerUp}
      >
        <canvas
          ref={canvasRef}
          className="pointer-events-none absolute max-w-none"
          style={{
            left: previewPx / 2 - circle.cx * scale,
            top: previewPx / 2 - circle.cy * scale,
            width: image.width * scale,
            height: image.height * scale,
          }}
        />
        <div className="pointer-events-none absolute inset-0" aria-hidden="true">
          <div
            className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-warm-white"
            style={{
              width: `calc(100% - ${CROP_PREVIEW_PAD * 2}px)`,
              height: `calc(100% - ${CROP_PREVIEW_PAD * 2}px)`,
              boxShadow: '0 0 0 1px #1A1A2E, 0 0 0 9999px rgba(20, 16, 12, 0.45)',
            }}
          />
        </div>
      </div>

      <label className="mt-4 block text-sm text-dark">
        Масштаб
        <input
          className={SCALE_SLIDER}
          type="range"
          min={0}
          max={span}
          step={1}
          value={sliderPos}
          disabled={locked}
          style={{
            background: `linear-gradient(to right, #2C5282 ${span === 0 ? 0 : (sliderPos / span) * 100}%, #F5F0E8 ${span === 0 ? 0 : (sliderPos / span) * 100}%)`,
          }}
          onChange={(event) => {
            if (locked) return;
            const next = maxD - Number(event.target.value);
            commitCircle(withDiameter(circleRef.current, next, image.width, image.height));
          }}
        />
      </label>

      {message && (
        <p role="alert" className="mt-3 text-sm text-red-500">
          {message}
        </p>
      )}

      <div className="mt-4 flex items-center justify-between gap-3">
        <Button type="button" variant="secondary" onClick={onCancel} disabled={locked}>
          Отмена
        </Button>
        <Button type="button" onClick={handleSave} disabled={locked} isLoading={locked}>
          Сохранить фото
        </Button>
      </div>
    </Modal>
  );
}
