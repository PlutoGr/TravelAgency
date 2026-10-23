/**
 * Геометрия попапа у поля: панель над полем или под ним.
 * Документ не прокручивается и не блокируется.
 * Календарь без внутренней прокрутки: обычные ячейки, затем плотные,
 * затем один месяц, если два не входят по ширине. Два месяца стоят рядом,
 * поэтому узкая высота сама по себе не лечится отключением второго месяца.
 * Список городов ограничен свободным местом и прокручивается внутри,
 * только если пункты не помещаются.
 */

export const VIEWPORT_MARGIN = 8;

const WEEK_ROWS = 6;
const BORDER = 2;

export type CalendarPlan = {
  months: 1 | 2;
  compact: boolean;
  placeBelow: boolean;
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
};

export type ListPlanInput = {
  viewportWidth: number;
  viewportHeight: number;
  anchorTop: number;
  anchorBottom: number;
  anchorLeft: number;
  anchorWidth: number;
  minWidth: number;
  contentHeight: number;
};

export type ListPlan = {
  placeBelow: boolean;
  maxHeight: number;
  left: number;
  width: number;
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
  placeBelow: boolean,
): CalendarPlan {
  return {
    months: scale.months,
    compact: scale.compact,
    placeBelow,
    width: calendarPanelWidth(scale.months, scale.compact),
    height: calendarPanelHeight(scale.compact, shellOf(input.wide)),
    anchorTop: top,
    anchorBottom: bottom,
    anchorLeft: input.anchorLeft,
    anchorWidth: input.anchorWidth,
  };
}

function fitAt(input: CalendarPlanInput, top: number, bottom: number): CalendarPlan | null {
  const options = scalesFor(input);
  const list = options.length > 0 ? options : [{ months: 1 as const, compact: true }];
  for (const scale of list) {
    const height = calendarPanelHeight(scale.compact, shellOf(input.wide));
    const side = sideOf(top, bottom, height, input.viewportHeight);
    if (!side) continue;
    return finish(scale, input, top, bottom, side === 'below');
  }
  return null;
}

export function clampPanelLeft(anchorLeft: number, panelWidth: number, viewportWidth: number): number {
  const maxWidth = Math.max(160, viewportWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(panelWidth, maxWidth);
  let left = anchorLeft;
  if (left + width > viewportWidth - VIEWPORT_MARGIN) {
    left = Math.max(VIEWPORT_MARGIN, viewportWidth - VIEWPORT_MARGIN - width);
  }
  if (left < VIEWPORT_MARGIN) left = VIEWPORT_MARGIN;
  return left;
}

export function planListPopover(input: ListPlanInput): ListPlan {
  const maxWidth = Math.max(160, input.viewportWidth - VIEWPORT_MARGIN * 2);
  const width = Math.min(Math.max(input.anchorWidth, input.minWidth), maxWidth);
  const spaceBelow = input.viewportHeight - input.anchorBottom - VIEWPORT_MARGIN;
  const spaceAbove = input.anchorTop - VIEWPORT_MARGIN;
  const content = input.contentHeight;
  const fitsBelow = content > 0 && content <= spaceBelow;
  const fitsAbove = content > 0 && content <= spaceAbove;
  const placeBelow = fitsBelow || (!fitsAbove && spaceBelow >= spaceAbove);
  const available = Math.max(0, placeBelow ? spaceBelow : spaceAbove);
  return {
    placeBelow,
    maxHeight: Math.floor(available),
    left: clampPanelLeft(input.anchorLeft, width, input.viewportWidth),
    width,
  };
}

export function planCalendarPopover(input: CalendarPlanInput): CalendarPlan {
  if (!input.wide) {
    const regularFits = calendarPanelHeight(false, 'sheet') <= input.viewportHeight - VIEWPORT_MARGIN;
    return finish({ months: 1, compact: !regularFits }, input, input.anchorTop, input.anchorBottom, true);
  }

  const here = fitAt(input, input.anchorTop, input.anchorBottom);
  if (here) return here;

  const fallback: Scale =
    input.allowTwoMonths && widthFits(2, true, input.viewportWidth)
      ? { months: 2, compact: true }
      : { months: 1, compact: true };
  const { below, above } = room(input.anchorTop, input.anchorBottom, input.viewportHeight);
  return finish(fallback, input, input.anchorTop, input.anchorBottom, below >= above);
}
