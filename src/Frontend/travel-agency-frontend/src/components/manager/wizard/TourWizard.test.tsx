import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
    vi.mocked(saveProgram).mockResolvedValue(managed({ etag: '"2"', version: 2 }));
    vi.mocked(saveConditions).mockResolvedValue(managed());
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

  it('builds the photo preview from the file id and does not read an upload url', async () => {
    const user = userEvent.setup();
    vi.mocked(uploadTourImage).mockResolvedValue({
      id: FILE,
      width: 1600,
      height: 900,
      url: 'http://minio:9000/bucket/photo.webp?X-Amz-Signature=abc',
    } as Awaited<ReturnType<typeof uploadTourImage>> & { url: string });

    renderPage(<TourWizard tourId="tour-1" initialStep={6} />);
    const input = await screen.findByLabelText('Загрузить фото');
    const file = new File([new Uint8Array([1, 2, 3])], 'photo.jpg', { type: 'image/jpeg' });
    await user.upload(input, file);

    const preview = await screen.findByRole('img', { name: 'Фото тура' });
    expect(preview).toHaveAttribute('src', manageMediaFileUrl(FILE, 'w200'));
    expect(document.body.innerHTML).not.toContain('minio');
    expect(document.body.innerHTML).not.toContain('X-Amz-');
    expect(document.body.innerHTML).not.toContain('unsplash');
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
});
