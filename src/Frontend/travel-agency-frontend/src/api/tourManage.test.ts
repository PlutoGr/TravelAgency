import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createTourDraft, saveBasics, saveDescription } from './tourManage';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
}));

const tour = {
  id: 'tour-1',
  title: 'Сочи',
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
  version: 2,
  etag: '"2"',
  days: [],
  inclusions: [],
  offers: [],
  images: [],
};

describe('tour manage API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('creates a draft without If-Match', async () => {
    mockApiClient.post.mockResolvedValue({ data: { ...tour, version: 1, etag: '"1"' }, headers: {} });

    await createTourDraft({ title: 'Сочи' });

    expect(mockApiClient.post).toHaveBeenCalledWith('/catalog/manage/tours', { title: 'Сочи' });
  });

  it('sends If-Match from the tour version on each step write', async () => {
    mockApiClient.put.mockResolvedValueOnce({ data: tour, headers: { etag: '"2"' } });
    mockApiClient.put.mockResolvedValueOnce({ data: { ...tour, version: 3, etag: '"3"' }, headers: {} });

    const basics = await saveBasics('tour-1', '"1"', { title: 'Сочи' });
    await saveDescription(basics.id, basics.etag, 'Полное описание');

    expect(mockApiClient.put).toHaveBeenNthCalledWith(
      1,
      '/catalog/manage/tours/tour-1/basics',
      { title: 'Сочи' },
      { headers: { 'If-Match': '"1"' } },
    );
    expect(mockApiClient.put).toHaveBeenNthCalledWith(
      2,
      '/catalog/manage/tours/tour-1/description',
      { description: 'Полное описание' },
      { headers: { 'If-Match': '"2"' } },
    );
  });
});
