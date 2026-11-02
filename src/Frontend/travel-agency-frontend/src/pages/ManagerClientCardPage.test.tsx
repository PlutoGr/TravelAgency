import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import type { ClientCard, ClientNote } from '@/api/clientCards';
import { addClientNote, clientBookingTitles, getClientCard } from '@/api/clientCards';
import ManagerClientCardPage from './ManagerClientCardPage';

vi.mock('@/api/clientCards', () => ({
  getClientCard: vi.fn(),
  addClientNote: vi.fn(),
  clientBookingTitles: vi.fn(),
  clientNoteFieldError: (error: unknown) => {
    const response = (error as { response?: { status?: number; data?: { errors?: { text?: string[] } } } }).response;
    if (response?.status !== 400) return null;
    return response.data?.errors?.text?.[0] ?? null;
  },
}));

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

const older: ClientCard['bookings'][number] = {
  id: 'b-old',
  tourId: 'tour-missing',
  status: 'new',
  createdAt: '2026-11-01T09:00:00Z',
};
const newer: ClientCard['bookings'][number] = {
  id: 'b-new',
  tourId: 'tour-known',
  status: 'cancelled',
  createdAt: '2026-11-02T10:00:00Z',
};

const note: ClientNote = {
  id: 'n-old',
  text: 'старая заметка',
  authorName: 'Марина Иванова',
  createdAt: '2026-11-02T12:30:00Z',
};

function card(partial: Partial<ClientCard> = {}): ClientCard {
  return {
    id: 'anna-id',
    displayName: 'Анна Соколова',
    email: 'anna@example.com',
    avatarUrl: '/api/v1/media/files/file-1/a256',
    phone: '+79991234567',
    bookings: [newer, older],
    notes: [note],
    ...partial,
  };
}

function renderPage(id = 'anna-id') {
  return render(
    <MemoryRouter initialEntries={[`/manager/clients/${id}`]}>
      <LocationProbe />
      <Routes>
        <Route path="/manager/clients/:id" element={<ManagerClientCardPage />} />
        <Route path="/manager/bookings/:id" element={<div>заявка менеджера</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ManagerClientCardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(clientBookingTitles).mockImplementation(async (bookings) =>
      new Map(bookings.map((item) => [item.id, item.tourId === 'tour-known' ? 'Солнечная Греция' : 'Тур недоступен'])),
    );
    vi.mocked(getClientCard).mockResolvedValue(card());
  });

  it('shows email, bookings and notes, and drops the phone when the key is absent', async () => {
    const user = userEvent.setup();
    const { unmount } = renderPage();
    expect(await screen.findByRole('heading', { name: 'Анна Соколова' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'anna@example.com' })).toHaveAttribute('href', 'mailto:anna@example.com');
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Написать' })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: '+79991234567' })).toHaveAttribute('href', 'tel:+79991234567');
    expect(screen.getByText('2 заявки')).toBeInTheDocument();
    expect(screen.getByText('Солнечная Греция')).toBeInTheDocument();
    expect(screen.getByText('Тур недоступен')).toBeInTheDocument();
    expect(screen.getByText('Отменена')).toBeInTheDocument();
    expect(screen.getByText('Новая')).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: 'Открыть заявку' })[0]).toHaveAttribute(
      'href',
      '/manager/bookings/b-new',
    );
    expect(screen.queryByText(/Панель менеджера/)).toBeInTheDocument();
    expect(screen.getByText('Клиенты')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Сохранить' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Изменить' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Удалить' })).not.toBeInTheDocument();
    expect(clientBookingTitles).toHaveBeenCalled();

    await user.click(screen.getAllByRole('link', { name: 'Открыть заявку' })[0]);
    expect(screen.getByText('заявка менеджера')).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/manager/bookings/b-new');
    unmount();

    vi.mocked(getClientCard).mockResolvedValue(card({ phone: undefined, avatarUrl: undefined, displayName: 'Анна Соколова' }));
    renderPage();
    expect(await screen.findByRole('heading', { name: 'Анна Соколова' })).toBeInTheDocument();
    expect(screen.queryByText('Телефон')).not.toBeInTheDocument();
    expect(screen.queryByText('—')).not.toBeInTheDocument();
    expect(screen.getByText('АС')).toBeInTheDocument();
    expect(screen.queryByText('нет фото')).not.toBeInTheDocument();
  });

  it('shows only the loading line, then only the error phrase', async () => {
    let resolveCard: (value: ClientCard) => void = () => undefined;
    vi.mocked(getClientCard).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveCard = resolve;
        }),
    );
    const view = renderPage();
    expect(screen.getByText('Загрузка карточки...')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Добавить' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Анна Соколова' })).not.toBeInTheDocument();

    resolveCard(card());
    expect(await screen.findByRole('heading', { name: 'Анна Соколова' })).toBeInTheDocument();
    view.unmount();

    vi.mocked(getClientCard).mockRejectedValue({
      response: { status: 503, data: { detail: 'Upstream service unavailable' } },
    });
    renderPage('missing');
    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    expect(screen.queryByText('Upstream service unavailable')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Новая заметка')).not.toBeInTheDocument();
    expect(screen.queryByText('Анна Соколова')).not.toBeInTheDocument();
  });

  it('appends a note, clears the field, and keeps the text on an empty value or a failed save', async () => {
    const user = userEvent.setup();
    vi.mocked(getClientCard).mockResolvedValue(card({ notes: [] }));
    renderPage();
    expect(await screen.findByText('Заметок пока нет')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Просит вылет после 15 мая')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Напишите текст заметки.');
    expect(addClientNote).not.toHaveBeenCalled();

    await user.type(screen.getByLabelText('Новая заметка'), '   ');
    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Напишите текст заметки.');

    await user.clear(screen.getByLabelText('Новая заметка'));
    await user.type(screen.getByLabelText('Новая заметка'), 'я'.repeat(501));
    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Заметка не длиннее 500 символов.');
    expect(screen.queryByText('Не удалось сохранить заметку.')).not.toBeInTheDocument();
    expect(addClientNote).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText('Новая заметка'));
    vi.mocked(addClientNote).mockResolvedValue({
      id: 'n-new',
      text: 'просит вылет после 15 мая',
      authorName: 'Марина Иванова',
      createdAt: '2026-11-02T15:04:00Z',
    });
    await user.type(screen.getByLabelText('Новая заметка'), 'просит вылет после 15 мая');
    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(await screen.findByText('просит вылет после 15 мая')).toBeInTheDocument();
    expect(screen.getByText(/Марина Иванова/)).toBeInTheDocument();
    expect(screen.getByLabelText('Новая заметка')).toHaveValue('');
    expect(screen.getByRole('heading', { name: 'Анна Соколова' })).toBeInTheDocument();
    expect(addClientNote).toHaveBeenCalledWith('anna-id', 'просит вылет после 15 мая');

    vi.mocked(addClientNote).mockRejectedValueOnce({ response: { status: 503, data: { detail: 'Upstream service unavailable' } } });
    await user.type(screen.getByLabelText('Новая заметка'), 'ещё одна');
    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(await screen.findByText('Не удалось сохранить заметку.')).toBeInTheDocument();
    expect(screen.getByLabelText('Новая заметка')).toHaveValue('ещё одна');
    expect(screen.getByText('просит вылет после 15 мая')).toBeInTheDocument();
  });

  it('does not send a second note while the first request is in flight', async () => {
    const user = userEvent.setup();
    let resolveNote: (note: ClientNote) => void = () => undefined;
    vi.mocked(addClientNote).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveNote = resolve;
        }),
    );
    renderPage();
    await screen.findByRole('heading', { name: 'Анна Соколова' });
    await user.type(screen.getByLabelText('Новая заметка'), 'одна');
    await user.click(screen.getByRole('button', { name: 'Добавить' }));
    expect(addClientNote).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: 'Добавить' })).toBeDisabled();
    resolveNote({
      id: 'n2',
      text: 'одна',
      authorName: 'Марина Иванова',
      createdAt: '2026-11-02T16:00:00Z',
    });
    expect(await screen.findByText('одна')).toBeInTheDocument();
  });

  it('shows every booking status word and an empty booking list', async () => {
    const statuses = ['new', 'in_progress', 'proposal_sent', 'confirmed', 'closed', 'cancelled'] as const;
    const labels = ['Новая', 'В работе', 'Предложение отправлено', 'Подтверждена', 'Закрыта', 'Отменена'];
    vi.mocked(getClientCard).mockResolvedValue(
      card({
        bookings: statuses.map((status, index) => ({
          id: `b-${index}`,
          tourId: 'tour-known',
          status,
          createdAt: `2026-11-0${index + 1}T09:00:00Z`,
        })),
        notes: [],
      }),
    );
    const view = renderPage();
    for (const label of labels) {
      expect(await screen.findByText(label)).toBeInTheDocument();
    }
    expect(screen.queryByText(/\d[\d\s]*₽/)).not.toBeInTheDocument();
    view.unmount();

    vi.mocked(getClientCard).mockResolvedValue(card({ bookings: [], notes: [] }));
    renderPage('empty');
    expect(await screen.findByText('Заявок нет')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Открыть заявку' })).not.toBeInTheDocument();
  });

  it('does not show profile extras', async () => {
    renderPage();
    const page = await screen.findByRole('heading', { name: 'Анна Соколова' });
    expect(page).toBeInTheDocument();
    for (const label of ['Предпочтения', 'Паспорт', 'О себе', 'Языки', 'Специализация', 'не указано', 'Появится позже']) {
      expect(screen.queryByText(label)).not.toBeInTheDocument();
    }
  });
});
