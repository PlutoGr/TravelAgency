import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import ManagerBookingsPage from './ManagerBookingsPage';

vi.mock('@/api/bookings', () => ({
  getAllBookings: vi.fn().mockResolvedValue([]),
  bookingTourTitle: () => '',
}));

function freezeCalendar(isoUtc: string) {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(isoUtc));
}

describe('ManagerBookingsPage dates', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('filters by a calendar day instead of a native date input', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <ManagerBookingsPage />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: 'Бронирования' })).toBeInTheDocument();
    expect(document.querySelector('input[type="date"]')).toBeNull();

    await user.click(screen.getByLabelText('Создана с'));
    expect(screen.getByRole('dialog', { name: 'Выбор даты' })).toBeInTheDocument();
    await user.click(screen.getByRole('gridcell', { name: '1 октября 2026' }));
    expect(screen.getByLabelText('Создана с')).toHaveTextContent('1 октября 2026');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
