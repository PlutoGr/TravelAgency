import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ClientCard } from '@/api/clientCards';
import ClientShortCard from './ClientShortCard';

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

const anna: ClientCard = {
  id: 'anna-id',
  displayName: 'Анна Соколова',
  email: 'anna@example.com',
  avatarUrl: '/api/v1/media/files/file-1/a256',
  phone: '+79991234567',
  bookings: [
    { id: 'b1', tourId: 't1', status: 'new', createdAt: '2026-11-02T09:00:00Z' },
    { id: 'b2', tourId: 't2', status: 'closed', createdAt: '2026-11-01T09:00:00Z' },
  ],
  notes: [
    {
      id: 'n1',
      text: 'просит вылет после 15 мая',
      authorName: 'Марина Иванова',
      createdAt: '2026-11-02T12:30:00Z',
    },
  ],
};

function renderCard(loadCard: (id: string) => Promise<ClientCard>, path = '/manager/bookings/booking-1') {
  const onClose = vi.fn();
  const view = render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <Routes>
        <Route path="/manager/clients/:id" element={<div>полная карточка</div>} />
        <Route
          path="*"
          element={<ClientShortCard clientId="anna-id" onClose={onClose} loadCard={loadCard} />}
        />
      </Routes>
    </MemoryRouter>,
  );
  return { ...view, onClose };
}

describe('ClientShortCard', () => {
  it('shows photo, name, phone and count, and hides email, bookings and notes', async () => {
    const loadCard = vi.fn().mockResolvedValue(anna);
    const user = userEvent.setup();
    renderCard(loadCard);

    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    expect(dialog).not.toHaveTextContent('anna@example.com');
    expect(dialog).not.toHaveTextContent('просит вылет после 15 мая');
    expect(within(dialog).getByRole('img', { name: 'Анна Соколова' })).toHaveAttribute(
      'src',
      anna.avatarUrl,
    );
    expect(within(dialog).getByText('Анна Соколова')).toBeInTheDocument();
    expect(within(dialog).getByRole('link', { name: '+79991234567' })).toHaveAttribute(
      'href',
      'tel:+79991234567',
    );
    expect(within(dialog).getByText('Телефон')).toBeInTheDocument();
    expect(within(dialog).getByText('2 заявки')).toBeInTheDocument();
    expect(within(dialog).queryByRole('button', { name: 'Позвонить' })).not.toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/booking-1');

    await user.click(within(dialog).getByRole('link', { name: 'Открыть карточку' }));
    expect(screen.getByText('полная карточка')).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/clients/anna-id');
    expect(loadCard).toHaveBeenCalledWith('anna-id');
  });

  it('hides the phone row when the key is absent', async () => {
    const { phone: _phone, ...withoutPhone } = anna;
    renderCard(async () => withoutPhone);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    expect(within(dialog).queryByText('Телефон')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('—')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('не указано')).not.toBeInTheDocument();
    expect(within(dialog).queryByText('скрыт')).not.toBeInTheDocument();
    expect(within(dialog).getByText('Анна Соколова')).toBeInTheDocument();
    expect(within(dialog).getByRole('link', { name: 'Открыть карточку' })).toBeInTheDocument();
  });

  it('shows only the error phrase for 404 and 503 and drops the previous person', async () => {
    const user = userEvent.setup();
    const loadCard = vi
      .fn()
      .mockResolvedValueOnce(anna)
      .mockRejectedValueOnce({ response: { status: 404, data: { detail: 'Client card was not found.' } } });
    const view = render(
      <MemoryRouter initialEntries={['/manager/bookings/booking-1']}>
        <Routes>
          <Route
            path="*"
            element={<ClientShortCard clientId="anna-id" onClose={vi.fn()} loadCard={loadCard} />}
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(await screen.findByText('Анна Соколова')).toBeInTheDocument();
    view.rerender(
      <MemoryRouter initialEntries={['/manager/bookings/booking-1']}>
        <Routes>
          <Route
            path="*"
            element={<ClientShortCard clientId="other-id" onClose={vi.fn()} loadCard={loadCard} />}
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    expect(screen.queryByText('Анна Соколова')).not.toBeInTheDocument();
    expect(screen.queryByText('Client card was not found.')).not.toBeInTheDocument();
    expect(screen.queryByText('+79991234567')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Открыть карточку' })).not.toBeInTheDocument();
    await user.keyboard('{Escape}');
  });

  it('uses initials when there is no photo', async () => {
    const { avatarUrl: _avatar, ...withoutPhoto } = anna;
    renderCard(async () => withoutPhoto);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    expect(within(dialog).getByText('АС')).toBeInTheDocument();
    expect(within(dialog).queryByRole('img')).not.toBeInTheDocument();
    expect(dialog).not.toHaveTextContent('нет фото');
  });

  it('closes by the close button, the backdrop and Escape without leaving the chat path', async () => {
    const user = userEvent.setup();
    const { onClose } = renderCard(async () => anna);
    const dialog = await screen.findByRole('dialog', { name: 'Карточка клиента' });
    await user.click(within(dialog).getByRole('button', { name: 'Закрыть' }));
    expect(onClose).toHaveBeenCalledTimes(1);

    const backdrop = dialog.parentElement?.firstElementChild as HTMLElement;
    await user.click(backdrop);
    expect(onClose).toHaveBeenCalledTimes(2);
    await user.keyboard('{Escape}');
    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(3));
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/booking-1');
  });
});
