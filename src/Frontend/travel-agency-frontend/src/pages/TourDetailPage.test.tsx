import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import TourDetailPage from './TourDetailPage';
import { getTourById, getTours } from '@/api/catalog';
import { useAuthStore } from '@/store/authStore';
import { sampleTour } from '@/test/sampleTour';

vi.mock('@/api/catalog', () => ({
  getTourById: vi.fn(),
  getTours: vi.fn(),
  getDestinations: vi.fn(),
}));

function renderDetail() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/tours/tour-1']}>
        <Routes>
          <Route path="/tours/:id" element={<TourDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TourDetailPage', () => {
  beforeEach(() => {
    vi.mocked(getTours).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      pageSize: 6,
      totalPages: 0,
    });
    useAuthStore.setState({ isAuthenticated: true, user: null, isLoading: false });
  });

  it('shows gallery, program, inclusions, stay and offers, then opens a request', async () => {
    vi.mocked(getTourById).mockResolvedValue(sampleTour());
    const user = userEvent.setup();

    renderDetail();

    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Прилёт' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Включено' })).toBeInTheDocument();
    expect(screen.getByText('Проживание и питание по программе')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Не включено' })).toBeInTheDocument();
    expect(screen.getByText('Личные расходы')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Проживание' })).toBeInTheDocument();
    expect(screen.getByText('Водная вилла, всё включено')).toBeInTheDocument();
    expect(screen.getByText('Всё включено')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Предложения' })).toBeInTheDocument();

    const gallery = screen.getByRole('img', { name: 'Вилла' });
    expect(gallery).toHaveAttribute('src', expect.stringContaining('/w1600'));
    expect(gallery.getAttribute('srcset')).toContain('w800');
    expect(gallery).toHaveAttribute('loading', 'eager');

    await user.click(screen.getAllByRole('button', { name: 'Оставить заявку' })[0]);
    expect(await screen.findByRole('heading', { name: 'Оформить заявку' })).toBeInTheDocument();
  });

  it('shows not found when the tour API returns 404', async () => {
    vi.mocked(getTourById).mockRejectedValue(new Error('Тур с ID "tour-1" не найден'));

    renderDetail();

    expect(await screen.findByRole('heading', { name: 'Тур не найден' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Оставить заявку' })).not.toBeInTheDocument();
  });

  it('shows an error when the tour API fails', async () => {
    vi.mocked(getTourById).mockRejectedValue(new Error('Network'));

    renderDetail();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить тур');
  });
});
