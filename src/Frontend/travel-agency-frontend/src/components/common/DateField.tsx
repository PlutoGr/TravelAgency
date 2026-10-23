import { useId, useRef, useState, type ButtonHTMLAttributes } from 'react';
import clsx from 'clsx';
import { dayAccessibleName, parseCalendarIso } from '@/utils/homeCalendar';
import DateRangeCalendar from './DateRangeCalendar';
import SearchPopup from './SearchPopup';
import { useWideScreen } from './useWideScreen';

interface DateFieldProps {
  id: string;
  value?: string;
  onChange: (value: string | undefined) => void;
  minDate?: string;
  maxDate?: string;
  placeholder?: string;
  invalid?: boolean;
  describedBy?: string;
  className?: string;
  /** Имя кнопки, если снаружи нет подписи через label. */
  ariaLabel?: string;
}

const controlClass =
  'flex min-h-11 w-full items-center rounded-[12px] border border-sand bg-white px-4 py-2.5 text-left text-sm outline-none focus-visible:border-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary focus-visible:ring-2 focus-visible:ring-primary/10';

function shownValue(value: string | undefined, placeholder: string): string {
  if (!value) return placeholder;
  if (!parseCalendarIso(value)) return value;
  return dayAccessibleName(value);
}

export default function DateField({
  id,
  value,
  onChange,
  minDate,
  maxDate,
  placeholder = 'Выберите дату',
  invalid = false,
  describedBy,
  className,
  ariaLabel,
}: DateFieldProps) {
  const wide = useWideScreen();
  const popupId = useId();
  const anchorRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const [open, setOpen] = useState(false);
  const text = shownValue(value, placeholder);
  const bounds = {
    ...(minDate ? { min: minDate } : {}),
    ...(maxDate ? { max: maxDate } : {}),
  } as ButtonHTMLAttributes<HTMLButtonElement>;

  function close() {
    setOpen(false);
    buttonRef.current?.focus();
  }

  return (
    <div ref={anchorRef}>
      <button
        ref={buttonRef}
        id={id}
        type="button"
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? popupId : undefined}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        aria-label={ariaLabel ? (value ? `${ariaLabel}, ${text}` : ariaLabel) : undefined}
        {...bounds}
        onClick={() => setOpen((current) => !current)}
        className={clsx(controlClass, value ? 'text-dark' : 'text-[#4B5563]', className)}
      >
        {text}
      </button>
      {open ? (
        <SearchPopup
          id={popupId}
          wide={wide}
          anchorRef={anchorRef}
          dialog
          label="Выбор даты"
          contentSized
          onClose={close}
        >
          <DateRangeCalendar
            mode="single"
            value={{ from: value || undefined }}
            minDate={minDate}
            maxDate={maxDate}
            onChange={(next) => onChange(next.from)}
            onClose={close}
          />
        </SearchPopup>
      ) : null}
    </div>
  );
}
