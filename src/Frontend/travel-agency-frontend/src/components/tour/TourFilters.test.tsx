import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import TourFilters from './TourFilters';
import { DEPARTURE_DATE_MAX, DEPARTURE_DATE_MIN } from '@/utils/catalogQuery';
import type { TourFilters as TourFiltersType } from '@/types';

vi.mock('@/api/catalog', () => ({
  getDestinations: vi.fn().mockResolvedValue([]),
}));

function freezeCalendar(isoUtc: string) {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(isoUtc));
}

function setViewport(width: number) {
  Object.defineProperty(window, 'innerWidth', { configurable: true, value: width });
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

function Harness({ initial = {} }: { initial?: TourFiltersType }) {
  const [filters, setFilters] = useState(initial);
  return (
    <QueryClientProvider client={new QueryClient()}>
      <output data-testid="dates">{`${filters.dateFrom ?? ''}|${filters.dateTo ?? ''}`}</output>
      <TourFilters filters={filters} onChange={setFilters} onReset={() => setFilters({})} />
    </QueryClientProvider>
  );
}

describe('TourFilters dates', () => {
  beforeEach(() => {
    setViewport(1280);
  });

  afterEach(() => {
    vi.useRealTimers();
    Reflect.deleteProperty(window, 'matchMedia');
  });

  it('writes from and to through the shared calendar and keeps the bounds', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    render(<Harness />);

    expect(document.querySelector('input[type="date"]')).toBeNull();
    const from = screen.getByLabelText('С');
    const to = screen.getByLabelText('По');
    expect(from).toHaveAttribute('min', DEPARTURE_DATE_MIN);
    expect(from).toHaveAttribute('max', DEPARTURE_DATE_MAX);
    expect(to).toHaveAttribute('min', DEPARTURE_DATE_MIN);
    expect(to).toHaveAttribute('max', DEPARTURE_DATE_MAX);
    expect(from).toHaveTextContent('Выберите дату');
    expect(from.textContent).not.toMatch(/dd\.mm\.yyyy/i);

    await user.click(from);
    const dialog = screen.getByRole('dialog', { name: 'Выбор даты' });
    expect(dialog.className).not.toContain('inset-x-0');
    await user.click(screen.getByRole('button', { name: 'Следующий месяц' }));
    await user.click(screen.getByRole('gridcell', { name: '12 ноября 2026' }));
    expect(screen.getByTestId('dates')).toHaveTextContent('2026-11-12|');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(screen.getByLabelText('По'));
    await user.click(screen.getByRole('button', { name: 'Следующий месяц' }));
    await user.click(screen.getByRole('gridcell', { name: '20 ноября 2026' }));
    expect(screen.getByTestId('dates')).toHaveTextContent('2026-11-12|2026-11-20');
  });

  it('marks an inverted range and clears it from the calendar', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    render(<Harness initial={{ dateFrom: '2026-12-01', dateTo: '2026-11-01' }} />);

    expect(screen.getByLabelText('По')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByText('Дата «по» должна быть не раньше даты «с»')).toBeInTheDocument();

    await user.click(screen.getByLabelText('С'));
    await user.click(screen.getByRole('button', { name: 'Сбросить' }));
    expect(screen.getByTestId('dates')).toHaveTextContent('|2026-11-01');
  });
});
