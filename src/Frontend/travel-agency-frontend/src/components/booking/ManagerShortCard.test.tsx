import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ManagerPublicCard } from '@/api/managerCard';
import ManagerShortCard from './ManagerShortCard';

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

function renderCard(
  loadCard: (userId: string) => Promise<ManagerPublicCard>,
  path = '/dashboard/bookings/booking-1',
) {
  const onClose = vi.fn();
  const view = render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <Routes>
        <Route
          path="*"
          element={<ManagerShortCard userId="manager-1" onClose={onClose} loadCard={loadCard} />}
        />
      </Routes>
    </MemoryRouter>,
  );
  return { ...view, onClose };
}

const fullCard: ManagerPublicCard = {
  id: 'manager-1',
  displayName: 'Марина Иванова',
  avatarUrl: '/api/v1/media/files/file-1/a256',
};

describe('ManagerShortCard', () => {
  it('показывает фото и имя, без снятых строк', async () => {
    const cases: ManagerPublicCard[] = [
      fullCard,
      { id: 'manager-1', displayName: 'Марина Иванова' },
    ];

    for (const card of cases) {
      const view = renderCard(async () => card);
      if (card.avatarUrl) {
        expect(await screen.findByRole('img', { name: 'Марина Иванова' })).toHaveAttribute(
          'src',
          card.avatarUrl,
        );
        expect(document.body.innerHTML).not.toMatch(/minio|amazonaws|blob\.core/i);
      }
      expect(await screen.findByTestId('manager-card-name')).toHaveTextContent('Марина Иванова');
      expect(screen.queryByRole('heading', { name: 'О себе' })).not.toBeInTheDocument();
      expect(screen.queryByText(/В туризме/)).not.toBeInTheDocument();
      expect(screen.queryByText(/Языки/)).not.toBeInTheDocument();
      expect(screen.queryByText('не указано')).not.toBeInTheDocument();
      expect(screen.queryByText('Появится позже')).not.toBeInTheDocument();
      expect(screen.getByTestId('manager-card-name')).not.toHaveTextContent('Менеджер');
      if (!card.avatarUrl) expect(screen.getByText('МИ')).toBeInTheDocument();
      expect(screen.queryByText('secret@example.com')).not.toBeInTheDocument();
      expect(screen.queryByText('+79990000000')).not.toBeInTheDocument();
      view.unmount();
    }
  });

  it('пока запрос идёт, показывает индикатор без ошибки', async () => {
    let resolveCard: (card: ManagerPublicCard) => void = () => undefined;
    const pending = new Promise<ManagerPublicCard>((resolve) => {
      resolveCard = resolve;
    });
    renderCard(() => pending);

    expect(await screen.findByRole('status', { name: 'Загрузка' })).toBeInTheDocument();
    expect(screen.queryByText('Не удалось открыть карточку.')).not.toBeInTheDocument();

    resolveCard(fullCard);
    expect(await screen.findByText('Марина Иванова')).toBeInTheDocument();
  });

  it.each([
    ['network', new Error('network down')],
    ['404', Object.assign(new Error('missing'), { response: { status: 404, data: { detail: 'User not found.' } } })],
    ['500', Object.assign(new Error('boom'), { response: { status: 500, data: { detail: 'Internal server error' } } })],
  ])('ошибка %s даёт одну русскую фразу', async (_label, error) => {
    renderCard(async () => {
      throw error;
    });

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    expect(screen.queryByText('User not found.')).not.toBeInTheDocument();
    expect(screen.queryByText('Internal server error')).not.toBeInTheDocument();
    expect(screen.queryByTestId('manager-card-name')).not.toBeInTheDocument();
  });

  it('закрывается Escape, фоном и кнопкой, адрес не меняется', async () => {
    const user = userEvent.setup();
    const path = '/dashboard/profile';

    const first = renderCard(async () => fullCard, path);
    expect(await screen.findByText('Марина Иванова')).toBeInTheDocument();
    await user.keyboard('{Escape}');
    expect(first.onClose).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('location')).toHaveTextContent(path);
    first.unmount();

    const second = renderCard(async () => fullCard, path);
    expect(await screen.findByText('Марина Иванова')).toBeInTheDocument();
    const backdrop = document.querySelector('.backdrop-blur-sm');
    expect(backdrop).toBeTruthy();
    await user.click(backdrop as Element);
    expect(second.onClose).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('location')).toHaveTextContent(path);
    expect(screen.getByTestId('location').textContent).not.toContain('manager-1');
    second.unmount();

    const third = renderCard(async () => fullCard, '/dashboard/bookings/booking-1');
    expect(await screen.findByText('Марина Иванова')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Закрыть' }));
    expect(third.onClose).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('location')).toHaveTextContent('/dashboard/bookings/booking-1');
    expect(screen.getByTestId('location').textContent).not.toContain('manager-1');
  });
});
