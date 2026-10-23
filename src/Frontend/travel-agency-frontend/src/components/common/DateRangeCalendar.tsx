import { useEffect, useRef, useState, type KeyboardEvent, type RefObject } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import clsx from 'clsx';
import { DEPARTURE_DATE_MAX } from '@/utils/catalogQuery';
import {
  addCalendarDays,
  dayAccessibleName,
  isDayUnavailable,
  monthGrid,
  monthKey,
  monthTitle,
  moscowToday,
  parseCalendarIso,
  shiftCalendarMonth,
  WEEKDAY_LABELS,
  weekStartMonday,
} from '@/utils/homeCalendar';
import { useWideScreen } from './useWideScreen';

export type DateRange = {
  from?: string;
  to?: string;
};

interface DateRangeCalendarProps {
  value: DateRange;
  onChange: (value: DateRange) => void;
  onClose: () => void;
  /** Включительно. По умолчанию сегодня по календарю Europe/Moscow. */
  minDate?: string;
  /** Включительно. По умолчанию 2100-12-31. */
  maxDate?: string;
}

export default function DateRangeCalendar({
  value,
  onChange,
  onClose,
  minDate,
  maxDate = DEPARTURE_DATE_MAX,
}: DateRangeCalendarProps) {
  const wide = useWideScreen();
  const min = minDate ?? moscowToday();
  const today = moscowToday();
  const initial =
    value.from && !isDayUnavailable(value.from, min, maxDate) ? value.from : min;
  const initialParts = parseCalendarIso(initial) ?? parseCalendarIso(min) ?? { year: 2026, month: 1, day: 1 };
  const [focusedIso, setFocusedIso] = useState(initial);
  const [visible, setVisible] = useState({ year: initialParts.year, month: initialParts.month });
  const focusedRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    focusedRef.current?.focus();
  }, [focusedIso, visible.year, visible.month, wide]);

  const months = [visible];
  if (wide) months.push(shiftCalendarMonth(visible.year, visible.month, 1));

  const minParts = parseCalendarIso(min);
  const maxParts = parseCalendarIso(maxDate);
  const prevDisabled = minParts
    ? monthKey(visible.year, visible.month) <= monthKey(minParts.year, minParts.month)
    : false;
  const last = months[months.length - 1];
  const nextDisabled = maxParts ? monthKey(last.year, last.month) >= monthKey(maxParts.year, maxParts.month) : false;

  function shiftVisible(delta: number) {
    const next = shiftCalendarMonth(visible.year, visible.month, delta);
    setVisible(next);
    const left = monthKey(next.year, next.month);
    const span = wide ? 1 : 0;
    const parts = parseCalendarIso(focusedIso);
    const target = parts ? monthKey(parts.year, parts.month) : left;
    if (target >= left && target <= left + span) return;
    const candidate = monthGrid(next.year, next.month)
      .flat()
      .find((iso) => iso != null && !isDayUnavailable(iso, min, maxDate));
    if (candidate) setFocusedIso(candidate);
  }

  function moveFocus(nextIso: string) {
    if (isDayUnavailable(nextIso, min, maxDate)) return;
    const parts = parseCalendarIso(nextIso);
    if (!parts) return;
    setFocusedIso(nextIso);
    setVisible((current) => {
      const left = monthKey(current.year, current.month);
      const span = wide ? 1 : 0;
      const target = monthKey(parts.year, parts.month);
      if (target >= left && target <= left + span) return current;
      return { year: parts.year, month: parts.month };
    });
  }

  function choose(iso: string) {
    if (isDayUnavailable(iso, min, maxDate)) return;
    setFocusedIso(iso);
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

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      onClose();
      return;
    }
    const target = event.target as HTMLElement;
    const onDay = Boolean(target.dataset.calendarDay);
    if (!onDay) return;

    if (event.key === 'Enter') {
      event.preventDefault();
      choose(target.dataset.calendarDay ?? focusedIso);
      return;
    }

    const step: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
    if (event.key in step) {
      event.preventDefault();
      moveFocus(addCalendarDays(focusedIso, step[event.key]));
      return;
    }
    if (event.key === 'Home') {
      event.preventDefault();
      moveFocus(weekStartMonday(focusedIso));
      return;
    }
    if (event.key === 'End') {
      event.preventDefault();
      moveFocus(addCalendarDays(weekStartMonday(focusedIso), 6));
    }
  }

  return (
    <div onKeyDown={onKeyDown}>
      <div className="mb-2 flex items-center justify-between gap-2">
        <button
          type="button"
          aria-label="Предыдущий месяц"
          disabled={prevDisabled}
          onClick={() => shiftVisible(-1)}
          className="inline-flex size-11 items-center justify-center rounded-full text-dark hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:opacity-40"
        >
          <ChevronLeft size={20} aria-hidden="true" className="pointer-events-none" />
        </button>
        <button
          type="button"
          aria-label="Следующий месяц"
          disabled={nextDisabled}
          onClick={() => shiftVisible(1)}
          className="inline-flex size-11 items-center justify-center rounded-full text-dark hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:opacity-40"
        >
          <ChevronRight size={20} aria-hidden="true" className="pointer-events-none" />
        </button>
      </div>

      <div className={clsx('grid gap-6', wide ? 'grid-cols-2' : 'grid-cols-1')}>
        {months.map((month) => (
          <MonthGrid
            key={monthKey(month.year, month.month)}
            year={month.year}
            month={month.month}
            value={value}
            today={today}
            min={min}
            max={maxDate}
            focusedIso={focusedIso}
            focusedRef={focusedRef}
            onChoose={choose}
          />
        ))}
      </div>

      <button
        type="button"
        onClick={() => onChange({})}
        className="mt-3 flex min-h-11 w-full items-center justify-center rounded-[12px] text-base font-medium text-primary hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        Сбросить
      </button>
    </div>
  );
}

function MonthGrid({
  year,
  month,
  value,
  today,
  min,
  max,
  focusedIso,
  focusedRef,
  onChoose,
}: {
  year: number;
  month: number;
  value: DateRange;
  today: string;
  min: string;
  max: string;
  focusedIso: string;
  focusedRef: RefObject<HTMLButtonElement | null>;
  onChoose: (iso: string) => void;
}) {
  const titleId = `month-${year}-${month}`;
  const weeks = monthGrid(year, month);

  return (
    <div>
      <h2 id={titleId} className="mb-2 text-center text-base font-semibold text-dark">
        {monthTitle(year, month)}
      </h2>
      <div className="mb-1 grid grid-cols-7 text-center text-xs font-medium text-[#4B5563]">
        {WEEKDAY_LABELS.map((label) => (
          <span key={label}>{label}</span>
        ))}
      </div>
      <div role="grid" aria-labelledby={titleId}>
        {weeks.map((week, weekIndex) => (
          <div role="row" key={weekIndex} className="grid grid-cols-7">
            {week.map((iso, dayIndex) => {
              if (!iso) {
                return <span role="gridcell" key={`empty-${weekIndex}-${dayIndex}`} className="size-11" />;
              }
              const disabled = isDayUnavailable(iso, min, max);
              const selected = iso === value.from || (Boolean(value.to) && iso === value.to);
              const between = Boolean(
                value.from && value.to && value.from !== value.to && iso > value.from && iso < value.to,
              );
              const parts = parseCalendarIso(iso);
              return (
                <button
                  key={iso}
                  type="button"
                  role="gridcell"
                  data-calendar-day={iso}
                  aria-label={dayAccessibleName(iso)}
                  aria-selected={selected || undefined}
                  disabled={disabled}
                  tabIndex={iso === focusedIso ? 0 : -1}
                  ref={iso === focusedIso ? focusedRef : undefined}
                  onClick={() => onChoose(iso)}
                  className={clsx(
                    'inline-flex size-11 items-center justify-center rounded-full text-base focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
                    selected && 'bg-primary text-white',
                    !selected && between && 'bg-sand text-dark',
                    !selected && !between && 'text-dark hover:bg-sand',
                    !selected && iso === today && 'ring-2 ring-inset ring-primary',
                    disabled && 'text-[#4B5563] opacity-40 hover:bg-transparent',
                  )}
                >
                  {parts?.day}
                </button>
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
}
