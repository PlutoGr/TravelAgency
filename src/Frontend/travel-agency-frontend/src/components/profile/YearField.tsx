import { useId, useRef, useState } from 'react';
import clsx from 'clsx';
import SearchPopup from '@/components/common/SearchPopup';
import { useWideScreen } from '@/components/common/useWideScreen';
import { tourismYears } from '@/domain/managerCardForm';

type YearFieldProps = {
  id: string;
  value: number | null;
  onChange: (value: number | null) => void;
  invalid?: boolean;
  describedBy?: string;
};

const controlClass =
  'flex min-h-11 w-full items-center justify-between rounded-[12px] border border-sand bg-white px-4 py-2.5 text-left text-sm outline-none focus-visible:border-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary focus-visible:ring-2 focus-visible:ring-primary/10';

export default function YearField({ id, value, onChange, invalid = false, describedBy }: YearFieldProps) {
  const wide = useWideScreen();
  const popupId = useId();
  const anchorRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const [open, setOpen] = useState(false);
  const years = tourismYears();
  const text = value == null ? 'Выберите год' : String(value);

  function close() {
    setOpen(false);
    buttonRef.current?.focus();
  }

  function pick(next: number | null) {
    onChange(next);
    close();
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
        aria-label={value == null ? 'Работаю в туризме с' : `Работаю в туризме с, ${value}`}
        onClick={() => setOpen((current) => !current)}
        className={clsx(controlClass, value == null ? 'text-[#4B5563]' : 'text-dark')}
      >
        <span>{text}</span>
        <span aria-hidden="true" className="text-[#7a7268]">
          ▾
        </span>
      </button>
      {open ? (
        <SearchPopup id={popupId} wide={wide} anchorRef={anchorRef} dialog label="Выбор года" minWidth={220} onClose={close}>
          <ul className="py-1" aria-label="Годы">
            <li>
              <button
                type="button"
                className="min-h-11 w-full rounded-[10px] px-3 text-left text-sm text-[#5c564e] hover:bg-sand"
                onClick={() => pick(null)}
              >
                Не указан
              </button>
            </li>
            {years.map((year) => (
              <li key={year}>
                <button
                  type="button"
                  aria-current={year === value ? 'true' : undefined}
                  className={clsx(
                    'min-h-11 w-full rounded-[10px] px-3 text-left text-sm hover:bg-sand',
                    year === value ? 'bg-[#f3eee6] font-semibold text-[#1c1915]' : 'text-[#1c1915]',
                  )}
                  onClick={() => pick(year)}
                >
                  {year}
                </button>
              </li>
            ))}
          </ul>
        </SearchPopup>
      ) : null}
    </div>
  );
}
