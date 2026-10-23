import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import clsx from 'clsx';

interface SearchPopupProps {
  id: string;
  wide: boolean;
  anchorRef: RefObject<HTMLElement | null>;
  dialog?: boolean;
  label?: string;
  keyboardInset?: boolean;
  /** Минимальная ширина панели. Игнорируется, когда ширина берётся по содержимому. */
  minWidth?: number;
  /** Ширина по содержимому и не шире окна. Жёсткий minWidth вроде 700px не задаётся. */
  contentSized?: boolean;
  onClose: () => void;
  children: ReactNode;
}

type AnchorBox = {
  top: number;
  left: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
};

const VIEWPORT_MARGIN = 8;

function readAnchor(anchorRef: RefObject<HTMLElement | null>): AnchorBox | null {
  const node = anchorRef.current;
  if (!node) return null;
  const rect = node.getBoundingClientRect();
  return {
    top: rect.top,
    left: rect.left,
    right: rect.right,
    bottom: rect.bottom,
    width: rect.width,
    height: rect.height,
  };
}

function clampLeft(anchorLeft: number, panelWidth: number): number {
  const viewportWidth = window.innerWidth;
  const maxWidth = Math.max(160, viewportWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(panelWidth, maxWidth);
  let left = anchorLeft;
  if (left + width > viewportWidth - VIEWPORT_MARGIN) {
    left = Math.max(VIEWPORT_MARGIN, viewportWidth - VIEWPORT_MARGIN - width);
  }
  if (left < VIEWPORT_MARGIN) left = VIEWPORT_MARGIN;
  return left;
}

function desktopFrame(
  anchor: AnchorBox | null,
  minWidth: number,
  contentSized: boolean,
  panelWidth: number,
): { position: CSSProperties; maxHeight: number } {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const maxWidth = Math.max(160, viewportWidth - VIEWPORT_MARGIN * 2);
  const anchorWidth = anchor?.width ?? 0;
  const width = contentSized ? undefined : Math.min(Math.max(anchorWidth, minWidth), maxWidth);
  const left = anchor
    ? clampLeft(anchor.left, contentSized ? panelWidth || anchorWidth : width ?? anchorWidth)
    : VIEWPORT_MARGIN;
  const anchorTop = anchor?.top ?? VIEWPORT_MARGIN;
  const anchorBottom = anchor?.bottom ?? anchorTop;
  const spaceBelow = viewportHeight - anchorBottom - VIEWPORT_MARGIN;
  const spaceAbove = anchorTop - VIEWPORT_MARGIN;
  const placeBelow = spaceBelow >= 320 || spaceBelow >= spaceAbove;
  const maxHeight = Math.max(160, Math.floor(placeBelow ? spaceBelow : spaceAbove));
  const position: CSSProperties = placeBelow
    ? { top: anchorBottom + VIEWPORT_MARGIN, left, width, maxWidth }
    : { bottom: viewportHeight - anchorTop + VIEWPORT_MARGIN, left, width, maxWidth };
  if (contentSized) {
    position.width = 'max-content';
    position.minWidth = Math.min(anchorWidth, maxWidth) || undefined;
  }
  return { position, maxHeight };
}

function useKeyboardInset(active: boolean): number {
  const [inset, setInset] = useState(0);

  useLayoutEffect(() => {
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
  contentSized = false,
  onClose,
  children,
}: SearchPopupProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const inset = useKeyboardInset(!wide && keyboardInset);
  const [anchor, setAnchor] = useState<AnchorBox | null>(null);
  const [panelWidth, setPanelWidth] = useState(0);

  useLayoutEffect(() => {
    const update = () => {
      // До первой отрисовки панель скрыта: координаты появляются в этом эффекте, не в углу экрана.
      setAnchor(readAnchor(anchorRef));
      const width = panelRef.current?.getBoundingClientRect().width ?? 0;
      setPanelWidth((current) => (current === width ? current : width));
    };
    update();
    window.addEventListener('resize', update);
    window.addEventListener('scroll', update, true);
    return () => {
      window.removeEventListener('resize', update);
      window.removeEventListener('scroll', update, true);
    };
  }, [anchorRef, children, wide, contentSized]);

  useLayoutEffect(() => {
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

  const frame = desktopFrame(anchor, minWidth, contentSized, panelWidth);
  const viewportHeight = window.visualViewport?.height;
  const mobileStyle = {
    bottom: inset,
    maxHeight: viewportHeight ? Math.max(240, Math.round(viewportHeight - inset - 8)) : undefined,
  };
  const positioned = !wide || anchor != null;

  return createPortal(
    <>
      <div className="pointer-events-none fixed inset-0 z-50 bg-dark/40 md:bg-transparent" />
      <div
        ref={panelRef}
        id={id}
        role={dialog ? 'dialog' : undefined}
        aria-label={dialog ? label : undefined}
        data-anchored={positioned ? 'true' : 'false'}
        style={{
          ...(wide ? frame.position : mobileStyle),
          visibility: positioned ? 'visible' : 'hidden',
        }}
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
            'overflow-x-hidden overflow-y-auto px-3',
            wide ? 'py-3' : 'pb-[max(1.25rem,env(safe-area-inset-bottom))] pt-1',
          )}
          style={wide ? { maxHeight: frame.maxHeight } : { maxHeight: 'calc(85dvh - 2.5rem)' }}
        >
          {children}
        </div>
      </div>
    </>,
    document.body,
  );
}
