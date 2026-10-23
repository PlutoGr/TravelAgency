import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import type { Destination } from '@/types';
import { catalogPath, resolveHomeSearch } from '@/utils/catalogQuery';
import DateRangeCalendar from './DateRangeCalendar';
import SearchBar, { type SearchBarValues } from './SearchBar';
import { calendarPanelWidth } from './searchPopupLayout';

function place(partial: Pick<Destination, 'id' | 'name' | 'country'> & { description?: string }): Destination {
  return {
    photo: '',
    tourCount: 1,
    description: '',
    ...partial,
  };
}

const places: Destination[] = [
  place({ id: 'dir-bali', name: 'Бали', country: 'Индонезия', description: 'секретное описание бали' }),
  place({ id: 'dir-rome', name: 'Рим', country: 'Италия' }),
  place({ id: 'dir-black', name: 'Чёрное море', country: 'Россия' }),
  place({ id: 'dir-spb', name: 'Санкт Петербург', country: 'Россия' }),
  place({ id: 'dir-italy', name: 'Италия', country: 'Италия' }),
];

function setViewport(width: number, height = 768) {
  Object.defineProperty(window, 'innerWidth', { configurable: true, value: width });
  Object.defineProperty(window, 'innerHeight', { configurable: true, value: height });
  Object.defineProperty(window, 'matchMedia', {
    configurable: true,
    writable: true,
    value: (query: string) => {
      const min = Number(/min-width:\s*(\d+)px/.exec(query)?.[1] ?? 0);
      return {
        matches: width >= min,
        media: query,
        onchange: null,
        addEventListener() {},
        removeEventListener() {},
        addListener() {},
        removeListener() {},
        dispatchEvent() {
          return false;
        },
      };
    },
  });
}

function freezeCalendar(isoUtc: string) {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(isoUtc));
}

function contrastOnWhite(hex: string): number {
  const value = Number.parseInt(hex.slice(1), 16);
  const channel = (part: number) => {
    const srgb = part / 255;
    return srgb <= 0.04045 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4;
  };
  const luminance =
    0.2126 * channel((value >> 16) & 255) +
    0.7152 * channel((value >> 8) & 255) +
    0.0722 * channel(value & 255);
  return (1 + 0.05) / (luminance + 0.05);
}

function optionLabels() {
  return screen.getAllByRole('option').map((option) => option.getAttribute('aria-label'));
}

function suggestionPanel(): HTMLElement {
  const panel = screen.getByRole('listbox').parentElement?.parentElement;
  if (!(panel instanceof HTMLElement)) throw new Error('нет панели подсказок');
  return panel;
}

function pinScroll(value: number) {
  Object.defineProperty(window, 'scrollY', { configurable: true, value });
}

function trackScrollTop(element: HTMLElement, start: number): () => number {
  let top = start;
  Object.defineProperty(element, 'scrollTop', {
    configurable: true,
    get: () => top,
    set: (value: number) => {
      top = value;
    },
  });
  return () => top;
}

describe('SearchBar', () => {
  beforeEach(() => {
    setViewport(1280);
  });

  afterEach(() => {
    vi.useRealTimers();
    Reflect.deleteProperty(window, 'matchMedia');
  });

  it('keeps labels, placeholders and icons in one field system', () => {
    render(<SearchBar destinations={places} />);

    const where = screen.getByText('Куда?');
    const when = screen.getByText('Когда?');
    const input = screen.getByRole('combobox', { name: 'Куда?' });
    const find = screen.getByRole('button', { name: 'Найти' });

    expect(contrastOnWhite('#4B5563')).toBeGreaterThanOrEqual(4.5);
    expect(where.className).toContain('text-xs');
    expect(where.className).toContain('text-[#4B5563]');
    expect(where.className).not.toContain('warm-gray');
    expect(when.className).toBe(where.className);
    expect(input.className).toContain('text-base');
    expect(input.className).toContain('font-sans');
    expect(input.className).toContain('placeholder:text-[#4B5563]');
    expect(input).toHaveAttribute('placeholder', 'Город или страна');
    expect(screen.getByText('Выберите даты').className).toContain('text-base');
    expect(screen.getByText('Выберите даты').className).toContain('text-[#4B5563]');
    expect(find.className).toContain('min-h-11');
    expect(find.className).toContain('w-full');
    expect(find.className).toContain('md:w-auto');
    expect(find.className).toContain('bg-primary');
    expect(find.className).toContain('text-white');
    expect(document.querySelector('input[type="date"]')).toBeNull();
    expect(document.body.textContent).not.toMatch(/dd\.mm\.yyyy/);

    const pin = document.querySelector('.lucide-map-pin');
    const calendar = document.querySelector('.lucide-calendar');
    expect(pin).toHaveAttribute('aria-hidden', 'true');
    expect(pin).toHaveAttribute('width', '20');
    expect(pin?.getAttribute('class')).toContain('row-start-2');
    expect(calendar).toHaveAttribute('aria-hidden', 'true');
    expect(calendar).toHaveAttribute('width', '20');
    expect(calendar?.getAttribute('class')).toContain('row-start-2');
  });

  it('filters suggestions by yo, case and outer spaces and caps the list at 8', async () => {
    const user = userEvent.setup();
    render(<SearchBar destinations={places} />);
    const input = screen.getByRole('combobox', { name: 'Куда?' });

    await user.click(input);
    expect(input).toHaveAttribute('aria-expanded', 'true');
    expect(input).toHaveAttribute('aria-controls');
    expect(optionLabels()).toEqual([
      'Бали',
      'Италия',
      'Рим',
      'Санкт Петербург',
      'Чёрное море',
      'Индонезия',
      'Россия',
    ]);
    expect(screen.getAllByRole('option', { name: 'Италия' })).toHaveLength(1);
    expect(screen.queryByText('секретное описание бали')).not.toBeInTheDocument();

    await user.type(input, '  БАЛИ  ');
    expect(screen.getByRole('option', { name: 'Бали' })).toBeInTheDocument();

    await user.clear(input);
    await user.type(input, 'черное');
    expect(screen.getByRole('option', { name: 'Чёрное море' })).toBeInTheDocument();

    await user.clear(input);
    await user.type(input, 'санкт  петербург');
    expect(screen.getByRole('option', { name: 'Санкт Петербург' })).toBeInTheDocument();

    await user.clear(input);
    await user.type(input, 'Атлантида');
    expect(screen.getByText('Ничего не найдено')).toBeInTheDocument();
    expect(screen.queryByRole('option')).not.toBeInTheDocument();
  });

  it('shows at most eight suggestion rows', async () => {
    const user = userEvent.setup();
    const many = 'АБВГДЕЖЗИ'.split('').map((letter, index) =>
      place({ id: `city-${index}`, name: `Город ${letter}`, country: 'Общая' }),
    );
    render(<SearchBar destinations={many} />);

    await user.click(screen.getByRole('combobox', { name: 'Куда?' }));

    expect(screen.getAllByRole('option')).toHaveLength(8);
    expect(screen.queryByRole('option', { name: 'Город И' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Общая' })).not.toBeInTheDocument();
  });

  it('selects a suggestion with Enter and does not open the catalog', async () => {
    const user = userEvent.setup();
    renderSearchRoute(places);

    const input = screen.getByRole('combobox', { name: 'Куда?' });
    await user.click(input);
    await user.keyboard('{ArrowDown}');
    expect(input).toHaveAttribute('aria-activedescendant', screen.getByRole('option', { name: 'Бали' }).id);
    await user.keyboard('{Enter}');

    expect(input).toHaveValue('Бали');
    expect(input).toHaveFocus();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(screen.getByTestId('url').textContent).toBe('/');
  });

  it('closes suggestions with Escape and keeps focus in the field', async () => {
    const user = userEvent.setup();
    render(<SearchBar destinations={places} />);
    const input = screen.getByRole('combobox', { name: 'Куда?' });

    await user.click(input);
    expect(screen.getByRole('listbox')).toBeInTheDocument();
    await user.keyboard('{Escape}');

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(input).toHaveFocus();
  });

  it('clears the place without leaving the page', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<SearchBar destinations={places} onSearch={onSearch} />);
    const input = screen.getByRole('combobox', { name: 'Куда?' });

    await user.type(input, 'Бали');
    const clear = screen.getByRole('button', { name: /^Очистить$/ });
    expect(clear.className).toContain('size-11');
    await user.click(clear);

    expect(input).toHaveValue('');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(onSearch).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /^Очистить$/ })).not.toBeInTheDocument();
  });

  it('does not scroll the place field into view and walks the tab order', async () => {
    const user = userEvent.setup();
    if (!HTMLElement.prototype.scrollIntoView) {
      HTMLElement.prototype.scrollIntoView = () => {};
    }
    const scroll = vi.spyOn(HTMLElement.prototype, 'scrollIntoView').mockImplementation(() => {});
    render(<SearchBar destinations={places} />);
    const input = screen.getByRole('combobox', { name: 'Куда?' });

    act(() => {
      input.focus();
    });
    expect(scroll).not.toHaveBeenCalled();

    await user.type(input, 'Рим');
    act(() => {
      input.focus();
    });
    await user.tab();
    expect(screen.getByRole('button', { name: /^Очистить$/ })).toHaveFocus();
    await user.tab();
    const when = screen.getByRole('button', { name: /Когда\?/ });
    expect(when).toHaveFocus();
    expect(when).toHaveAttribute('aria-haspopup', 'dialog');
    await user.tab();
    expect(screen.getByRole('button', { name: 'Найти' })).toHaveFocus();
    scroll.mockRestore();
  });

  it('formats a range, keeps a single start and blocks yesterday in Moscow', async () => {
    freezeCalendar('2026-11-11T22:00:00.000Z');
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<SearchBar destinations={places} onSearch={onSearch} />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    const dialog = screen.getByRole('dialog', { name: 'Выбор дат' });
    expect(dialog.className).not.toContain('inset-x-0');
    expect(screen.getByRole('heading', { name: 'ноябрь 2026' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'декабрь 2026' })).toBeInTheDocument();
    expect(screen.getAllByText('пн').length).toBeGreaterThan(0);
    expect(screen.getAllByText('вс').length).toBeGreaterThan(0);

    const yesterday = screen.getByRole('gridcell', { name: '11 ноября 2026' });
    const today = screen.getByRole('gridcell', { name: '12 ноября 2026' });
    expect(yesterday).toBeDisabled();
    expect(today).toBeEnabled();
    expect(today).toHaveFocus();
    expect(today.className).toContain('size-10');
    expect(dialog.innerHTML).not.toContain('overflow-y-auto');
    expect(document.body.style.overflow).toBe('');
    expect(document.documentElement.style.overflow).toBe('');

    await user.keyboard('{ArrowLeft}');
    expect(today).toHaveFocus();
    await user.keyboard('{Home}');
    expect(today).toHaveFocus();
    await user.keyboard('{Enter}');
    expect(onSearch).not.toHaveBeenCalled();
    expect(screen.getByRole('dialog', { name: 'Выбор дат' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Когда\?/ })).toHaveTextContent('12 ноя');

    await user.keyboard('{ArrowDown}');
    expect(screen.getByRole('gridcell', { name: '19 ноября 2026' })).toHaveFocus();
    await user.keyboard('{End}');
    expect(screen.getByRole('gridcell', { name: '22 ноября 2026' })).toHaveFocus();

    await user.click(screen.getByRole('gridcell', { name: '20 ноября 2026' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    const when = screen.getByRole('button', { name: /Когда\?/ });
    expect(when).toHaveTextContent('с 12 по 20 ноя');
    expect(when.textContent).not.toMatch(/[-\u2013\u2014]/);
    expect(when).toHaveFocus();
  });

  it('shows one month in a bottom sheet below 768px and mutes neighbouring days', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    setViewport(390);
    const user = userEvent.setup();
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    const dialog = screen.getByRole('dialog', { name: 'Выбор дат' });
    expect(dialog.className).toContain('fixed');
    expect(dialog.className).toContain('inset-x-0');
    expect(dialog.className).toContain('bottom-0');
    expect(dialog.className).toContain('w-full');
    expect(dialog.innerHTML).toContain('env(safe-area-inset-bottom)');
    expect(dialog.innerHTML).not.toContain('overflow-y-auto');
    expect(document.body.style.overflow).toBe('');
    expect(document.documentElement.style.overflow).toBe('');
    expect(screen.getByRole('heading', { name: 'октябрь 2026' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'ноябрь 2026' })).not.toBeInTheDocument();

    for (const cell of screen.getAllByRole('gridcell')) {
      expect(cell.getAttribute('aria-label') ?? '').not.toMatch(/сентября|ноября/);
    }

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Когда\?/ })).toHaveFocus();
  });

  it('formats cross-month and cross-year ranges and resets the field', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<SearchBar onSearch={onSearch} />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    await user.click(screen.getByRole('gridcell', { name: '28 октября 2026' }));
    await user.click(screen.getByRole('gridcell', { name: '3 ноября 2026' }));
    expect(screen.getByRole('button', { name: /Когда\?/ })).toHaveTextContent('с 28 окт по 3 ноя');

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    await user.click(screen.getByRole('button', { name: 'Сбросить' }));
    expect(screen.getByText('Выберите даты')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Найти' }));
    expect(onSearch).toHaveBeenCalledWith({ destination: '', date: '', dateTo: undefined });

    onSearch.mockClear();
    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    await user.click(screen.getByRole('button', { name: 'Следующий месяц' }));
    await user.click(screen.getByRole('button', { name: 'Следующий месяц' }));
    await user.click(screen.getByRole('gridcell', { name: '28 декабря 2026' }));
    await user.click(screen.getByRole('gridcell', { name: '3 января 2027' }));
    expect(screen.getByRole('button', { name: /Когда\?/ })).toHaveTextContent('с 28 дек 2026 по 3 янв 2027');

    await user.click(screen.getByRole('button', { name: 'Найти' }));
    const values = onSearch.mock.calls.at(-1)?.[0] as SearchBarValues;
    expect(values).toEqual({ destination: '', date: '2026-12-28', dateTo: '2027-01-03' });
  });

  it('keeps the start when the calendar closes before the end and submits dates only', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<SearchBar onSearch={onSearch} />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    await user.click(screen.getByRole('gridcell', { name: '1 ноября 2026' }));
    await user.keyboard('{Escape}');
    expect(screen.getByRole('button', { name: /Когда\?/ })).toHaveTextContent('1 ноя');

    await user.click(screen.getByRole('button', { name: 'Найти' }));
    expect(onSearch).toHaveBeenCalledWith({ destination: '', date: '2026-11-01', dateTo: undefined });
  });

  it('closes the calendar when the click lands outside the field', async () => {
    const user = userEvent.setup();
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    expect(screen.getByRole('dialog', { name: 'Выбор дат' })).toBeInTheDocument();
    await user.click(document.body);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('anchors the calendar to the field instead of the corner', async () => {
    const user = userEvent.setup();
    const anchor = {
      x: 420,
      y: 180,
      width: 200,
      height: 72,
      top: 180,
      left: 420,
      right: 620,
      bottom: 252,
      toJSON() {
        return {};
      },
    } as DOMRect;
    const spy = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(function rect(this: HTMLElement) {
      if (this.getAttribute('role') === 'dialog') {
        return { ...anchor, width: 640, height: 420, right: anchor.left + 640, bottom: anchor.top + 420 } as DOMRect;
      }
      return anchor;
    });
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    const dialog = screen.getByRole('dialog', { name: 'Выбор дат' });
    expect(dialog).toHaveAttribute('data-anchored', 'true');
    expect(dialog.style.visibility).toBe('visible');
    expect(dialog.style.left).toBe('420px');
    expect(dialog.style.top).toBe('260px');
    expect(dialog.style.width).toBe(`${calendarPanelWidth(2, false)}px`);
    expect(dialog.style.minWidth).not.toBe('700px');
    expect(dialog.innerHTML).not.toContain('overflow-y-auto');
    spy.mockRestore();
  });

  it('disables days after 31 December 2100', () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    render(
      <DateRangeCalendar value={{ from: '2100-12-31' }} onChange={() => {}} onClose={() => {}} />,
    );

    expect(screen.getByRole('gridcell', { name: '31 декабря 2100' })).toBeEnabled();
    expect(screen.getByRole('gridcell', { name: '1 января 2101' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Следующий месяц' })).toBeDisabled();
  });

  it('opens suggestions without scrolling the document or hiding the scrollbar', async () => {
    const user = userEvent.setup();
    pinScroll(240);
    const scrollBy = vi.spyOn(window, 'scrollBy').mockImplementation(() => {});
    const scrollTo = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    const scrollIntoView = vi.spyOn(HTMLElement.prototype, 'scrollIntoView').mockImplementation(() => {});
    render(<SearchBar destinations={places} />);

    await user.click(screen.getByRole('combobox', { name: 'Куда?' }));

    expect(window.scrollY).toBe(240);
    expect(scrollBy).not.toHaveBeenCalled();
    expect(scrollTo).not.toHaveBeenCalled();
    expect(scrollIntoView).not.toHaveBeenCalled();
    expect(document.body.style.overflow).toBe('');
    expect(document.documentElement.style.overflow).toBe('');
    expect(document.body.style.paddingRight).toBe('');
    expect(document.documentElement.style.paddingRight).toBe('');
    expect(document.documentElement.style.scrollbarGutter).toBe('');
    expect(suggestionPanel()).toHaveAttribute('data-place', 'below');
    expect(document.querySelector('[data-popup-scroll]')?.className).toContain('overflow-y-auto');
    expect(document.querySelector('[data-popup-scroll]')?.className).toContain('overscroll-contain');

    scrollBy.mockRestore();
    scrollTo.mockRestore();
    scrollIntoView.mockRestore();
  });

  it('opens the calendar without scrolling the document or hiding the scrollbar', async () => {
    const user = userEvent.setup();
    pinScroll(180);
    const scrollBy = vi.spyOn(window, 'scrollBy').mockImplementation(() => {});
    const scrollIntoView = vi.spyOn(HTMLElement.prototype, 'scrollIntoView').mockImplementation(() => {});
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));

    expect(screen.getByRole('dialog', { name: 'Выбор дат' })).toBeInTheDocument();
    expect(window.scrollY).toBe(180);
    expect(scrollBy).not.toHaveBeenCalled();
    expect(scrollIntoView).not.toHaveBeenCalled();
    expect(document.body.style.overflow).toBe('');
    expect(document.documentElement.style.overflow).toBe('');
    expect(document.body.style.paddingRight).toBe('');

    scrollBy.mockRestore();
    scrollIntoView.mockRestore();
  });

  it('closes suggestions on window scroll and the calendar on document scroll', async () => {
    const user = userEvent.setup();
    render(<SearchBar destinations={places} />);
    const input = screen.getByRole('combobox', { name: 'Куда?' });

    await user.click(input);
    expect(screen.getByRole('listbox')).toBeInTheDocument();
    act(() => {
      window.dispatchEvent(new Event('scroll'));
    });
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();

    await user.click(input);
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'Выбор дат' })).toBeInTheDocument();
    act(() => {
      document.dispatchEvent(new Event('scroll'));
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('keeps the wheel over the city list inside that list', async () => {
    const user = userEvent.setup();
    pinScroll(80);
    render(<SearchBar destinations={places} />);

    await user.click(screen.getByRole('combobox', { name: 'Куда?' }));
    const scrollEl = document.querySelector('[data-popup-scroll]');
    if (!(scrollEl instanceof HTMLElement)) throw new Error('нет внутреннего списка');
    const readTop = trackScrollTop(scrollEl, 0);
    Object.defineProperty(scrollEl, 'scrollHeight', { configurable: true, value: 100 });
    Object.defineProperty(scrollEl, 'clientHeight', { configurable: true, value: 160 });

    const idle = new WheelEvent('wheel', { deltaY: 40, bubbles: true, cancelable: true });
    screen.getAllByRole('option')[0].dispatchEvent(idle);
    expect(idle.defaultPrevented).toBe(true);
    expect(readTop()).toBe(0);
    expect(window.scrollY).toBe(80);

    Object.defineProperty(scrollEl, 'scrollHeight', { configurable: true, value: 480 });
    Object.defineProperty(scrollEl, 'clientHeight', { configurable: true, value: 160 });
    const overList = new WheelEvent('wheel', { deltaY: 36, bubbles: true, cancelable: true });
    screen.getAllByRole('option')[0].dispatchEvent(overList);
    expect(overList.defaultPrevented).toBe(true);
    expect(readTop()).toBe(36);
    expect(window.scrollY).toBe(80);
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    const atEnd = new WheelEvent('wheel', { deltaY: 400, bubbles: true, cancelable: true });
    screen.getAllByRole('option')[0].dispatchEvent(atEnd);
    expect(atEnd.defaultPrevented).toBe(true);
    expect(readTop()).toBe(320);
    expect(window.scrollY).toBe(80);

    const outside = new WheelEvent('wheel', { deltaY: 36, bubbles: true, cancelable: true });
    document.body.dispatchEvent(outside);
    expect(outside.defaultPrevented).toBe(false);
    expect(readTop()).toBe(320);
    expect(window.scrollY).toBe(80);
  });

  it('opens the city list above the field when there is no room below', async () => {
    setViewport(1280, 700);
    const rect = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => ({
      x: 40,
      y: 640,
      top: 640,
      left: 40,
      right: 320,
      bottom: 690,
      width: 280,
      height: 50,
      toJSON() {
        return {};
      },
    }));
    const user = userEvent.setup();
    render(<SearchBar destinations={places} />);

    await user.click(screen.getByRole('combobox', { name: 'Куда?' }));
    const panel = suggestionPanel();
    expect(panel).toHaveAttribute('data-place', 'above');
    expect(panel.style.top).toBe('');
    expect(panel.style.bottom).toBe('68px');
    const scrollEl = panel.querySelector('[data-popup-scroll]');
    expect(scrollEl).toHaveStyle({ maxHeight: '632px' });
    rect.mockRestore();
  });

  it('opens the calendar above the field when there is no room below', async () => {
    setViewport(1280, 800);
    const rect = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => ({
      x: 40,
      y: 620,
      top: 620,
      left: 40,
      right: 320,
      bottom: 680,
      width: 280,
      height: 60,
      toJSON() {
        return {};
      },
    }));
    const user = userEvent.setup();
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    const dialog = screen.getByRole('dialog', { name: 'Выбор дат' });
    expect(dialog).toHaveAttribute('data-place', 'above');
    expect(dialog.style.bottom).not.toBe('');
    expect(dialog.style.top).toBe('');
    expect(dialog.innerHTML).not.toContain('overflow-y-auto');
    expect(screen.getAllByRole('gridcell')[0].className).toContain('size-10');
    rect.mockRestore();
  });

  it('uses compact day cells when neither side fits a regular month', async () => {
    setViewport(1280, 760);
    const rect = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => ({
      x: 40,
      y: 360,
      top: 360,
      left: 40,
      right: 320,
      bottom: 420,
      width: 280,
      height: 60,
      toJSON() {
        return {};
      },
    }));
    const user = userEvent.setup();
    render(<SearchBar />);

    await user.click(screen.getByRole('button', { name: /Когда\?/ }));
    const dialog = screen.getByRole('dialog', { name: 'Выбор дат' });
    expect(dialog).toHaveAttribute('data-place', 'above');
    expect(dialog.innerHTML).not.toContain('overflow-y-auto');
    expect(screen.getAllByRole('gridcell')[0].className).toContain('size-9');
    rect.mockRestore();
  });
});

function UrlProbe() {
  const location = useLocation();
  return <div data-testid="url">{`${location.pathname}${location.search}`}</div>;
}

function SearchRoute({ destinations }: { destinations: Destination[] }) {
  const navigate = useNavigate();
  return (
    <SearchBar
      destinations={destinations}
      onSearch={(values) => navigate(catalogPath(resolveHomeSearch(values, destinations)))}
    />
  );
}

function renderSearchRoute(destinations: Destination[]) {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <UrlProbe />
      <Routes>
        <Route path="*" element={<SearchRoute destinations={destinations} />} />
      </Routes>
    </MemoryRouter>,
  );
}
