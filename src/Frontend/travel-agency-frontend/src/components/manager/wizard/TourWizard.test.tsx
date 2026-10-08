import { type ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useLocation, useNavigate } from 'react-router-dom';
import TourWizard from './TourWizard';
import { getDestinations } from '@/api/catalog';
import { uploadTourImage } from '@/api/mediaUpload';
import {
  createTourDraft,
  deleteManagedTour,
  getManagedTour,
  publishManagedTour,
  saveBasics,
  saveConditions,
  saveDescription,
  saveImages,
  saveOffers,
  saveProgram,
  unpublishManagedTour,
} from '@/api/tourManage';
import type { ManagedTour } from '@/api/tourManage';
import { renderPage } from '@/test/render';
import { manageMediaFileUrl } from '@/utils/media';

vi.mock('@/api/catalog', () => ({
  getDestinations: vi.fn(),
}));

vi.mock('@/api/mediaUpload', () => ({
  uploadTourImage: vi.fn(),
}));

vi.mock('@/api/tourManage', () => ({
  getManagedTour: vi.fn(),
  createTourDraft: vi.fn(),
  saveBasics: vi.fn(),
  saveDescription: vi.fn(),
  saveProgram: vi.fn(),
  saveConditions: vi.fn(),
  saveOffers: vi.fn(),
  saveImages: vi.fn(),
  publishManagedTour: vi.fn(),
  unpublishManagedTour: vi.fn(),
  deleteManagedTour: vi.fn(),
}));

const FILE = '11111111-1111-1111-1111-111111111111';

function managed(overrides: Partial<ManagedTour> = {}): ManagedTour {
  return {
    id: 'tour-1',
    title: '',
    shortDescription: null,
    description: '',
    departureCity: null,
    country: '',
    tourType: 'Beach',
    durationDays: 0,
    directionId: null,
    mealPlan: null,
    accommodationText: null,
    status: 'Draft',
    source: 'Manager',
    ownerId: 'manager-1',
    version: 1,
    etag: '"1"',
    days: [],
    inclusions: [],
    offers: [],
    images: [],
    ...overrides,
  };
}

describe('TourWizard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getDestinations).mockResolvedValue([]);
    vi.mocked(getManagedTour).mockResolvedValue(managed());
    vi.mocked(createTourDraft).mockResolvedValue(managed({ title: 'Сочи', etag: '"1"' }));
    vi.mocked(saveBasics).mockResolvedValue(managed());
    vi.mocked(saveDescription).mockResolvedValue(managed());
    vi.mocked(saveProgram).mockResolvedValue(managed({ etag: '"2"', version: 2 }));
    vi.mocked(saveConditions).mockResolvedValue(managed());
    vi.mocked(saveOffers).mockResolvedValue(managed());
    vi.mocked(saveImages).mockResolvedValue(managed());
    vi.mocked(publishManagedTour).mockResolvedValue(managed({ status: 'Published', etag: '"3"' }));
    vi.mocked(unpublishManagedTour).mockResolvedValue(managed({ status: 'Unpublished', etag: '"4"' }));
    vi.mocked(deleteManagedTour).mockResolvedValue();
  });

  it('shows seven steps and saves a new draft, then the next step with If-Match', async () => {
    const user = userEvent.setup();
    renderPage(<TourWizard tourId={null} />);

    const steps = screen.getByRole('navigation', { name: 'Шаги мастера' });
    expect(within(steps).getByRole('button', { name: /Основное/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Программа/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Включено/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Проживание/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Даты и цены/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Фото/ })).toBeInTheDocument();
    expect(within(steps).getByRole('button', { name: /Проверка и публикация/ })).toBeInTheDocument();

    await user.type(screen.getByLabelText('Название'), 'Сочи');
    await user.click(screen.getByRole('button', { name: 'Далее' }));

    expect(createTourDraft).toHaveBeenCalledWith(expect.objectContaining({ title: 'Сочи', description: null }));
    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(saveProgram).toHaveBeenCalledWith('tour-1', '"1"', []);
  });

  it('saves included items and stay on the same conditions endpoint', async () => {
    const user = userEvent.setup();
    renderPage(<TourWizard tourId="tour-1" initialStep={3} />);

    expect(await screen.findByRole('heading', { name: 'Включено и не включено' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(saveConditions).toHaveBeenCalledTimes(1);
    expect(saveConditions).toHaveBeenCalledWith('tour-1', '"1"', expect.any(Object));

    expect(await screen.findByRole('heading', { name: 'Проживание и питание' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(saveConditions).toHaveBeenCalledTimes(2);
  });

  it('keeps an overlong title on the step and does not call the API', async () => {
    const user = userEvent.setup();
    renderPage(<TourWizard tourId={null} />);
    fireEvent.change(screen.getByLabelText('Название'), { target: { value: 'А'.repeat(201) } });
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(createTourDraft).not.toHaveBeenCalled();
    expect(screen.getByText(/200/)).toBeInTheDocument();
  });

  it('shows the publish checklist and the server 422 codes', async () => {
    const user = userEvent.setup();
    vi.mocked(publishManagedTour).mockRejectedValue({
      response: {
        status: 422,
        data: { missing: ['images.coverMinWidth', 'images.minCount', 'images.owner'] },
      },
    });

    renderPage(<TourWizard tourId="tour-1" initialStep={7} />);

    const checklist = await screen.findByRole('list', { name: 'Чего не хватает для публикации' });
    expect(checklist).toHaveTextContent(/не меньше 3 фото/);
    expect(checklist).toHaveTextContent(/1280/);

    await user.click(screen.getByRole('button', { name: 'Опубликовать' }));
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(/1280/);
    expect(alert).toHaveTextContent(/не меньше 3 фото/);
    expect(alert).toHaveTextContent(/другому пользователю/);
  });

  it('builds the photo preview from mediaFileId and ignores a minio upload url', async () => {
    const user = userEvent.setup();
    vi.mocked(uploadTourImage).mockResolvedValue({
      id: FILE,
      url: 'http://minio:9000/bucket/photo.webp',
      width: 1600,
      height: 900,
    });

    renderPage(<TourWizard tourId="tour-1" initialStep={6} />);
    const input = await screen.findByLabelText('Загрузить фото');
    const file = new File([new Uint8Array([1, 2, 3])], 'photo.jpg', { type: 'image/jpeg' });
    await user.upload(input, file);

    const preview = await screen.findByRole('img', { name: 'Фото тура' });
    expect(preview).toHaveAttribute('src', manageMediaFileUrl(FILE, 'w200'));
    expect(preview.getAttribute('src')).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('minio:9000');
  });

  it('shows a clear message when upload returns 413', async () => {
    const user = userEvent.setup();
    vi.mocked(uploadTourImage).mockRejectedValue({
      response: { status: 413, data: '<html>request entity too large</html>' },
    });

    renderPage(<TourWizard tourId="tour-1" initialStep={6} />);
    const input = await screen.findByLabelText('Загрузить фото');
    await user.upload(input, new File([new Uint8Array([1])], 'big.jpg', { type: 'image/jpeg' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Файл больше 10 МБ');
  });

  it('explains 412 and 428 when a step is saved with a bad version', async () => {
    const user = userEvent.setup();
    vi.mocked(saveBasics).mockRejectedValueOnce({
      response: { status: 412, data: { detail: 'precondition failed' } },
    });

    renderPage(<TourWizard tourId="tour-1" />);
    await user.click(await screen.findByRole('button', { name: 'Сохранить черновик' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(/актуальную версию/);

    vi.mocked(saveBasics).mockRejectedValueOnce({
      response: { status: 428, data: { detail: 'If-Match header is required.' } },
    });
    await user.click(screen.getByRole('button', { name: 'Сохранить черновик' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(/If-Match/);
  });

  it('deletes only a draft and lets a manager unpublish', async () => {
    const user = userEvent.setup();
    vi.mocked(getManagedTour).mockResolvedValue(managed({ status: 'Published', title: 'Мальдивы' }));
    renderPage(<TourWizard tourId="tour-1" initialStep={7} />);

    expect(await screen.findByRole('heading', { name: 'Проверка и публикация' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Удалить черновик' })).not.toBeInTheDocument();
    expect(screen.getByText('Удалить можно только черновик.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Снять с публикации' }));
    expect(unpublishManagedTour).toHaveBeenCalledWith('tour-1', '"1"');

    vi.mocked(getManagedTour).mockResolvedValue(managed({ status: 'Draft' }));
    renderPage(<TourWizard tourId="tour-1" />);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    await user.click(await screen.findByRole('button', { name: 'Удалить черновик' }));
    expect(deleteManagedTour).toHaveBeenCalledWith('tour-1', '"1"');
    confirm.mockRestore();
  });

  it('writes ?step for steps 1..7, back, and a jump in the step list', async () => {
    const user = userEvent.setup();
    const headings = [
      'Основное',
      'Программа',
      'Включено и не включено',
      'Проживание и питание',
      'Даты и цены',
      'Фото',
      'Проверка и публикация',
    ];
    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=1');

    for (let index = 0; index < headings.length; index += 1) {
      expect(await screen.findByRole('heading', { name: headings[index] })).toBeInTheDocument();
      expect(currentStep()).toBe(String(index + 1));
      if (index < headings.length - 1) {
        await user.click(screen.getByRole('button', { name: 'Далее' }));
      }
    }

    await user.click(screen.getByRole('button', { name: '4. Проживание и питание' }));
    expect(await screen.findByRole('heading', { name: 'Проживание и питание' })).toBeInTheDocument();
    expect(currentStep()).toBe('4');

    for (let step = 4; step >= 1; step -= 1) {
      expect(screen.getByRole('heading', { name: headings[step - 1] })).toBeInTheDocument();
      expect(currentStep()).toBe(String(step));
      if (step > 1) await user.click(screen.getByRole('button', { name: 'Назад' }));
    }
  });

  it('opens step 5 from ?step=5 with the saved draft', async () => {
    vi.mocked(getManagedTour).mockResolvedValue(
      managed({
        offers: [
          {
            id: 'offer-1',
            validFrom: '2026-11-01T00:00:00Z',
            validTo: '2026-11-08T00:00:00Z',
            pricePerPerson: 1500,
            currency: 'RUB',
            availableSeats: 4,
          },
        ],
      }),
    );
    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=5');

    expect(await screen.findByRole('heading', { name: 'Даты и цены' })).toBeInTheDocument();
    expect(currentStep()).toBe('5');
    expect(screen.getByLabelText('Цена за человека')).toHaveValue('1500');
  });

  it('clamps an out-of-range step and falls back when the step is not a number', async () => {
    const { unmount } = renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=9');
    expect(await screen.findByRole('heading', { name: 'Проверка и публикация' })).toBeInTheDocument();
    await waitFor(() => expect(currentStep()).toBe('7'));
    unmount();

    const zero = renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=0');
    expect(await screen.findByRole('heading', { name: 'Основное' })).toBeInTheDocument();
    await waitFor(() => expect(currentStep()).toBe('1'));
    zero.unmount();

    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=нет');
    expect(await screen.findByRole('heading', { name: 'Основное' })).toBeInTheDocument();
    await waitFor(() => expect(currentStep()).toBe('1'));
  });

  it('lets the browser back and forward buttons move between steps', async () => {
    const user = userEvent.setup();
    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=1');

    expect(await screen.findByRole('heading', { name: 'Основное' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Далее' }));
    expect(await screen.findByRole('heading', { name: 'Включено и не включено' })).toBeInTheDocument();
    expect(currentStep()).toBe('3');

    await user.click(screen.getByRole('button', { name: 'История назад' }));
    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();
    expect(currentStep()).toBe('2');

    await user.click(screen.getByRole('button', { name: 'История вперёд' }));
    expect(await screen.findByRole('heading', { name: 'Включено и не включено' })).toBeInTheDocument();
    expect(currentStep()).toBe('3');
  });

  it('keeps the first step of a new draft in history when moving to step 2', async () => {
    const user = userEvent.setup();
    renderAt(<TourWizard tourId={null} />, '/manager/tours/new');

    await user.type(screen.getByLabelText('Название'), 'Сочи');
    await user.click(screen.getByRole('button', { name: 'Далее' }));

    expect(await screen.findByRole('heading', { name: 'Программа' })).toBeInTheDocument();
    expect(screen.getByTestId('url')).toHaveTextContent('/manager/tours/tour-1?step=2');

    await user.click(screen.getByRole('button', { name: 'История назад' }));
    expect(await screen.findByRole('heading', { name: 'Основное' })).toBeInTheDocument();
    expect(screen.getByTestId('url')).toHaveTextContent('/manager/tours/tour-1?step=1');
    expect(screen.getByLabelText('Название')).toHaveValue('Сочи');
  });

  it('shows the published status instead of the publish button', async () => {
    vi.mocked(getManagedTour).mockResolvedValue(managed({ status: 'Published', title: 'Мальдивы' }));
    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=7');

    expect(await screen.findByRole('heading', { name: 'Проверка и публикация' })).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Опубликован');
    expect(screen.queryByRole('button', { name: 'Опубликовать' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Снять с публикации' })).toBeInTheDocument();
  });

  it('still shows the publish button for a draft and an unpublished tour', async () => {
    const { unmount } = renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=7');
    expect(await screen.findByRole('heading', { name: 'Проверка и публикация' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Опубликовать' })).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    unmount();

    vi.mocked(getManagedTour).mockResolvedValue(managed({ status: 'Unpublished', title: 'Сочи' }));
    renderAt(<TourWizard tourId="tour-1" />, '/manager/tours/tour-1?step=7');
    expect(await screen.findByRole('heading', { name: 'Проверка и публикация' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Опубликовать' })).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});

function UrlProbe() {
  const location = useLocation();
  const navigate = useNavigate();
  return (
    <>
      <div data-testid="url">{`${location.pathname}${location.search}`}</div>
      <button type="button" onClick={() => navigate(-1)}>
        История назад
      </button>
      <button type="button" onClick={() => navigate(1)}>
        История вперёд
      </button>
    </>
  );
}

function renderAt(ui: ReactElement, entry: string) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[entry]}>
        <UrlProbe />
        {ui}
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function currentStep(): string | null {
  const href = screen.getByTestId('url').textContent ?? '';
  return new URL(href, 'http://test.local').searchParams.get('step');
}
