import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import toast from 'react-hot-toast';
import BookingForm from './BookingForm';
import { createBooking } from '@/api/bookings';
import { getTours } from '@/api/catalog';
import { sampleTour } from '@/test/sampleTour';
import { formatFromMoney, formatMoney } from '@/utils/format';

vi.mock('@/api/bookings', () => ({
  createBooking: vi.fn().mockResolvedValue({ id: 'b1' }),
}));

vi.mock('@/api/catalog', () => ({
  getTours: vi.fn(),
  getTourById: vi.fn(),
}));

vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

function renderForm(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

function flat(value: string | null | undefined) {
  return (value ?? '').replace(/[\u00a0\u202f]/g, ' ');
}

describe('BookingForm', () => {
  beforeEach(() => {
    vi.mocked(createBooking).mockClear();
    vi.mocked(createBooking).mockResolvedValue({ id: 'b1' } as Awaited<ReturnType<typeof createBooking>>);
    vi.mocked(toast.success).mockClear();
    vi.mocked(toast.error).mockClear();
  });

  it('sends only tourId and comment, with dates written into the comment', async () => {
    const user = userEvent.setup();
    const tour = sampleTour({ id: 'tour-77', title: 'Санторини' });

    renderForm(<BookingForm tourId={tour.id} tours={[tour]} />);

    expect(screen.queryByText('Выбор тура')).not.toBeInTheDocument();
    expect(screen.getByText('Укажите удобные даты и сколько человек поедет.')).toBeInTheDocument();
    const comment = screen.getByPlaceholderText(/хочу поехать/i);
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

  it('shows the selected date price and currency, then a link to the booking', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const tour = sampleTour({ id: 'tour-77', title: 'Санторини', price: 0, priceFrom: 100, currency: 'RUB' });

    renderForm(
      <BookingForm
        tourId={tour.id}
        tours={[tour]}
        priceQuote={{ amount: 1500, currency: 'USD', from: false }}
        scheduleLabel="1 дек. — 8 дек. 2026"
        onClose={onClose}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();

    const priceText = flat(screen.getByText('Цена').parentElement?.textContent);
    expect(flat(screen.getByText('Даты').parentElement?.textContent)).toContain('1 дек. — 8 дек. 2026');
    expect(priceText).toContain(flat(formatMoney(1500, 'USD')));
    expect(priceText).not.toContain('₽');
    expect(priceText).not.toContain('от');

    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(await screen.findByText('Заявка отправлена. Менеджер ответит в чате заявки, обычно в течение рабочего дня')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Перейти к заявке' })).toHaveAttribute(
      'href',
      '/dashboard/bookings/b1',
    );
    expect(toast.success).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Продолжить просмотр' }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('uses «от» and the tour currency when no date is selected, and hides a zero price', async () => {
    const user = userEvent.setup();
    const priced = sampleTour({
      id: 'tour-eur',
      title: 'Рим',
      price: 0,
      priceFrom: 900,
      currency: 'EUR',
    });
    const { unmount } = renderForm(
      <BookingForm
        tourId={priced.id}
        tours={[priced]}
        priceQuote={{ amount: 900, currency: 'EUR', from: true }}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    expect(flat(screen.getByText('Цена').parentElement?.textContent)).toContain(
      flat(formatFromMoney(900, 'EUR')),
    );

    unmount();

    const empty = sampleTour({ id: 'tour-empty', title: 'Без цены', price: 0, priceFrom: null });
    renderForm(<BookingForm tourId={empty.id} tours={[empty]} />);
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    expect(screen.queryByText('Цена')).not.toBeInTheDocument();
    expect(screen.queryByText(/0\s*₽/)).not.toBeInTheDocument();
  });

  it('keeps the tour picker and closes after submit when the tour is not chosen yet', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const onSuccess = vi.fn();
    const tour = sampleTour({ id: 'tour-77', title: 'Санторини', price: 0, priceFrom: 500, currency: 'USD' });
    vi.mocked(getTours).mockResolvedValue({
      items: [tour],
      total: 1,
      page: 1,
      pageSize: 6,
      totalPages: 1,
    });

    renderForm(<BookingForm onSuccess={onSuccess} onClose={onClose} />);

    expect(flat((await screen.findByRole('button', { name: /Санторини/ })).textContent)).toContain(
      flat(formatFromMoney(500, 'USD')),
    );
    expect(screen.getByText('Выбор тура')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Санторини/ }));
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByText('Укажите удобные даты и сколько человек поедет.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(onSuccess).toHaveBeenCalledOnce();
    expect(onClose).toHaveBeenCalledOnce();
    expect(toast.success).toHaveBeenCalledWith('Заявка успешно создана!');
    expect(screen.queryByRole('link', { name: 'Перейти к заявке' })).not.toBeInTheDocument();
  });

  it('keeps the tour card form open and shows an error when submit fails', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const tour = sampleTour({ id: 'tour-77', title: 'Санторини' });
    vi.mocked(createBooking).mockRejectedValueOnce({ response: { status: 500 } });

    renderForm(<BookingForm tourId={tour.id} tours={[tour]} onClose={onClose} />);

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(toast.error).toHaveBeenCalledWith('Не удалось создать заявку. Попробуйте ещё раз.');
    expect(screen.getByRole('heading', { name: 'Проверьте данные заявки' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Отправить заявку' })).toBeInTheDocument();
    expect(screen.queryByText(/Заявка отправлена/)).not.toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
