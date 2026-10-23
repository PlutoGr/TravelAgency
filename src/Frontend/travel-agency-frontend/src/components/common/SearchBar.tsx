import { useId, useMemo, useRef, useState, type FormEvent, type KeyboardEvent, type MouseEvent } from 'react';
import { Calendar, MapPin, Search, X } from 'lucide-react';
import clsx from 'clsx';
import type { Destination } from '@/types';
import { formatHomeDateLabel, suggestPlaces, type PlaceSuggestion } from '@/utils/catalogQuery';
import DateRangeCalendar, { type DateRange } from './DateRangeCalendar';
import SearchPopup from './SearchPopup';
import { useWideScreen } from './useWideScreen';

export interface SearchBarValues {
  destination: string;
  date: string;
  dateTo?: string;
}

interface SearchBarProps {
  onSearch?: (values: SearchBarValues) => void;
  className?: string;
  destinations?: Destination[];
}

const fieldShell =
  'flex min-w-0 flex-1 items-stretch border-b border-sand focus-within:bg-cream md:border-b-0 md:border-r';

export default function SearchBar({ onSearch, className, destinations = [] }: SearchBarProps) {
  const [destination, setDestination] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [suggestionsOpen, setSuggestionsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const [calendarOpen, setCalendarOpen] = useState(false);
  const wide = useWideScreen();
  const listId = useId();
  const calendarId = useId();
  const whenLabelId = useId();
  const whenValueId = useId();
  const destinationRef = useRef<HTMLInputElement>(null);
  const destinationCellRef = useRef<HTMLDivElement>(null);
  const dateButtonRef = useRef<HTMLButtonElement>(null);
  const dateCellRef = useRef<HTMLDivElement>(null);
  const skipSuggestionOpen = useRef(false);

  const suggestions = useMemo(() => suggestPlaces(destination, destinations), [destination, destinations]);
  const showNothing = destination.trim().length > 0 && suggestions.length === 0;
  const listOpen = suggestionsOpen && (suggestions.length > 0 || showNothing);
  const safeActive = activeIndex >= 0 && activeIndex < suggestions.length ? activeIndex : -1;
  const dateLabel = formatHomeDateLabel(dateFrom || undefined, dateTo || undefined);

  function submit(event: FormEvent) {
    event.preventDefault();
    onSearch?.({
      destination,
      date: dateFrom,
      dateTo: dateTo || undefined,
    });
  }

  function chooseSuggestion(item: PlaceSuggestion) {
    skipSuggestionOpen.current = true;
    setDestination(item.value);
    setSuggestionsOpen(false);
    setActiveIndex(-1);
    destinationRef.current?.focus();
  }

  function clearDestination(event: MouseEvent<HTMLButtonElement>) {
    event.preventDefault();
    event.stopPropagation();
    skipSuggestionOpen.current = true;
    setDestination('');
    setSuggestionsOpen(false);
    setActiveIndex(-1);
    destinationRef.current?.focus();
  }

  function clearDates(event: MouseEvent<HTMLButtonElement>) {
    event.preventDefault();
    event.stopPropagation();
    setDateFrom('');
    setDateTo('');
    setCalendarOpen(false);
    dateButtonRef.current?.focus();
  }

  function closeCalendar() {
    setCalendarOpen(false);
    dateButtonRef.current?.focus();
  }

  function onRangeChange(next: DateRange) {
    setDateFrom(next.from ?? '');
    setDateTo(next.to ?? '');
  }

  function onDestinationFocus() {
    setCalendarOpen(false);
    destinationRef.current?.scrollIntoView?.({ block: 'center', inline: 'nearest', behavior: 'instant' });
    if (skipSuggestionOpen.current) {
      skipSuggestionOpen.current = false;
      return;
    }
    setSuggestionsOpen(true);
    setActiveIndex(-1);
  }

  function onDestinationKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      if (suggestions.length === 0) return;
      setSuggestionsOpen(true);
      setActiveIndex((index) => {
        if (index < 0 || index >= suggestions.length) return event.key === 'ArrowUp' ? suggestions.length - 1 : 0;
        if (event.key === 'ArrowDown') return Math.min(index + 1, suggestions.length - 1);
        return Math.max(index - 1, 0);
      });
      return;
    }
    if (event.key === 'Escape' && listOpen) {
      event.preventDefault();
      event.stopPropagation();
      setSuggestionsOpen(false);
      setActiveIndex(-1);
      return;
    }
    if (event.key === 'Enter' && listOpen && safeActive >= 0) {
      event.preventDefault();
      chooseSuggestion(suggestions[safeActive]);
    }
  }

  return (
    <form
      onSubmit={submit}
      className={clsx(
        'flex w-full min-w-0 flex-col gap-0 overflow-hidden rounded-[20px] bg-white font-sans shadow-card md:flex-row md:items-stretch',
        className,
      )}
    >
      <div ref={destinationCellRef} className={fieldShell}>
        <div
          className="grid min-w-0 flex-1 grid-cols-[20px_minmax(0,1fr)] items-center gap-x-3 px-4 py-3"
          onMouseDown={(event) => {
            const target = event.target as HTMLElement;
            if (target.closest('input, button')) return;
            event.preventDefault();
            destinationRef.current?.focus();
          }}
        >
          <label htmlFor="search-dest" className="col-start-2 text-xs font-medium text-[#4B5563]">
            Куда?
          </label>
          <MapPin
            size={20}
            aria-hidden="true"
            className="pointer-events-none col-start-1 row-start-2 self-center text-terracotta"
          />
          <input
            ref={destinationRef}
            id="search-dest"
            role="combobox"
            type="text"
            autoComplete="off"
            autoCorrect="off"
            spellCheck={false}
            aria-autocomplete="list"
            aria-expanded={listOpen}
            aria-controls={listId}
            aria-activedescendant={listOpen && safeActive >= 0 ? `${listId}-option-${safeActive}` : undefined}
            placeholder="Город или страна"
            value={destination}
            onChange={(event) => {
              setDestination(event.target.value);
              setSuggestionsOpen(true);
              setActiveIndex(-1);
            }}
            onFocus={onDestinationFocus}
            onKeyDown={onDestinationKeyDown}
            className="col-start-2 row-start-2 min-h-11 w-full min-w-0 bg-transparent text-base font-sans text-dark outline-none placeholder:text-[#4B5563] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          />
        </div>
        {destination ? (
          <button
            type="button"
            aria-label="Очистить"
            onClick={clearDestination}
            className="my-auto mr-2 inline-flex size-11 shrink-0 items-center justify-center rounded-full text-dark hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <X size={18} aria-hidden="true" className="pointer-events-none" />
          </button>
        ) : null}
      </div>

      <div ref={dateCellRef} className={fieldShell}>
        <button
          ref={dateButtonRef}
          type="button"
          aria-haspopup="dialog"
          aria-expanded={calendarOpen}
          aria-controls={calendarId}
          aria-labelledby={`${whenLabelId} ${whenValueId}`}
          onClick={() => {
            setSuggestionsOpen(false);
            setCalendarOpen((open) => !open);
          }}
          className="grid min-h-11 min-w-0 flex-1 grid-cols-[20px_minmax(0,1fr)] items-center gap-x-3 px-4 py-3 text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        >
          <span id={whenLabelId} className="col-start-2 text-xs font-medium text-[#4B5563]">
            Когда?
          </span>
          <Calendar
            size={20}
            aria-hidden="true"
            className="pointer-events-none col-start-1 row-start-2 self-center text-olive"
          />
          <span
            id={whenValueId}
            className={clsx(
              'col-start-2 row-start-2 flex min-h-11 items-center text-base font-sans',
              dateLabel ? 'text-dark' : 'text-[#4B5563]',
            )}
          >
            {dateLabel || 'Выберите даты'}
          </span>
        </button>
        {dateFrom ? (
          <button
            type="button"
            aria-label="Очистить даты"
            onClick={clearDates}
            className="my-auto mr-2 inline-flex size-11 shrink-0 items-center justify-center rounded-full text-dark hover:bg-sand focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <X size={18} aria-hidden="true" className="pointer-events-none" />
          </button>
        ) : null}
      </div>

      <button
        type="submit"
        className="inline-flex min-h-11 w-full items-center justify-center gap-2 bg-primary px-8 py-3 font-heading text-base font-semibold text-white transition-colors hover:bg-primary-light active:bg-primary-dark focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white md:w-auto"
      >
        <Search size={20} aria-hidden="true" className="pointer-events-none text-white" />
        <span>Найти</span>
      </button>

      {listOpen ? (
        <SearchPopup
          id={`${listId}-popup`}
          wide={wide}
          anchorRef={destinationCellRef}
          keyboardInset
          onClose={() => {
            setSuggestionsOpen(false);
            setActiveIndex(-1);
          }}
        >
          <ul id={listId} role="listbox" aria-label="Подсказки" className="m-0 list-none p-0">
            {showNothing ? (
              <li className="flex min-h-10 items-center px-3 text-sm text-[#4B5563]">Ничего не найдено</li>
            ) : (
              suggestions.map((item, index) => (
                <li
                  key={item.id}
                  id={`${listId}-option-${index}`}
                  role="option"
                  aria-selected={index === safeActive}
                  aria-label={item.value}
                  tabIndex={-1}
                  onMouseDown={(event) => event.preventDefault()}
                  onMouseEnter={() => setActiveIndex(index)}
                  onClick={() => chooseSuggestion(item)}
                  className={clsx(
                    'flex min-h-10 cursor-pointer items-center gap-2 px-3 text-left text-sm text-dark focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
                    index === safeActive && 'bg-sand ring-2 ring-inset ring-primary',
                  )}
                >
                  <span>{item.value}</span>
                  {item.hint ? (
                    <span aria-hidden="true" className="text-[#4B5563]">
                      {item.hint}
                    </span>
                  ) : null}
                </li>
              ))
            )}
          </ul>
        </SearchPopup>
      ) : null}

      {calendarOpen ? (
        <SearchPopup
          id={calendarId}
          wide={wide}
          anchorRef={dateCellRef}
          dialog
          label="Выбор дат"
          contentSized
          allowTwoMonths
          onClose={closeCalendar}
        >
          <DateRangeCalendar
            value={{ from: dateFrom || undefined, to: dateTo || undefined }}
            onChange={onRangeChange}
            onClose={closeCalendar}
          />
        </SearchPopup>
      ) : null}
    </form>
  );
}
