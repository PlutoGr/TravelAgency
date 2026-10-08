import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ManagerToursPage from './ManagerToursPage';
import { deleteManagedTour, listManagedTours, unpublishManagedTour } from '@/api/tourManage';
import type { ManagedTour } from '@/api/tourManage';
import { renderPage } from '@/test/render';
import { manageMediaFileUrl } from '@/utils/media';

vi.mock('@/api/tourManage', () => ({
  listManagedTours: vi.fn(),
  deleteManagedTour: vi.fn(),
  publishManagedTour: vi.fn(),
  unpublishManagedTour: vi.fn(),
}));

const FILE = '22222222-2222-2222-2222-222222222222';

function tour(overrides: Partial<ManagedTour> = {}): ManagedTour {
  return {
    id: 'tour-1',
    title: 'Черновик у моря',
    shortDescription: null,
    description: '',
    departureCity: 'Москва',
    country: 'Россия',
    tourType: 'Beach',
    durationDays: 7,
    directionId: null,
    mealPlan: null,
    accommodationText: null,
    status: 'Draft',
    source: 'Manager',
    ownerId: 'manager-1',
    version: 4,
    etag: '"4"',
    days: [],
    inclusions: [],
    offers: [],
    images: [
      {
        id: 'img-1',
        mediaFileId: FILE,
        sortOrder: 0,
        isCover: true,
        alt: null,
        widthPx: 1600,
      },
    ],
    ...overrides,
  };
}

describe('ManagerToursPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('previews a cover by mediaFileId and deletes only drafts', async () => {
    const user = userEvent.setup();
    const draft = tour();
    const published = tour({
      id: 'tour-2',
      title: 'Опубликованный тур',
      status: 'Published',
      etag: '"9"',
      images: [],
    });
    vi.mocked(listManagedTours).mockResolvedValue([
      { ...draft, url: 'http://minio:9000/bucket/cover.webp' } as ManagedTour & { url: string },
      published,
    ]);
    vi.mocked(deleteManagedTour).mockResolvedValue();
    vi.mocked(unpublishManagedTour).mockResolvedValue({ ...published, status: 'Unpublished' });

    renderPage(<ManagerToursPage />);

    const cover = await screen.findByRole('img', { name: 'Черновик у моря' });
    expect(cover).toHaveAttribute('src', manageMediaFileUrl(FILE, 'w200'));
    expect(document.body.innerHTML).not.toContain('minio');

    expect(screen.getAllByRole('button', { name: 'Удалить' })).toHaveLength(1);
    expect(screen.getAllByText('Удалить можно только черновик.')).toHaveLength(1);

    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    await user.click(screen.getByRole('button', { name: 'Удалить' }));
    expect(deleteManagedTour).toHaveBeenCalledWith('tour-1', '"4"');

    await user.click(screen.getByRole('button', { name: 'Снять с публикации' }));
    expect(unpublishManagedTour).toHaveBeenCalledWith('tour-2', '"9"');
    confirm.mockRestore();
  });
});
