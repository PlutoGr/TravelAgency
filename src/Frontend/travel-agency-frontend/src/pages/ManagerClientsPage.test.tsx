import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { getAllBookings } from '@/api/bookings';
import { getClientList } from '@/api/clientCards';
import type { Booking } from '@/types';
import ManagerClientsPage from './ManagerClientsPage';

vi.mock('@/api/clientCards', () => ({
  getClientList: vi.fn(),
}));

vi.mock('@/api/bookings', () => ({
  getAllBookings: vi.fn(),
  bookingTourTitle: (booking: { tour?: { title?: string } }) => booking.tour?.title || 'Тур недоступен',
}));

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

const booking = {
  id: 'b1',
  clientId: 'anna-id',
  tourId: 't1',
  comment: 'комментарий заявки',
  status: 'new',
  createdAt: '2026-11-02T09:00:00Z',
  updatedAt: null,
  proposals: [],
  clientName: 'Из заявки',
  clientEmail: 'from-booking@example.com',
  clientPhone: null,
  assignedManagerId: null,
  assignedManagerName: null,
  tour: { id: 't1', title: 'Солнечная Греция' },
} as unknown as Booking;

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/manager/clients']}>
      <LocationProbe />
      <Routes>
        <Route path="/manager/clients" element={<ManagerClientsPage />} />
        <Route path="/manager/clients/:id" element={<div>полная карточка</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ManagerClientsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getAllBookings).mockResolvedValue([booking]);
  });

  it('reads the row from the clients list and does not search by phone', async () => {
    const user = userEvent.setup();
    vi.mocked(getClientList).mockResolvedValue([
      {
        id: 'anna-id',
        displayName: 'Анна Соколова',
        email: 'anna@example.com',
        avatarUrl: '/api/v1/media/files/file-1/a256',
        phone: '+79991234567',
        bookingCount: 2,
      },
    ]);
    renderPage();

    expect(await screen.findByRole('link', { name: 'Карточка клиента, Анна Соколова' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Анна Соколова' })).toHaveAttribute(
      'src',
      '/api/v1/media/files/file-1/a256',
    );
    expect(screen.getAllByRole('link', { name: '+79991234567' }).every((link) => link.getAttribute('href') === 'tel:+79991234567')).toBe(true);
    const narrowWithPhone = document.querySelector('.sm\\:hidden');
    expect(narrowWithPhone).toHaveTextContent('2 заявки');
    expect(narrowWithPhone?.querySelector('a')).toHaveAttribute('href', 'tel:+79991234567');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(getClientList).toHaveBeenCalled();

    await user.click(screen.getByRole('link', { name: 'Карточка клиента, Анна Соколова' }));
    expect(screen.getByText('полная карточка')).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/clients/anna-id');
  });

  it('hides the phone when the key is absent and expands bookings without notes', async () => {
    const user = userEvent.setup();
    vi.mocked(getClientList).mockResolvedValue([
      {
        id: 'anna-id',
        displayName: 'Анна Соколова',
        email: 'anna@example.com',
        bookingCount: 2,
      },
    ]);
    renderPage();
    expect(await screen.findByText('Анна Соколова')).toBeInTheDocument();
    expect(screen.queryByText('—')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /7999/ })).not.toBeInTheDocument();
    const narrowWithoutPhone = document.querySelector('.sm\\:hidden');
    expect(narrowWithoutPhone).toHaveTextContent('2 заявки');
    expect(narrowWithoutPhone?.querySelector('a')).toBeNull();

    await user.click(screen.getByRole('button', { name: /Анна Соколова/ }));
    expect(await screen.findByText('Солнечная Греция')).toBeInTheDocument();
    expect(screen.queryByText('просит вылет после 15 мая')).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.type(screen.getByPlaceholderText('Поиск по имени или email...'), '7999');
    expect(screen.queryByText('Анна Соколова')).not.toBeInTheDocument();
    await user.clear(screen.getByPlaceholderText('Поиск по имени или email...'));
    await user.type(screen.getByPlaceholderText('Поиск по имени или email...'), 'anna@example.com');
    expect(await screen.findByText('Анна Соколова')).toBeInTheDocument();
  });

  it('shows the existing list error and no placeholder phone', async () => {
    vi.mocked(getClientList).mockRejectedValue(new Error('down'));
    renderPage();
    expect(await screen.findByText('Не удалось загрузить список бронирований')).toBeInTheDocument();
    expect(screen.queryByText('—')).not.toBeInTheDocument();
    expect(screen.queryByText('Анна Соколова')).not.toBeInTheDocument();
  });
});
