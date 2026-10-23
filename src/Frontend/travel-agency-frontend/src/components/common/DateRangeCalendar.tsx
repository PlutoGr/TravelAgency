import {
  useEffect,
  useMemo,
  useRef,
  type ButtonHTMLAttributes,
  type HTMLAttributes,
  type KeyboardEvent as ReactKeyboardEvent,
} from 'react';
import {
  DayPicker,
  type DayButtonProps,
  type DayProps,
  type Matcher,
} from 'react-day-picker';
import { ru } from 'react-day-picker/locale/ru';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import clsx from 'clsx';
import { DEPARTURE_DATE_MAX } from '@/utils/catalogQuery';
import {
  WEEKDAY_LABELS,
  dayAccessibleName,
  monthTitle,
  moscowToday,
  parseCalendarIso,
  shiftCalendarMonth,
} from '@/utils/homeCalendar';
import { useCalendarFit } from './calendarFit';
import { calendarChrome } from './searchPopupLayout';
import { useWideScreen } from './useWideScreen';

export type DateRange = {
  from?: string;
  to?: string;
};

interface DateRangeCalendarProps {
  value: DateRange;
  onChange: (value: DateRange) => void;
  onClose: () => void;
  /** Одна дата или диапазон. По умолчанию диапазон, как на главной. */
  mode?: 'single' | 'range';
  /** Включительно. Для диапазона по умолчанию сегодня по календарю Europe/Moscow. */
  minDate?: string;
  /** Включительно. Для диапазона по умолчанию 2100-12-31. */
  maxDate?: string;
}

function isoToDate(iso: string): Date | undefined {
  const parts = parseCalendarIso(iso);
  if (!parts) return undefined;
  return new Date(parts.year, parts.month - 1, parts.day);
}

function dateToIso(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function monthStart(iso: string): Date | undefined {
  const parts = parseCalendarIso(iso);
  if (!parts) return undefined;
  return new Date(parts.year, parts.month - 1, 1);
}

function monthAfter(iso: string): Date | undefined {
  const parts = parseCalendarIso(iso);
  if (!parts) return undefined;
  const next = shiftCalendarMonth(parts.year, parts.month, 1);
  return new Date(next.year, next.month - 1, 1);
}

function inBounds(iso: string, min?: string, max?: string): boolean {
  if (min && iso < min) return false;
  if (max && iso > max) return false;
  return true;
}

function CalendarChevron({
  orientation,
  className,
}: {
  orientation?: 'up' | 'down' | 'left' | 'right';
  className?: string;
  size?: number;
  disabled?: boolean;
}) {
  const Icon = orientation === 'right' || orientation === 'down' ? ChevronRight : ChevronLeft;
  return <Icon size={18} aria-hidden="true" className={clsx('pointer-events-none', className)} />;
}

function omitKeys<T extends object>(source: T, keys: readonly string[]): T {
  const next = { ...source };
  for (const key of keys) delete (next as Record<string, unknown>)[key];
  return next;
}

function CalendarCaption(props: HTMLAttributes<HTMLSpanElement>) {
  const fit = useCalendarFit();
  const { className, children, ...rest } = omitKeys(props, ['role']);
  return (
    <h2 className={clsx(calendarChrome(fit?.compact ?? false, 1).caption, className)} {...rest}>
      {children}
    </h2>
  );
}

function CalendarNavButton(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  const blocked = props['aria-disabled'] === true || props['aria-disabled'] === 'true';
  return <button {...props} disabled={blocked || props.disabled} />;
}

function CalendarDayCell(props: DayProps) {
  return <td {...omitKeys(props, ['day', 'modifiers', 'role'])} role="presentation" />;
}

function CalendarDayButton(props: DayButtonProps) {
  const { modifiers, className } = props;
  const buttonProps = omitKeys(props, ['day', 'modifiers']);
  const ref = useRef<HTMLButtonElement>(null);
  const edge = Boolean(modifiers.range_start || modifiers.range_end || (modifiers.selected && !modifiers.range_middle));
  const middle = Boolean(modifiers.range_middle);

  useEffect(() => {
    if (modifiers.focused) ref.current?.focus();
  }, [modifiers.focused]);

  return (
    <button
      ref={ref}
      {...buttonProps}
      role="gridcell"
      className={clsx(
        edge && 'rounded-full bg-primary text-white',
        middle && !edge && 'bg-transparent text-dark',
        !edge && !middle && !modifiers.disabled && 'rounded-full text-dark hover:bg-sand',
        modifiers.today && !edge && 'rounded-full ring-2 ring-inset ring-primary',
        modifiers.disabled && 'rounded-full text-[#4B5563] opacity-40',
        className,
      )}
    />
  );
}

const calendarComponents = {
  CaptionLabel: CalendarCaption,
  Chevron: CalendarChevron,
  Day: CalendarDayCell,
  DayButton: CalendarDayButton,
  NextMonthButton: CalendarNavButton,
  PreviousMonthButton: CalendarNavButton,
};

const calendarFormatters = {
  formatCaption: (month: Date) => monthTitle(month.getFullYear(), month.getMonth() + 1),
  formatWeekdayName: (date: Date) => WEEKDAY_LABELS[(date.getDay() + 6) % 7],
  formatDay: (date: Date) => String(date.getDate()),
};

const calendarLabels = {
  labelPrevious: () => 'Предыдущий месяц',
  labelNext: () => 'Следующий месяц',
  labelDayButton: (date: Date) => dayAccessibleName(dateToIso(date)),
};

export default function DateRangeCalendar({
  value,
  onChange,
  onClose,
  mode = 'range',
  minDate,
  maxDate,
}: DateRangeCalendarProps) {
  const wide = useWideScreen();
  const fit = useCalendarFit();
  const min = minDate ?? (mode === 'range' ? moscowToday() : undefined);
  const max = maxDate ?? (mode === 'range' ? DEPARTURE_DATE_MAX : undefined);
  const months = fit?.months ?? (mode === 'range' && wide ? 2 : 1);
  const compact = fit?.compact ?? false;
  const chrome = useMemo(() => calendarChrome(compact, months), [compact, months]);

  const startMonth = min ? monthStart(min) : undefined;
  // Два месяца: следующий за максимумом остаётся на экране, но дни в нём недоступны.
  const endMonth = max ? (months > 1 ? monthAfter(max) : monthStart(max)) : undefined;
  const defaultMonth = useMemo(() => {
    const preferred = value.from ?? value.to;
    if (preferred && inBounds(preferred, min, max)) return isoToDate(preferred);
    const today = moscowToday();
    if (inBounds(today, min, max)) return isoToDate(today);
    return (min && isoToDate(min)) || isoToDate(today);
  }, [value.from, value.to, min, max]);

  const disabled = useMemo(() => {
    const matchers: Matcher[] = [];
    const minDateValue = min ? isoToDate(min) : undefined;
    const maxDateValue = max ? isoToDate(max) : undefined;
    if (minDateValue) matchers.push({ before: minDateValue });
    if (maxDateValue) matchers.push({ after: maxDateValue });
    return matchers.length > 0 ? matchers : undefined;
  }, [min, max]);

  const classNames = useMemo(
    () => ({
      root: 'w-full text-dark',
      months: chrome.months,
      month: 'w-full',
      month_caption: 'flex justify-center',
      nav: chrome.nav,
      button_previous: chrome.navButton,
      button_next: chrome.navButton,
      month_grid: 'w-full border-collapse',
      weekdays: '',
      weekday: chrome.weekday,
      week: '',
      day: clsx(chrome.box, 'p-0 text-center align-middle'),
      day_button: chrome.dayButton,
      range_start: 'bg-sand',
      range_middle: 'bg-sand',
      range_end: 'bg-sand',
    }),
    [chrome],
  );

  function onKeyDown(event: ReactKeyboardEvent<HTMLDivElement>) {
    if (event.key !== 'Escape') return;
    event.preventDefault();
    event.stopPropagation();
    onClose();
  }

  function choose(iso: string) {
    if (!inBounds(iso, min, max)) return;
    if (mode === 'single') {
      onChange({ from: iso });
      onClose();
      return;
    }
    const start = value.from;
    const end = value.to;
    if (!start || end) {
      onChange({ from: iso });
      return;
    }
    if (iso < start) {
      onChange({ from: iso });
      return;
    }
    onChange({ from: start, to: iso });
    onClose();
  }

  const shared = {
    locale: ru,
    weekStartsOn: 1 as const,
    numberOfMonths: months,
    showOutsideDays: false,
    autoFocus: true,
    today: isoToDate(moscowToday()),
    startMonth,
    endMonth,
    defaultMonth,
    disabled,
    components: calendarComponents,
    formatters: calendarFormatters,
    labels: calendarLabels,
    classNames,
    onSelect: (_selected: unknown, triggerDate: Date) => {
      choose(dateToIso(triggerDate));
    },
  };

  return (
    <div onKeyDown={onKeyDown}>
      {mode === 'single' ? (
        <DayPicker mode="single" selected={value.from ? isoToDate(value.from) : undefined} {...shared} />
      ) : (
        <DayPicker
          mode="range"
          selected={{
            from: value.from ? isoToDate(value.from) : undefined,
            to: value.to ? isoToDate(value.to) : undefined,
          }}
          {...shared}
        />
      )}
      <button
        type="button"
        onClick={() => onChange({})}
        className={chrome.reset}
      >
        Сбросить
      </button>
    </div>
  );
}
