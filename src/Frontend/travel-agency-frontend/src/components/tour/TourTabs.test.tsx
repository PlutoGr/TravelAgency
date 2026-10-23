import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TourTabs from './TourTabs';
import { sampleTour } from '@/test/sampleTour';

describe('TourTabs reviews', () => {
  it('marks the empty reviews tab as not ready', async () => {
    const user = userEvent.setup();
    render(<TourTabs tour={sampleTour()} />);

    await user.click(screen.getByRole('button', { name: 'Отзывы' }));

    expect(screen.getByText('Появится позже')).toBeInTheDocument();
    expect(screen.queryByText('Пока нет отзывов для этого тура')).not.toBeInTheDocument();
  });
});
