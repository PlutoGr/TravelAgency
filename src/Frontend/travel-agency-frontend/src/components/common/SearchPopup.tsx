import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import clsx from 'clsx';
import { CalendarFitProvider } from './calendarFit';
import {
  VIEWPORT_MARGIN,
  calendarChrome,
  calendarPanelHeight,
  calendarPanelWidth,
  planCalendarPopover,
  readScrollBudget,
  type CalendarPlan,
} from './searchPopupLayout';

interface SearchPopupProps {
  id: string;
  wide: boolean;
  anchorRef: RefObject<HTMLElement | null>;
  dialog?: boolean;
  label?: string;
  keyboardInset?: boolean;
  /** Минимальная ширина панели списка. Для календаря ширина берётся из раскладки. */
  minWidth?: number;
  /** Календарь: панель по содержимому, без внутреннего скролла. */
  contentSized?: boolean;
  /** Два месяца, если пара входит в окно. Для одной даты оставлять false. */
  allowTwoMonths?: boolean;
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

function listFrame(anchor: AnchorBox | null, minWidth: number): { position: CSSProperties; maxHeight: number } {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const maxWidth = Math.max(160, viewportWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(Math.max(anchor?.width ?? 0, minWidth), maxWidth);
  const left = anchor ? clampLeft(anchor.left, width) : VIEWPORT_MARGIN;
  const anchorTop = anchor?.top ?? VIEWPORT_MARGIN;
  const anchorBottom = anchor?.bottom ?? anchorTop;
  const spaceBelow = viewportHeight - anchorBottom - VIEWPORT_MARGIN;
  const spaceAbove = anchorTop - VIEWPORT_MARGIN;
  const placeBelow = spaceBelow >= 240 || spaceBelow >= spaceAbove;
  const maxHeight = Math.max(120, Math.floor(placeBelow ? spaceBelow : spaceAbove));
  const position: CSSProperties = placeBelow
    ? { top: anchorBottom + VIEWPORT_MARGIN, left, width }
    : { bottom: viewportHeight - anchorTop + VIEWPORT_MARGIN, left, width };
  return { position, maxHeight };
}

function calendarPosition(plan: CalendarPlan): CSSProperties {
  const maxWidth = Math.max(160, window.innerWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(plan.width, maxWidth);
  const left = clampLeft(plan.anchorLeft, width);
  if (plan.placeBelow) {
    return { top: plan.anchorBottom + VIEWPORT_MARGIN, left, width, maxWidth };
  }
  return {
    bottom: window.innerHeight - plan.anchorTop + VIEWPORT_MARGIN,
    left,
    width,
    maxWidth,
  };
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
      className="flex touch-none justify-center pb-1 pt-2"
      aria-hidden="true"
      onPointerDown={(event) => {
        startY.current = event.clientY;
      }}
      onPointerUp={(event) => {
        if (startY.current != null && event.clientY - startY.current > 48) onClose();
        startY.current = null;
      }}
    >
      <span className="h-1 w-10 rounded-full bg-dark/30" />
    </div>
  );
}

function canConsumeWheel(panel: HTMLElement | null, event: WheelEvent): boolean {
  if (!panel || !(event.target instanceof Node) || !panel.contains(event.target)) return false;
  const scrollEl = panel.querySelector('[data-popup-scroll]');
  if (!(scrollEl instanceof HTMLElement) || !scrollEl.contains(event.target)) return false;
  if (scrollEl.scrollHeight <= scrollEl.clientHeight + 1) return false;
  if (event.deltaY < 0) return scrollEl.scrollTop > 0;
  if (event.deltaY > 0) return scrollEl.scrollTop + scrollEl.clientHeight < scrollEl.scrollHeight - 1;
  return false;
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
  allowTwoMonths = false,
  onClose,
  children,
}: SearchPopupProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const inset = useKeyboardInset(!wide && keyboardInset);
  const [anchor, setAnchor] = useState<AnchorBox | null>(null);
  const [plan, setPlan] = useState<CalendarPlan | null>(null);

  useLayoutEffect(() => {
    const body = document.body;
    const root = document.documentElement;
    const previous = { body: body.style.overflow, root: root.style.overflow };

    const lock = () => {
      body.style.overflow = 'hidden';
      root.style.overflow = 'hidden';
    };

    const place = () => {
      const box = readAnchor(anchorRef);
      setAnchor(box);
      if (!contentSized) {
        lock();
        return;
      }
      if (!box) {
        lock();
        return;
      }
      root.style.overflow = previous.root;
      body.style.overflow = previous.body;
      const budget = readScrollBudget();
      let next = planCalendarPopover({
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight,
        anchorTop: box.top,
        anchorBottom: box.bottom,
        anchorLeft: box.left,
        anchorWidth: box.width,
        wide,
        allowTwoMonths,
        scrollY: budget.scrollY,
        maxScrollDown: budget.maxScrollDown,
      });
      if (next.scrollDelta !== 0) {
        window.scrollBy({ top: next.scrollDelta, left: 0, behavior: 'instant' });
        const moved = readAnchor(anchorRef);
        if (moved) {
          next = planCalendarPopover({
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            anchorTop: moved.top,
            anchorBottom: moved.bottom,
            anchorLeft: moved.left,
            anchorWidth: moved.width,
            wide,
            allowTwoMonths,
            scrollY: window.scrollY || document.documentElement.scrollTop || 0,
            maxScrollDown: 0,
          });
        }
      }
      lock();
      setPlan(next);
    };

    place();
    window.addEventListener('resize', place);
    return () => {
      window.removeEventListener('resize', place);
      body.style.overflow = previous.body;
      root.style.overflow = previous.root;
    };
  }, [allowTwoMonths, anchorRef, contentSized, wide]);

  useLayoutEffect(() => {
    const panel = panelRef.current;
    const blockWheel = (event: WheelEvent) => {
      if (canConsumeWheel(panel, event)) return;
      event.preventDefault();
    };
    const blockTouch = (event: TouchEvent) => {
      const scrollEl = panel?.querySelector('[data-popup-scroll]');
      if (
        scrollEl instanceof HTMLElement &&
        event.target instanceof Node &&
        scrollEl.contains(event.target) &&
        scrollEl.scrollHeight > scrollEl.clientHeight + 1
      ) {
        return;
      }
      if (event.cancelable) event.preventDefault();
    };
    window.addEventListener('wheel', blockWheel, { capture: true, passive: false });
    window.addEventListener('touchmove', blockTouch, { capture: true, passive: false });
    return () => {
      window.removeEventListener('wheel', blockWheel, { capture: true });
      window.removeEventListener('touchmove', blockTouch, { capture: true });
    };
  }, []);

  useLayoutEffect(() => {
    if (!contentSized || !wide || !plan || !panelRef.current) return;
    const measured = panelRef.current.getBoundingClientRect().height;
    if (measured <= 1) return;
    const box = readAnchor(anchorRef);
    if (!box) return;
    const available = plan.placeBelow
      ? window.innerHeight - box.bottom - VIEWPORT_MARGIN
      : box.top - VIEWPORT_MARGIN;
    if (measured <= available + 1) return;

    setPlan((current) => {
      if (!current) return current;
      if (!current.compact) {
        return {
          ...current,
          compact: true,
          width: calendarPanelWidth(current.months, true),
          height: calendarPanelHeight(true, 'desktop'),
        };
      }
      if (current.months === 2) {
        return {
          ...current,
          months: 1,
          width: calendarPanelWidth(1, true),
          height: calendarPanelHeight(true, 'desktop'),
        };
      }
      const other = current.placeBelow
        ? box.top - VIEWPORT_MARGIN
        : window.innerHeight - box.bottom - VIEWPORT_MARGIN;
      if (other >= measured && current.placeBelow) {
        return {
          ...current,
          placeBelow: false,
          anchorTop: box.top,
          anchorBottom: box.bottom,
          scrollDelta: 0,
        };
      }
      return current;
    });
  }, [anchorRef, contentSized, plan, wide]);

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

  const frame = listFrame(anchor, minWidth);
  const chrome = calendarChrome(plan?.compact ?? false, plan?.months ?? 1);
  const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
  const mobileStyle: CSSProperties = {
    bottom: inset,
  };
  const listMax = wide
    ? frame.maxHeight
    : Math.max(120, Math.round(viewportHeight - inset - 48));
  const ready = contentSized ? plan != null : !wide || anchor != null;
  const positionStyle: CSSProperties = wide
    ? contentSized && plan
      ? calendarPosition(plan)
      : frame.position
    : mobileStyle;

  return createPortal(
    <>
      <div className="pointer-events-none fixed inset-0 z-50 bg-dark/40 md:bg-transparent" />
      <div
        ref={panelRef}
        id={id}
        role={dialog ? 'dialog' : undefined}
        aria-label={dialog ? label : undefined}
        data-anchored={ready ? 'true' : 'false'}
        data-place={contentSized ? (plan?.placeBelow === false ? 'above' : 'below') : undefined}
        style={{
          ...positionStyle,
          visibility: ready ? 'visible' : 'hidden',
        }}
        className={clsx(
          'z-50 border border-sand bg-cream font-sans text-dark shadow-modal',
          wide ? 'fixed rounded-[16px]' : 'fixed inset-x-0 bottom-0 w-full max-w-none rounded-t-[20px]',
          !wide && !contentSized && 'overflow-hidden',
        )}
      >
        {wide ? null : <DragHandle onClose={onClose} />}
        <CalendarFitProvider value={contentSized && plan ? { months: plan.months, compact: plan.compact } : null}>
          <div
            data-popup-scroll={contentSized ? undefined : ''}
            className={clsx(
              contentSized
                ? clsx('overflow-visible', wide ? chrome.popupPad : 'px-2 pt-1')
                : 'overflow-y-auto overscroll-contain px-2 py-1',
              !wide && 'pb-[max(0.5rem,env(safe-area-inset-bottom))]',
              !wide && !contentSized && 'pt-1',
            )}
            style={contentSized ? undefined : { maxHeight: listMax }}
          >
            {children}
          </div>
        </CalendarFitProvider>
      </div>
    </>,
    document.body,
  );
}
