import { addDays, addMonths, format, getDay, getDaysInMonth, startOfWeek } from 'date-fns';

const NOMINATIVE_MONTHS = [
  'январь',
  'февраль',
  'март',
  'апрель',
  'май',
  'июнь',
  'июль',
  'август',
  'сентябрь',
  'октябрь',
  'ноябрь',
  'декабрь',
] as const;

const GENITIVE_MONTHS = [
  'января',
  'февраля',
  'марта',
  'апреля',
  'мая',
  'июня',
  'июля',
  'августа',
  'сентября',
  'октября',
  'ноября',
  'декабря',
] as const;

export const WEEKDAY_LABELS = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс'] as const;

export type CalendarParts = { year: number; month: number; day: number };

export function parseCalendarIso(iso: string): CalendarParts | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (month < 1 || month > 12) return null;
  const probe = new Date(year, month - 1, day);
  if (probe.getFullYear() !== year || probe.getMonth() !== month - 1 || probe.getDate() !== day) return null;
  return { year, month, day };
}

function toLocalDate(iso: string): Date | null {
  const parts = parseCalendarIso(iso);
  if (!parts) return null;
  return new Date(parts.year, parts.month - 1, parts.day);
}

function fromLocalDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

/** Календарный день Europe/Moscow, без сдвига поясом браузера. */
export function moscowToday(now: Date = new Date()): string {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Europe/Moscow',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(now);
  const pick = (type: Intl.DateTimeFormatPartTypes) => parts.find((part) => part.type === type)?.value ?? '';
  return `${pick('year')}-${pick('month')}-${pick('day')}`;
}

export function addCalendarDays(iso: string, amount: number): string {
  const date = toLocalDate(iso);
  if (!date) return iso;
  return fromLocalDate(addDays(date, amount));
}

export function shiftCalendarMonth(year: number, month: number, delta: number): { year: number; month: number } {
  const next = addMonths(new Date(year, month - 1, 1), delta);
  return { year: next.getFullYear(), month: next.getMonth() + 1 };
}

export function isDayUnavailable(iso: string, minIso: string, maxIso: string): boolean {
  return iso < minIso || iso > maxIso;
}

export function weekStartMonday(iso: string): string {
  const date = toLocalDate(iso);
  if (!date) return iso;
  return fromLocalDate(startOfWeek(date, { weekStartsOn: 1 }));
}

export function monthGrid(year: number, month: number): (string | null)[][] {
  const first = new Date(year, month - 1, 1);
  const lead = (getDay(first) + 6) % 7;
  const count = getDaysInMonth(first);
  const cells: (string | null)[] = Array.from({ length: lead }, () => null);
  for (let day = 1; day <= count; day += 1) {
    cells.push(fromLocalDate(new Date(year, month - 1, day)));
  }
  while (cells.length % 7 !== 0) cells.push(null);
  const weeks: (string | null)[][] = [];
  for (let index = 0; index < cells.length; index += 7) weeks.push(cells.slice(index, index + 7));
  return weeks;
}

export function dayAccessibleName(iso: string): string {
  const parts = parseCalendarIso(iso);
  if (!parts) return iso;
  return `${parts.day} ${GENITIVE_MONTHS[parts.month - 1]} ${parts.year}`;
}

export function monthTitle(year: number, month: number): string {
  return `${NOMINATIVE_MONTHS[month - 1]} ${year}`;
}

export function monthKey(year: number, month: number): number {
  return year * 12 + month;
}
