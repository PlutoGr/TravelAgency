import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import DatesEditor from './DatesEditor';

function freezeCalendar(isoUtc: string) {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(isoUtc));
}

describe('DatesEditor', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('adds a stay as calendar days without a native date input', async () => {
    freezeCalendar('2026-10-23T09:00:00.000Z');
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DatesEditor dates={[]} onChange={onChange} />);

    expect(document.querySelector('input[type="date"]')).toBeNull();
    await user.click(screen.getByRole('button', { name: 'Начало заезда' }));
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Начало заезда' }));
    await user.click(screen.getByRole('gridcell', { name: '28 октября 2026' }));
    await user.click(screen.getByRole('button', { name: 'Конец заезда' }));
    await user.click(screen.getByRole('button', { name: 'Следующий месяц' }));
    await user.click(screen.getByRole('gridcell', { name: '3 ноября 2026' }));
    await user.click(screen.getByRole('button', { name: '+' }));

    expect(onChange).toHaveBeenCalledWith([{ start: '2026-10-28', end: '2026-11-03' }]);
  });
});
