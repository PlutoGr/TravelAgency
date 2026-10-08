import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import BookingForm from './BookingForm';
import { createBooking } from '@/api/bookings';
import { sampleTour } from '@/test/sampleTour';

vi.mock('@/api/bookings', () => ({
  createBooking: vi.fn().mockResolvedValue({ id: 'b1' }),
}));

vi.mock('@/api/catalog', () => ({
  getTours: vi.fn(),
  getTourById: vi.fn(),
}));

describe('BookingForm', () => {
  it('sends only tourId and comment, with dates written into the comment', async () => {
    const user = userEvent.setup();
    const tour = sampleTour({ id: 'tour-77', title: 'Санторини' });

    render(<BookingForm tourId={tour.id} tours={[tour]} />);

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    const comment = await screen.findByPlaceholderText(/хочу поехать/i);
    await user.type(comment, 'Хочу поехать 1–8 июня, двое взрослых');
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    await user.click(await screen.findByRole('button', { name: 'Отправить заявку' }));

    expect(createBooking).toHaveBeenCalledWith({
      tourId: 'tour-77',
      comment: 'Хочу поехать 1–8 июня, двое взрослых',
    });
    const payload = vi.mocked(createBooking).mock.calls[0][0];
    expect(Object.keys(payload).sort()).toEqual(['comment', 'tourId']);
  });
});
