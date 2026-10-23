/**
 * Геометрия попапа календаря: панель целиком над полем или под ним,
 * без внутреннего скролла. Два месяца стоят рядом, поэтому узкая высота
 * сама по себе не лечится отключением второго месяца: сначала компактные
 * ячейки, затем переворот, затем сдвиг якоря. Второй месяц убирается,
 * когда пара не входит в окно по ширине.
 */

export const VIEWPORT_MARGIN = 8;
export const HEADER_CLEARANCE = 88;

const WEEK_ROWS = 6;
const BORDER = 2;

export type CalendarPlan = {
  months: 1 | 2;
  compact: boolean;
  placeBelow: boolean;
  scrollDelta: number;
  width: number;
  height: number;
  anchorTop: number;
  anchorBottom: number;
  anchorLeft: number;
  anchorWidth: number;
};

export type CalendarPlanInput = {
  viewportWidth: number;
  viewportHeight: number;
  anchorTop: number;
  anchorBottom: number;
  anchorLeft: number;
  anchorWidth: number;
  wide: boolean;
  allowTwoMonths: boolean;
  scrollY: number;
  maxScrollDown: number;
  headerClearance?: number;
};

type Scale = { months: 1 | 2; compact: boolean };

type Chrome = {
  cell: string;
  box: string;
  weekday: string;
  caption: string;
  nav: string;
  navButton: string;
  dayButton: string;
  reset: string;
  months: string;
  popupPad: string;
};

/** Классы и пиксели совпадают: size-10 = 40, size-9 = 36, gap-x-4 = 16, gap-x-3 = 12. */
export function calendarChrome(compact: boolean, months: 1 | 2): Chrome {
  const cell = compact ? 'size-9' : 'size-10';
  const box = compact ? 'h-9 w-9' : 'h-10 w-10';
  return {
    cell,
    box,
    weekday: compact
      ? 'h-5 w-9 p-0 text-center text-[11px] font-medium leading-5 text-[#4B5563]'
      : 'h-6 w-10 p-0 text-center text-xs font-medium leading-6 text-[#4B5563]',
    caption: compact
      ? 'mb-0.5 text-center text-sm font-semibold leading-4 text-dark'
      : 'mb-1 text-center text-sm font-semibold leading-5 text-dark',
    nav: compact
      ? 'col-span-full mb-0.5 flex items-center justify-between'
      : 'col-span-full mb-1 flex items-center justify-between',
    navButton: `inline-flex ${cell} items-center justify-center rounded-full text-dark hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary aria-disabled:opacity-40`,
    dayButton: `inline-flex ${cell} items-center justify-center text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary`,
    reset: compact
      ? 'mt-1 flex min-h-9 w-full items-center justify-center rounded-[12px] text-sm font-medium text-primary hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary'
      : 'mt-2 flex min-h-10 w-full items-center justify-center rounded-[12px] text-sm font-medium text-primary hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
    months: months > 1 ? (compact ? 'grid grid-cols-2 gap-x-3' : 'grid grid-cols-2 gap-x-4') : 'grid grid-cols-1',
    popupPad: compact ? 'px-2 py-1' : 'px-2 py-2',
  };
}

function metrics(compact: boolean) {
  const cell = compact ? 36 : 40;
  const navMargin = compact ? 2 : 4;
  const caption = compact ? 16 : 20;
  const captionMargin = compact ? 2 : 4;
  const weekday = compact ? 20 : 24;
  const resetMargin = compact ? 4 : 8;
  const padY = compact ? 8 : 16;
  const gap = compact ? 12 : 16;
  const padX = 16;
  const content = cell + navMargin + caption + captionMargin + weekday + WEEK_ROWS * cell + resetMargin + cell;
  return { cell, gap, padX, padY, content };
}

export function calendarPanelHeight(compact: boolean, shell: 'desktop' | 'sheet'): number {
  const { content, padY } = metrics(compact);
  if (shell === 'desktop') return content + padY + BORDER;
  return content + 16 + 12 + BORDER;
}

export function calendarPanelWidth(months: 1 | 2, compact: boolean): number {
  const { cell, gap, padX } = metrics(compact);
  const between = months === 2 ? gap : 0;
  return padX + months * 7 * cell + between + BORDER;
}

function shellOf(wide: boolean): 'desktop' | 'sheet' {
  return wide ? 'desktop' : 'sheet';
}

function widthFits(months: 1 | 2, compact: boolean, viewportWidth: number): boolean {
  return calendarPanelWidth(months, compact) + VIEWPORT_MARGIN * 2 <= viewportWidth;
}

function room(top: number, bottom: number, viewportHeight: number) {
  return {
    below: viewportHeight - bottom - VIEWPORT_MARGIN,
    above: top - VIEWPORT_MARGIN,
  };
}

function sideOf(
  top: number,
  bottom: number,
  height: number,
  viewportHeight: number,
): 'below' | 'above' | null {
  const { below, above } = room(top, bottom, viewportHeight);
  if (below >= height) return 'below';
  if (above >= height) return 'above';
  return null;
}

function scalesFor(input: CalendarPlanInput): Scale[] {
  const all: Scale[] = [
    { months: 2, compact: false },
    { months: 2, compact: true },
    { months: 1, compact: false },
    { months: 1, compact: true },
  ];
  return all.filter((scale) => {
    if (scale.months === 2 && (!input.allowTwoMonths || !input.wide)) return false;
    return widthFits(scale.months, scale.compact, input.viewportWidth);
  });
}

function finish(
  scale: Scale,
  input: CalendarPlanInput,
  top: number,
  bottom: number,
  scrollDelta: number,
  placeBelow: boolean,
): CalendarPlan {
  return {
    months: scale.months,
    compact: scale.compact,
    placeBelow,
    scrollDelta,
    width: calendarPanelWidth(scale.months, scale.compact),
    height: calendarPanelHeight(scale.compact, shellOf(input.wide)),
    anchorTop: top,
    anchorBottom: bottom,
    anchorLeft: input.anchorLeft,
    anchorWidth: input.anchorWidth,
  };
}

function fitAt(
  input: CalendarPlanInput,
  top: number,
  bottom: number,
  scrollDelta: number,
): CalendarPlan | null {
  const options = scalesFor(input);
  const list = options.length > 0 ? options : [{ months: 1 as const, compact: true }];
  for (const scale of list) {
    const height = calendarPanelHeight(scale.compact, shellOf(input.wide));
    const side = sideOf(top, bottom, height, input.viewportHeight);
    if (!side) continue;
    return finish(scale, input, top, bottom, scrollDelta, side === 'below');
  }
  return null;
}

function clampDelta(delta: number, input: CalendarPlanInput): number | null {
  if (delta > input.maxScrollDown + 0.5 || delta < -input.scrollY - 0.5) return null;
  return delta;
}

export function readScrollBudget(): { scrollY: number; maxScrollDown: number } {
  const scrollY = window.scrollY || document.documentElement.scrollTop || 0;
  const maxScrollDown = Math.max(0, document.documentElement.scrollHeight - window.innerHeight - scrollY);
  return { scrollY, maxScrollDown };
}

export function planCalendarPopover(input: CalendarPlanInput): CalendarPlan {
  const header = input.headerClearance ?? HEADER_CLEARANCE;

  if (!input.wide) {
    const regularFits = calendarPanelHeight(false, 'sheet') <= input.viewportHeight - VIEWPORT_MARGIN;
    return finish(
      { months: 1, compact: !regularFits },
      input,
      input.anchorTop,
      input.anchorBottom,
      0,
      true,
    );
  }

  const here = fitAt(input, input.anchorTop, input.anchorBottom, 0);
  if (here) return here;

  const smallest = scalesFor(input).at(-1) ?? { months: 1 as const, compact: true };
  const need = calendarPanelHeight(smallest.compact, 'desktop');
  const upperTarget = Math.max(header, input.viewportHeight / 3);
  const attempts = [
    input.anchorTop - upperTarget,
    need + VIEWPORT_MARGIN - (input.viewportHeight - input.anchorBottom),
    input.anchorTop - (need + VIEWPORT_MARGIN),
    input.anchorTop - header,
  ];

  const seen = new Set<string>();
  for (const raw of attempts) {
    const delta = clampDelta(raw, input);
    if (delta == null || Math.abs(delta) < 0.5) continue;
    const key = delta.toFixed(2);
    if (seen.has(key)) continue;
    seen.add(key);
    const top = input.anchorTop - delta;
    const bottom = input.anchorBottom - delta;
    if (top < header - 0.5) continue;
    if (bottom > input.viewportHeight - VIEWPORT_MARGIN) continue;
    const fitted = fitAt(input, top, bottom, delta);
    if (fitted) return fitted;
  }

  const fallback: Scale =
    input.allowTwoMonths && widthFits(2, true, input.viewportWidth)
      ? { months: 2, compact: true }
      : { months: 1, compact: true };
  const { below, above } = room(input.anchorTop, input.anchorBottom, input.viewportHeight);
  return finish(fallback, input, input.anchorTop, input.anchorBottom, 0, below >= above);
}
