import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import clsx from 'clsx';
import { CalendarFitProvider } from './calendarFit';
import {
  VIEWPORT_MARGIN,
  calendarChrome,
  calendarPanelHeight,
  calendarPanelWidth,
  clampPanelLeft,
  planCalendarPopover,
  planListPopover,
  type CalendarPlan,
  type ListPlan,
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

function listPosition(anchor: AnchorBox, plan: ListPlan): CSSProperties {
  const shared = { left: plan.left, width: plan.width };
  if (plan.placeBelow) return { ...shared, top: anchor.bottom + VIEWPORT_MARGIN };
  return { ...shared, bottom: window.innerHeight - anchor.top + VIEWPORT_MARGIN };
}

function calendarPosition(plan: CalendarPlan): CSSProperties {
  const maxWidth = Math.max(160, window.innerWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(plan.width, maxWidth);
  const left = clampPanelLeft(plan.anchorLeft, width, window.innerWidth);
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

function wheelDistance(event: WheelEvent, pageSize: number): number {
  if (event.deltaMode === WheelEvent.DOM_DELTA_LINE) return event.deltaY * 16;
  if (event.deltaMode === WheelEvent.DOM_DELTA_PAGE) return event.deltaY * pageSize;
  return event.deltaY;
}

function scrollListUnderCursor(panel: HTMLElement, event: WheelEvent): void {
  const scrollEl = panel.querySelector('[data-popup-scroll]');
  if (!(scrollEl instanceof HTMLElement) || !(event.target instanceof Node) || !scrollEl.contains(event.target)) return;
  const distance = wheelDistance(event, scrollEl.clientHeight);
  if (scrollEl.scrollHeight <= scrollEl.clientHeight + 1 || distance === 0) return;
  const max = scrollEl.scrollHeight - scrollEl.clientHeight;
  const next = Math.min(max, Math.max(0, scrollEl.scrollTop + distance));
  if (next !== scrollEl.scrollTop) scrollEl.scrollTop = next;
}

function isDocumentScroll(target: EventTarget | null, panel: HTMLElement | null): boolean {
  if (target instanceof Node) {
    if (panel?.contains(target)) return false;
    return target === document || target === document.documentElement || target === document.body;
  }
  return target != null;
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
  const [listContentHeight, setListContentHeight] = useState(0);

  useLayoutEffect(() => {
    const place = () => {
      const box = readAnchor(anchorRef);
      setAnchor(box);
      if (!contentSized || !box) {
        setPlan(null);
        return;
      }
      setPlan(
        planCalendarPopover({
          viewportWidth: window.innerWidth,
          viewportHeight: window.innerHeight,
          anchorTop: box.top,
          anchorBottom: box.bottom,
          anchorLeft: box.left,
          anchorWidth: box.width,
          wide,
          allowTwoMonths,
        }),
      );
    };

    place();
    window.addEventListener('resize', place);
    return () => window.removeEventListener('resize', place);
  }, [allowTwoMonths, anchorRef, contentSized, wide]);

  useLayoutEffect(() => {
    if (contentSized || !wide) return;
    const scrollEl = panelRef.current?.querySelector('[data-popup-scroll]');
    if (!(scrollEl instanceof HTMLElement)) return;
    const next = Math.ceil(scrollEl.scrollHeight);
    setListContentHeight((current) => (current === next ? current : next));
  }, [anchor, children, contentSized, wide]);

  useLayoutEffect(() => {
    const panel = panelRef.current;
    const blockWheel = (event: WheelEvent) => {
      if (!panel || !(event.target instanceof Node) || !panel.contains(event.target)) return;
      scrollListUnderCursor(panel, event);
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
    const onScroll = (event: Event) => {
      if (!isDocumentScroll(event.target, panelRef.current)) return;
      onClose();
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    document.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', onScroll);
      document.removeEventListener('scroll', onScroll);
    };
  }, [onClose]);

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

  const listPlan =
    !contentSized && wide && anchor
      ? planListPopover({
          viewportWidth: window.innerWidth,
          viewportHeight: window.innerHeight,
          anchorTop: anchor.top,
          anchorBottom: anchor.bottom,
          anchorLeft: anchor.left,
          anchorWidth: anchor.width,
          minWidth,
          contentHeight: listContentHeight,
        })
      : null;
  const chrome = calendarChrome(plan?.compact ?? false, plan?.months ?? 1);
  const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
  const mobileStyle: CSSProperties = {
    bottom: inset,
  };
  const listMax = wide ? (listPlan?.maxHeight ?? 0) : Math.max(120, Math.round(viewportHeight - inset - 48));
  const ready = contentSized ? plan != null : !wide || anchor != null;
  const place: 'above' | 'below' = contentSized
    ? plan?.placeBelow === false
      ? 'above'
      : 'below'
    : listPlan?.placeBelow === false
      ? 'above'
      : 'below';
  const positionStyle: CSSProperties = wide
    ? contentSized && plan
      ? calendarPosition(plan)
      : anchor && listPlan
        ? listPosition(anchor, listPlan)
        : {}
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
        data-place={wide ? place : undefined}
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
