import { useEffect, useRef, useState, type CSSProperties, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import clsx from 'clsx';

interface SearchPopupProps {
  id: string;
  wide: boolean;
  anchorRef: RefObject<HTMLElement | null>;
  dialog?: boolean;
  label?: string;
  keyboardInset?: boolean;
  /** На ширине от 768px календарь из двух месяцев не влезает в узкую ячейку. */
  minWidth?: number;
  onClose: () => void;
  children: ReactNode;
}

function desktopFrame(anchor: DOMRect | null, minWidth: number): {
  position: CSSProperties;
  maxHeight: number;
} {
  const margin = 8;
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const width = Math.min(Math.max(anchor?.width ?? 0, minWidth), Math.max(160, viewportWidth - margin * 2));
  let left = anchor?.left ?? margin;
  if (left + width > viewportWidth - margin) left = Math.max(margin, viewportWidth - margin - width);
  const anchorTop = anchor?.top ?? margin;
  const anchorBottom = anchor?.bottom ?? anchorTop;
  const spaceBelow = viewportHeight - anchorBottom - margin;
  const spaceAbove = anchorTop - margin;
  const placeBelow = spaceBelow >= 320 || spaceBelow >= spaceAbove;
  const maxHeight = Math.max(160, Math.floor(placeBelow ? spaceBelow : spaceAbove));
  const position = placeBelow
    ? { top: anchorBottom + margin, left, width }
    : { bottom: viewportHeight - anchorTop + margin, left, width };
  return { position, maxHeight };
}

function useKeyboardInset(active: boolean): number {
  const [inset, setInset] = useState(0);

  useEffect(() => {
    if (!active) return;
    const viewport = window.visualViewport;
    if (!viewport) return;
    const update = () => {
      const covered = window.innerHeight - viewport.height - viewport.offsetTop;
      setInset(covered > 0 ? Math.round(covered) : 0);
    };
    update();
    viewport.addEventListener('resize', update);
    viewport.addEventListener('scroll', update);
    return () => {
      viewport.removeEventListener('resize', update);
      viewport.removeEventListener('scroll', update);
    };
  }, [active]);

  return inset;
}

function DragHandle({ onClose }: { onClose: () => void }) {
  const startY = useRef<number | null>(null);

  return (
    <div
      className="flex touch-none justify-center pb-1 pt-3"
      aria-hidden="true"
      onPointerDown={(event) => {
        startY.current = event.clientY;
      }}
      onPointerUp={(event) => {
        if (startY.current != null && event.clientY - startY.current > 48) onClose();
        startY.current = null;
      }}
    >
      <span className="h-1.5 w-12 rounded-full bg-dark/30" />
    </div>
  );
}

export default function SearchPopup({
  id,
  wide,
  anchorRef,
  dialog = false,
  label,
  keyboardInset = false,
  minWidth = 280,
  onClose,
  children,
}: SearchPopupProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const inset = useKeyboardInset(!wide && keyboardInset);
  const [anchor, setAnchor] = useState<DOMRect | null>(null);

  useEffect(() => {
    const update = () => setAnchor(anchorRef.current?.getBoundingClientRect() ?? null);
    update();
    window.addEventListener('resize', update);
    window.addEventListener('scroll', update, true);
    return () => {
      window.removeEventListener('resize', update);
      window.removeEventListener('scroll', update, true);
    };
  }, [anchorRef]);

  useEffect(() => {
    function onPointerDown(event: MouseEvent) {
      const target = event.target as Node | null;
      if (!target) return;
      if (panelRef.current?.contains(target)) return;
      if (anchorRef.current?.contains(target)) return;
      onClose();
    }
    document.addEventListener('mousedown', onPointerDown);
    return () => document.removeEventListener('mousedown', onPointerDown);
  }, [anchorRef, onClose]);

  const frame = desktopFrame(anchor, minWidth);
  const viewportHeight = window.visualViewport?.height;
  const mobileStyle = {
    bottom: inset,
    maxHeight: viewportHeight ? Math.max(240, Math.round(viewportHeight - inset - 8)) : undefined,
  };

  return createPortal(
    <>
      <div className="pointer-events-none fixed inset-0 z-50 bg-dark/40 md:bg-transparent" />
      <div
        ref={panelRef}
        id={id}
        role={dialog ? 'dialog' : undefined}
        aria-label={dialog ? label : undefined}
        style={wide ? frame.position : mobileStyle}
        className={clsx(
          'z-50 border border-sand bg-cream font-sans text-dark shadow-modal',
          wide
            ? 'fixed rounded-[16px]'
            : 'fixed inset-x-0 bottom-0 w-full max-h-[85dvh] max-w-none rounded-t-[20px]',
        )}
      >
        {wide ? null : <DragHandle onClose={onClose} />}
        <div
          className={clsx(
            'overflow-y-auto px-3',
            wide ? 'py-3' : 'pb-[max(1.25rem,env(safe-area-inset-bottom))] pt-1',
          )}
          style={wide ? { maxHeight: frame.maxHeight } : undefined}
        >
          {children}
        </div>
      </div>
    </>,
    document.body,
  );
}
